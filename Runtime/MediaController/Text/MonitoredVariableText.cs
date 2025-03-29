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
using TMPro;
using UnityEngine;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Text
{
    public abstract class MonitoredVariableTextBase : MonoBehaviour
    {
        [SerializeField]
        protected TextMeshProUGUI _textField;

        [SerializeField]
        protected string _format = "{0}";
    }

    public abstract class MonitoredVariableText<TVar, TMessage> : MonitoredVariableTextBase
        where TVar : IEquatable<TVar>, IConvertible
        where TMessage : EventArgs
    {
        private MonitorBase<TVar, TMessage> _monitor;

        protected abstract MonitorBase<TVar, TMessage> CreateMonitor(BcpInterface bcpInterface);

        private void OnEnable()
        {
            if (MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface))
            {
                _monitor = CreateMonitor(bcpInterface);
                SetText(_monitor.VarValue);
                _monitor.ValueChanged += Monitor_ValueChanged;
            }
        }

        private void OnDisable()
        {
            if (_monitor != null)
            {
                _monitor.ValueChanged -= Monitor_ValueChanged;
                _monitor.Dispose();
            }
        }

        private void Monitor_ValueChanged(object sender, TVar value) => SetText(value);

        private void SetText(TVar value) => _textField.text = string.Format(_format, value);
    }
}
