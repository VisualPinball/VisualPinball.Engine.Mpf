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
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.MachineVariable;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Text
{
    public abstract class MachineVariableText<T> : MonitoredVariableText<T, MachineVariableMessage>
        where T : IEquatable<T>, IConvertible
    {
        [SerializeField] private string _variableName;

        private static Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected override MonitorBase<T, MachineVariableMessage> CreateMonitor(
            BcpInterface bcpInterface
        )
        {
            if (string.IsNullOrWhiteSpace(_variableName))
            {
                Logger.Warn("No MPF variable name is specified. The component 'MPF Machine Variable Text' on game "
                            + $"object '{gameObject.name}' will not do anything.");
            }

            return new MachineVariableMonitor<T>(bcpInterface, _variableName);
        }
    }
}