// Visual Pinball Engine
// Copyright (C) 2025 freezy and VPE Team
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VisualPinball.Unity;

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    public enum BcpConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Disconnecting,
    };

    /// <summary>
    /// Sends and receives <c>BcpMessage</c> objects via TCP
    /// </summary>
    public class BcpServer : IDisposable
    {
        public event EventHandler<StateChangedEventArgs<BcpConnectionState>> StateChanged;
        private readonly object _connectionStateLock = new();
        private BcpConnectionState _connectionState = BcpConnectionState.NotConnected;
        public BcpConnectionState ConnectionState
        {
            get
            {
                lock (_connectionStateLock)
                {
                    return _connectionState;
                }
            }
            private set
            {
                BcpConnectionState prevState;
                lock (_connectionStateLock)
                {
                    prevState = _connectionState;
                    _connectionState = value;
                }

                if (prevState != value)
                {
                    StateChanged?.Invoke(this, new(value, prevState));
                }
            }
        }

        private CancellationTokenSource _cts = null;
        private Task _communicationTask = null;
        private readonly object _receivedMessagesLock = new();
        private readonly Queue<BcpMessage> _receivedMessages = new();
        private readonly object _outboundMessagesLock = new();
        private readonly Queue<BcpMessage> _outboundMessages = new();
        private readonly ManualResetEventSlim _disconnectRequested = new(false);
        private readonly int _port;
        private LazyInit<SemaphoreSlim> _startStopSemaphore = new(() => new SemaphoreSlim(1, 1));

        private const char Terminator = '\n';

        private enum ReceiveEndReason
        {
            Finished,
            ClientDisconnected,
        };

        public BcpServer(int port)
        {
            _port = port;
        }

        public async Task OpenConnectionAsync()
        {
            await _startStopSemaphore.Ref.WaitAsync();
            try
            {
                if (ConnectionState == BcpConnectionState.Connected)
                    return;
                if (ConnectionState == BcpConnectionState.NotConnected)
                {
                    _disconnectRequested.Reset();
                    _cts = new CancellationTokenSource();
                    _communicationTask = CommunicateAsync(_port, _cts.Token);
                }
            }
            finally
            {
                _startStopSemaphore.Ref.Release();
            }
        }

        public async Task CloseConnectionAsync()
        {
            await _startStopSemaphore.Ref.WaitAsync();
            try
            {
                if (ConnectionState == BcpConnectionState.NotConnected)
                    return;
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
                try
                {
                    await _communicationTask;
                }
                catch (OperationCanceledException) { }
            }
            finally
            {
                _startStopSemaphore.Ref.Release();
            }
        }

        public bool TryDequeueReceivedMessage(out BcpMessage message)
        {
            lock (_receivedMessagesLock)
                return _receivedMessages.TryDequeue(out message);
        }

        public void EnqueueMessage(BcpMessage message)
        {
            lock (_outboundMessagesLock)
                _outboundMessages.Enqueue(message);
        }

        public void RequestDisconnect()
        {
            if (ConnectionState == BcpConnectionState.Connected)
                _disconnectRequested.Set();
        }

        private bool TryDequeueOutboundMessage(out BcpMessage message)
        {
            lock (_outboundMessagesLock)
                return _outboundMessages.TryDequeue(out message);
        }

        private async Task CommunicateAsync(int port, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ConnectionState = BcpConnectionState.Connecting;
            var listener = new TcpListener(IPAddress.Any, port);
            try
            {
                listener.Start();
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (listener.Pending())
                    {
                        using TcpClient client = listener.AcceptTcpClient();
                        ConnectionState = BcpConnectionState.Connected;
                        using NetworkStream stream = client.GetStream();
                        const int bufferSize = 1024;
                        var byteBuffer = new byte[bufferSize];
                        var stringBuffer = new StringBuilder();
                        while (!_disconnectRequested.IsSet)
                        {
                            ct.ThrowIfCancellationRequested();
                            var sendTask = SendMessagesAsync(stream, ct);
                            var receiveTask = ReceiveMessagesAsync(
                                stream,
                                byteBuffer,
                                stringBuffer,
                                ct
                            );
                            await Task.WhenAll(sendTask, receiveTask);
                            try
                            {
                                var endReason = await receiveTask;
                                if (endReason == ReceiveEndReason.Finished)
                                    await Task.Delay(10, ct);
                                else
                                    break;
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                        }
                        ConnectionState = BcpConnectionState.Disconnecting;
                        await SendMessagesAsync(stream, ct);
                        _disconnectRequested.Reset();
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            }
            finally
            {
                listener.Stop();
                ConnectionState = BcpConnectionState.NotConnected;
            }
        }

        private async Task<ReceiveEndReason> ReceiveMessagesAsync(
            NetworkStream stream,
            byte[] byteBuffer,
            StringBuilder stringBuffer,
            CancellationToken ct
        )
        {
            while (stream.DataAvailable)
            {
                ct.ThrowIfCancellationRequested();
                int numBytesRead;
                numBytesRead = await stream.ReadAsync(byteBuffer, 0, byteBuffer.Length, ct);

                if (numBytesRead == 0)
                    return ReceiveEndReason.ClientDisconnected;

                var stringRead = Encoding.UTF8.GetString(byteBuffer, 0, numBytesRead);
                stringBuffer.Append(stringRead);
                int messageLength;
                while ((messageLength = stringBuffer.ToString().IndexOf(Terminator)) > -1)
                {
                    ct.ThrowIfCancellationRequested();
                    var message = stringBuffer.ToString(0, messageLength);
                    stringBuffer.Remove(0, messageLength + 1);
                    if (message.Length > 0 && !message.StartsWith("#"))
                    {
                        var bcpMessage = BcpMessage.FromString(message);
                        lock (_receivedMessagesLock)
                            _receivedMessages.Enqueue(bcpMessage);
                    }
                }
            }

            ct.ThrowIfCancellationRequested();
            return ReceiveEndReason.Finished;
        }

        private async Task SendMessagesAsync(NetworkStream stream, CancellationToken ct)
        {
            while (TryDequeueOutboundMessage(out BcpMessage bcpMessage))
            {
                var stringMessage = bcpMessage.ToString(encode: true);
                stringMessage += Terminator;
                var packet = Encoding.UTF8.GetBytes(stringMessage);
                await stream.WriteAsync(packet, ct);
                await stream.FlushAsync(ct);
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _startStopSemaphore.Ref.Dispose();
        }
    }
}
