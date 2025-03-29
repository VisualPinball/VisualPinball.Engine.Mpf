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

using VisualPinball.Engine.Mpf.Unity.MediaController.Messages;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerAdded;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Text
{
    public class PlayerCountText : MonitoredVariableText<int, PlayerAddedMessage>
    {
        protected override MonitorBase<int, PlayerAddedMessage> CreateMonitor(
            BcpInterface bcpInterface
        )
        {
            return new PlayerCountMonitor(bcpInterface);
        }
    }
}
