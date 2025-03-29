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
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Monitor
{
    public class MonitorStartMessage : EventArgs, ISentMessage
    {
        public const string Command = "monitor_start";
        private const string CategoryParamName = "category";
        public readonly MonitoringCategory Category;

        public MonitorStartMessage(MonitoringCategory category)
        {
            Category = category;
        }

        public BcpMessage ToGenericMessage()
        {
            var categoryString = Category.GetStringValue();
            if (string.IsNullOrEmpty(categoryString))
                Debug.LogError(
                    "[MonitorStartMessage] Cannot create proper BCP message because monitoring "
                        + "category has no associated string value."
                );
            return new(
                command: Command,
                parameters: new JObject { [CategoryParamName] = categoryString }
            );
        }
    }
}
