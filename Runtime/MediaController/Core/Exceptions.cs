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

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    public class BcpParseException : Exception
    {
        public readonly BcpMessage Culprit;

        public BcpParseException(
            string failReason,
            BcpMessage culprit = null,
            Exception innerException = null
        )
            : base(
                $"Failed to parse bcp message: '{culprit?.ToString() ?? "Unknown"}' "
                    + $"Reason: {failReason ?? "None given"}",
                innerException
            )
        {
            Culprit = culprit;
        }
    }

    public class WrongParserException : BcpParseException
    {
        public WrongParserException(
            BcpMessage culprit,
            string expectedCommand,
            Exception innerException = null
        )
            : base(
                "Wrong parser chosen for message. Parser expected command type: "
                    + $"'{expectedCommand}' Actual: '{culprit.Command}'",
                culprit,
                innerException
            ) { }
    }

    public class ParameterException : BcpParseException
    {
        public ParameterException(
            string parameterName,
            BcpMessage culprit = null,
            Exception innerException = null
        )
            : base($"Missing or invalid parameter '{parameterName}'", culprit, innerException) { }
    }
}
