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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Ball
{
    public class BallStartMessage : EventArgs
    {
        public const string Command = "ball_start";
        public const string PlayerNumParamName = "player_num";
        public const string BallNumParamName = "ball";

        public readonly int PlayerNum;
        public readonly int BallNum;

        public BallStartMessage(int playerNum, int ballNum)
        {
            PlayerNum = playerNum;
            BallNum = ballNum;
        }

        public static BallStartMessage FromGenericMessage(BcpMessage bcpMessage) =>
            new BallStartMessage(
                playerNum: bcpMessage.GetParamValue<int>(PlayerNumParamName),
                ballNum: bcpMessage.GetParamValue<int>(BallNumParamName)
            );
    }
}
