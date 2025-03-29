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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Settings
{
    public class SettingsMessage : EventArgs
    {
        public const string Command = "settings";
        private const string SettingsParamName = "settings";
        public readonly JArray Settings;

        public SettingsMessage(JArray settings)
        {
            Settings = settings;
        }

        public static SettingsMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new SettingsMessage(bcpMessage.GetParamValue<JArray>(SettingsParamName));
        }
    }
}
