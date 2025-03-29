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
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerVariable;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Text
{
    public abstract class PlayerVariableText<T> : MonitoredVariableText<T, PlayerVariableMessage>
        where T : IEquatable<T>, IConvertible
    {
        [SerializeField]
        private string _variableName;

        protected override MonitorBase<T, PlayerVariableMessage> CreateMonitor(
            BcpInterface bcpInterface
        )
        {
            return new PlayerVariableMonitor<T>(bcpInterface, _variableName);
        }
    }
}
