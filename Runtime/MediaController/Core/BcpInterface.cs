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
using System.Threading;
using System.Threading.Tasks;
using NLog;
using UnityEngine;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Error;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Goodbye;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Hello;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Monitor;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Reset;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Trigger;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    [Serializable]
    public class BcpInterfaceOptions
    {
        [SerializeField]
        private int _port = 5050;
        public int Port => _port;

        [SerializeField]
        private bool _logReceivedMessages = false;
        public bool LogReceivedMessages => _logReceivedMessages;

        [SerializeField]
        private bool _logSentMessages = false;
        public bool LogSentMessages => _logSentMessages;
    }

    /// <summary>
    /// Central hub for all communication via BCP. Manages BCP server and message handlers, provides
    /// reset events and sends hello, reset and goodbye messages at appropriate times.
    /// </summary>
    public class BcpInterface : IDisposable
    {
        public BcpConnectionState ConnectionState => Server.ConnectionState;
        public event EventHandler<StateChangedEventArgs<BcpConnectionState>> ConnectionStateChanged
        {
            add { Server.StateChanged += value; }
            remove { Server.StateChanged -= value; }
        }

        public event EventHandler ResetRequested;
        public event EventHandler ResetCompleted;

        private readonly BcpInterfaceOptions _options;
        private CancellationTokenSource _connectionCts;
        private Task _receiveMessagesLoop;
        private BcpServer _server;
        private BcpServer Server => _server ??= new BcpServer(_options.Port);
        private LazyInit<SemaphoreSlim> _startStopSemaphore = new(() => new SemaphoreSlim(1, 1));

        public delegate void HandleMessage(BcpMessage message);
        private MpfEventRequester<MonitoringCategory> _monitoringCategories;
        public MpfEventRequester<MonitoringCategory> MonitoringCategories =>
            _monitoringCategories ??= new(
                bcpInterface: this,
                createStartListeningMessage: category => new MonitorStartMessage(category),
                createStopListeningMessage: category => new MonitorStopMessage(category)
            );

        private MpfEventRequester<string> _mpfEvents;
        public MpfEventRequester<string> MpfEvents =>
            _mpfEvents ??= new(
                bcpInterface: this,
                createStartListeningMessage: category => new RegisterTriggerMessage(category),
                createStopListeningMessage: category => new RemoveTriggerMessage(category)
            );

        public readonly BcpMessageHandlers MessageHandlers;

        private static Logger Logger = LogManager.GetCurrentClassLogger();

        public BcpInterface(BcpInterfaceOptions options)
        {
            _options = options;
            MessageHandlers = new BcpMessageHandlers(this);
            MessageHandlers.Hello.Received += OnHelloMessageReceived;
            MessageHandlers.Reset.Received += OnResetMessageReceived;
            MessageHandlers.Goodbye.Received += (sender, message) => Server.RequestDisconnect();
        }

        public void EnqueueMessage(ISentMessage message)
        {
            BcpMessage bcpMessage = message.ToGenericMessage();
            if (_options.LogSentMessages)
                Logger.Info($"Sending BCP message: {bcpMessage}");
            Server.EnqueueMessage(bcpMessage);
        }

        public async Task StartServer()
        {
            await _startStopSemaphore.Ref.WaitAsync();
            try
            {
                if (Server.ConnectionState == BcpConnectionState.Connected)
                    return;
                await Server.OpenConnectionAsync();
                _connectionCts = new CancellationTokenSource();
                _receiveMessagesLoop = HandleReceivedMessagesLoop(_connectionCts.Token);
            }
            finally
            {
                _startStopSemaphore.Ref.Release();
            }
        }

        public async Task StopServer()
        {
            await _startStopSemaphore.Ref.WaitAsync();
            try
            {
                if (Server.ConnectionState == BcpConnectionState.NotConnected)
                    return;
                EnqueueMessage(new GoodbyeMessage());
                _connectionCts?.Cancel();
                _connectionCts?.Dispose();
                try
                {
                    await Task.WhenAll(Server.CloseConnectionAsync(), _receiveMessagesLoop);
                }
                catch (OperationCanceledException) { }
            }
            finally
            {
                _startStopSemaphore.Ref.Release();
            }
        }

        private void OnHelloMessageReceived(object sender, HelloMessage message)
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);

            ISentMessage response;
            if (message.BcpSpecVersion == Constants.BcpSpecVersion)
            {
                response = new HelloMessage(
                    Constants.BcpSpecVersion,
                    Constants.MediaControllerName,
                    Constants.MediaControllerVersion
                );
            }
            else
            {
                string originalHelloMessage = message.ToGenericMessage().ToString();
                response = new ErrorMessage(
                    message: "unknown protocol version",
                    commandThatCausedError: originalHelloMessage
                );
            }

            EnqueueMessage(response);
            ResetCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnResetMessageReceived(object sender, ResetMessage resetMessage)
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
            EnqueueMessage(new ResetCompleteMessage());
            ResetCompleted?.Invoke(this, EventArgs.Empty);
        }

        private async Task HandleReceivedMessagesLoop(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                float startTime = Time.unscaledTime;
                float timeSpentMs = 0f;
                while (timeSpentMs < 1f && Server.TryDequeueReceivedMessage(out var message))
                {
                    HandleReceivedMessage(message);
                    timeSpentMs = (Time.unscaledTime - startTime) * 1000f;
                }
                await Task.Yield();
            }
        }

        private void HandleReceivedMessage(BcpMessage message)
        {
            if (_options.LogReceivedMessages)
                Logger.Info($"BCP Message received: {message}");

            if (MessageHandlers.Handlers.TryGetValue(message.Command, out var handler))
            {
                try
                {
                    handler.Handle(message);
                }
                catch (BcpParseException e)
                {
                    Logger.Error($"Failed to parse BCP message. Message: {message} Exception: {e}");
                }
            }
            else
            {
                Logger.Error(
                    $"No parser registered for BCP message with command '{message.Command}' "
                        + $"Message: {message}"
                );
                EnqueueMessage(new ErrorMessage("unknown command", message.ToString()));
            }
        }

        public void Dispose()
        {
            _server?.Dispose();
            _startStopSemaphore.Ref.Dispose();
        }
    }
}
