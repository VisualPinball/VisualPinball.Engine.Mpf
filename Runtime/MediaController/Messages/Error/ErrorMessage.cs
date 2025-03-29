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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Error
{
    public class ErrorMessage : EventArgs, ISentMessage
    {
        public const string Command = "error";
        private const string MessageParamName = "message";
        private const string CommandThatCausedErrorParamName = "command";
        public string Message { get; private set; }
        public string CommandThatCausedError { get; private set; }

        public ErrorMessage(string message, string commandThatCausedError)
        {
            Message = message;
            CommandThatCausedError = commandThatCausedError;
        }

        public BcpMessage ToGenericMessage()
        {
            return new BcpMessage(
                command: Command,
                parameters: new JObject
                {
                    { MessageParamName, Message },
                    { CommandThatCausedErrorParamName, CommandThatCausedError },
                }
            );
        }

        public static ErrorMessage FromGenericMessage(BcpMessage bcpMessage)
        {
            return new ErrorMessage(
                message: bcpMessage.GetParamValue<string>(MessageParamName),
                commandThatCausedError: bcpMessage.GetParamValue<string>(CommandThatCausedErrorParamName)
            );
        }
    }
}
