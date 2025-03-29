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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Hello
{
    public class HelloMessage : EventArgs, ISentMessage
    {
        public const string Command = "hello";
        private const string VersionName = "version";
        private const string ControllerNameName = "controller_name";
        private const string ControllerVersionName = "controller_version";

        public readonly string BcpSpecVersion;
        public readonly string ControllerName;
        public readonly string ControllerVersion;

        public HelloMessage(string version, string controllerName, string controllerVersion)
        {
            BcpSpecVersion = version;
            ControllerName = controllerName;
            ControllerVersion = controllerVersion;
        }

        public BcpMessage ToGenericMessage()
        {
            return new BcpMessage(
                command: Command,
                parameters: new JObject
                {
                    { VersionName, BcpSpecVersion },
                    { ControllerNameName, ControllerName },
                    { ControllerVersionName, ControllerVersion },
                }
            );
        }

        public static HelloMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new HelloMessage(
                version: bcpMessage.GetParamValue<string>(VersionName),
                controllerName: bcpMessage.GetParamValue<string>(ControllerNameName),
                controllerVersion: bcpMessage.GetParamValue<string>(ControllerVersionName)
            );
        }
    }
}
