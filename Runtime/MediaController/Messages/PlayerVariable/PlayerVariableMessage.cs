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

using Newtonsoft.Json.Linq;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerVariable
{
    public class PlayerVariableMessage : MpfVariableMessageBase
    {
        public const string Command = "player_variable";
        public const string PlayerNumParamName = "player_num";
        public readonly int PlayerNum;

        public PlayerVariableMessage(string name, int playerNum, JToken value)
            : base(name, value)
        {
            PlayerNum = playerNum;
        }

        public static PlayerVariableMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new PlayerVariableMessage(
                name: bcpMessage.GetParamValue<string>(NameParamName),
                playerNum: bcpMessage.GetParamValue<int>(PlayerNumParamName),
                value: bcpMessage.GetParamValue<JToken>(ValueParamName)
            );
        }
    }
}
