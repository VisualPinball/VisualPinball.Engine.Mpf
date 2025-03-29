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
using Newtonsoft.Json.Linq;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Switch
{
    public class SwitchMessage : EventArgs, ISentMessage
    {
        public const string Command = "switch";
        private const string NameParamName = "name";
        private const string StateParamName = "state";

        public readonly string Name;
        public readonly bool IsActive;

        public SwitchMessage(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }

        public static SwitchMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            int intState = bcpMessage.GetParamValue<int>(StateParamName);
            bool boolState = intState switch
            {
                0 => false,
                1 => true,
                _ => throw new ParameterException(StateParamName, bcpMessage),
            };

            return new SwitchMessage(
                name: bcpMessage.GetParamValue<string>(NameParamName),
                isActive: boolState
            );
        }

        public BcpMessage ToGenericMessage()
        {
            return new BcpMessage(
                command: Command,
                parameters: new JObject
                {
                    { NameParamName, Name },
                    { StateParamName, IsActive ? 1 : 0 },
                }
            );
        }
    }
}
