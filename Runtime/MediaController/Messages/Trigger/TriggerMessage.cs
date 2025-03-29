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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Trigger
{
    public class TriggerMessage : EventArgs, ISentMessage
    {
        public const string Command = "trigger";
        public const string NameParamName = "name";

        public readonly string TriggerName;

        public TriggerMessage(string name)
        {
            TriggerName = name;
        }

        public static TriggerMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new TriggerMessage(name: bcpMessage.GetParamValue<string>(NameParamName));
        }

        public BcpMessage ToGenericMessage()
        {
            return new BcpMessage(
                command: Command,
                parameters: new JObject { { NameParamName, TriggerName } }
            );
        }
    }
}
