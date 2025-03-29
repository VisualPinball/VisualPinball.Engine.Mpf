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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerAdded
{
    public class PlayerCountMonitor : MonitorBase<int, PlayerAddedMessage>
    {
        public PlayerCountMonitor(BcpInterface bcpInterface)
            : base(bcpInterface) { }

        // Assumes that player numbers are assigned consecutively starting at 1
        protected override int GetValueFromMessage(PlayerAddedMessage msg) => msg.PlayerNum;

        protected override string BcpCommand => PlayerAddedMessage.Command;
    }
}
