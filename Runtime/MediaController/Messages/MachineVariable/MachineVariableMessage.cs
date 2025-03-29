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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.MachineVariable
{
    public class MachineVariableMessage : MpfVariableMessageBase
    {
        public const string Command = "machine_variable";

        public MachineVariableMessage(string name, JToken value)
            : base(name, value) { }

        public static MachineVariableMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new MachineVariableMessage(
                name: bcpMessage.GetParamValue<string>(NameParamName),
                value: bcpMessage.GetParamValue<JToken>(ValueParamName)
            );
        }
    }
}
