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
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Mode
{
    public class ModeListMessage : EventArgs
    {
        public const string Command = "mode_list";
        public const string RunningModesParamName = "running_modes";
        public ReadOnlyCollection<Mode> RunningModes => Array.AsReadOnly(_runningModes);
        private readonly Mode[] _runningModes;

        public ModeListMessage(Mode[] runningModes)
        {
            _runningModes = runningModes;
        }

        public static ModeListMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            try
            {
                var jArr = bcpMessage.GetParamValue<JArray>(RunningModesParamName);
                Mode[] runningModes = new Mode[jArr.Count];

                for (int i = 0; i < jArr.Count; i++)
                {
                    var modeJArr = (JArray)jArr[i];
                    var modeName = (string)modeJArr[0];
                    var modePrio = (int)modeJArr[1];
                    runningModes[i] = new Mode(modeName, modePrio);
                }

                return new ModeListMessage(runningModes);
            }
            catch (Exception e)
                when (e is JsonException
                    || e is InvalidCastException
                    || e is IndexOutOfRangeException
                )
            {
                throw new ParameterException(RunningModesParamName, null, e);
            }
        }
    }
}
