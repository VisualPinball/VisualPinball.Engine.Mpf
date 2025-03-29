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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device
{
    public class WrongDeviceAttributeTypeException : BcpParseException
    {
        public WrongDeviceAttributeTypeException(
            string name,
            Type expectedType,
            string oldValue,
            string newValue,
            Exception innerException = null
        )
            : base(
                $"The new and old values of device attribute '{name}' (Old: '{oldValue}', "
                    + $"New: {newValue}) could not be cast to the expected type '{expectedType}'",
                null,
                innerException
            ) { }
    }

    public class UnknownDeviceAttributeException : BcpParseException
    {
        public UnknownDeviceAttributeException(
            string attributeName,
            string deviceType,
            Exception innerException = null
        )
            : base(
                $"The attribute name '{attributeName}' is not valid for device type '{deviceType}'",
                null,
                innerException
            ) { }
    }

    public class InvalidDeviceStateException : BcpParseException
    {
        public InvalidDeviceStateException(
            string type,
            Type parseFormat,
            Exception innerException = null
        )
            : base(
                $"Json key 'state' could not be parsed to the type '{parseFormat}'. The type was "
                    + $"chosen based on the value of the 'type' key ('{type}'.)",
                null,
                innerException
            ) { }
    }
}
