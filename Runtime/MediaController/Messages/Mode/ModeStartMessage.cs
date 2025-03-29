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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Mode
{
    public class ModeStartMessage : EventArgs
    {
        public const string Command = "mode_start";
        public const string NameParamName = "name";
        public const string PriorityParamName = "priority";

        public readonly string Name;
        public readonly int Priority;

        public ModeStartMessage(string name, int priority)
        {
            Name = name;
            Priority = priority;
        }

        public static ModeStartMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new ModeStartMessage(
                name: bcpMessage.GetParamValue<string>(NameParamName),
                priority: bcpMessage.GetParamValue<int>(PriorityParamName)
            );
        }
    }
}
