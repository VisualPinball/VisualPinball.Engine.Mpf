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

using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Switch
{
    public class SwitchMonitor : MonitorBase<bool, SwitchMessage>
    {
        protected string _switchName;

        public SwitchMonitor(BcpInterface bcpInterface, string switchName)
            : base(bcpInterface)
        {
            _switchName = switchName;
        }

        protected override string BcpCommand => SwitchMessage.Command;

        protected override bool MatchesMonitoringCriteria(SwitchMessage msg)
        {
            return base.MatchesMonitoringCriteria(msg) && msg.Name == _switchName;
        }

        protected override bool GetValueFromMessage(SwitchMessage msg) => msg.IsActive;
    }
}
