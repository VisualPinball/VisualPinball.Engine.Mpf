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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerTurnStart
{
    public class CurrentPlayerMonitor : MonitorBase<int, PlayerTurnStartMessage>
    {
        protected override string BcpCommand => PlayerTurnStartMessage.Command;

        protected override int GetValueFromMessage(PlayerTurnStartMessage msg) => msg.PlayerNum;
    }
}
