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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerTurnStart
{
    public class PlayerTurnStartMessage : EventArgs
    {
        public const string Command = "player_turn_start";
        public const string PlayerNumParamName = "player_num";

        public readonly int PlayerNum;

        public PlayerTurnStartMessage(int playerNum)
        {
            PlayerNum = playerNum;
        }

        public static PlayerTurnStartMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new PlayerTurnStartMessage(bcpMessage.GetParamValue<int>(PlayerNumParamName));
        }
    }
}
