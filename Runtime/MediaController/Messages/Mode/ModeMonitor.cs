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
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Mode
{
    public class ModeMonitor : IDisposable
    {
        private string _modeName;
        private BcpMessageHandler<ModeStartMessage> _modeStartMessageHandler;
        private BcpMessageHandler<ModeStopMessage> _modeStopMessageHandler;

        private bool _isModeActive = false;
        public bool IsModeActive
        {
            get => _isModeActive;
            set
            {
                if (value == _isModeActive)
                    return;

                _isModeActive = value;
                IsModeActiveChanged?.Invoke(this, _isModeActive);
            }
        }

        public event EventHandler<bool> IsModeActiveChanged;

        public ModeMonitor(BcpInterface bcpInterface, string modeName)
        {
            _modeName = modeName;
            _modeStartMessageHandler =
                (BcpMessageHandler<ModeStartMessage>)
                    bcpInterface.MessageHandlers.Handlers[ModeStartMessage.Command];
            _modeStartMessageHandler.Received += OnModeStarted;
            _modeStopMessageHandler =
                (BcpMessageHandler<ModeStopMessage>)
                    bcpInterface.MessageHandlers.Handlers[ModeStopMessage.Command];
            _modeStopMessageHandler.Received += OnModeStopped;
        }

        public void Dispose()
        {
            if (_modeStartMessageHandler != null)
                _modeStartMessageHandler.Received -= OnModeStarted;
            if (_modeStopMessageHandler != null)
                _modeStopMessageHandler.Received -= OnModeStopped;
        }

        private void OnModeStarted(object sender, ModeStartMessage msg)
        {
            if (msg.Name != _modeName)
                return;

            IsModeActive = true;
        }

        private void OnModeStopped(object sender, ModeStopMessage msg)
        {
            if (msg.Name != _modeName)
                return;

            IsModeActive = false;
        }
    }
}
