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
using Newtonsoft.Json;
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device
{
    public class DeviceAttributeChange
    {
        public readonly string AttributeName;
        public readonly string OldValue;
        public readonly string NewValue;

        public DeviceAttributeChange(string attributeName, string oldValue, string newValue)
        {
            AttributeName = attributeName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public delegate T ConvertAttributeDelegate<T>(string attributeValue);

        public DeviceAttributeChangeEventArgs<T> GetEventArgs<T>(
            ConvertAttributeDelegate<T> convertAttribute
        )
        {
            try
            {
                return new(convertAttribute(OldValue), convertAttribute(NewValue));
            }
            catch (ConversionException e)
            {
                throw new WrongDeviceAttributeTypeException(
                    AttributeName,
                    typeof(T),
                    OldValue,
                    NewValue,
                    e
                );
            }
        }

        private class ConversionException : InvalidCastException
        {
            public ConversionException(Exception innerException)
                : base("Conversion failed", innerException) { }
        }

        public DeviceAttributeChangeEventArgs<T> GetEventArgsForPrimitiveTypes<T>()
            where T : struct
        {
            try
            {
                return new(
                    (T)Convert.ChangeType(OldValue, typeof(T)),
                    (T)Convert.ChangeType(NewValue, typeof(T))
                );
            }
            catch (InvalidCastException e)
            {
                throw new ConversionException(e);
            }
        }

        public DeviceAttributeChangeEventArgs<T> GetEventArgsForEnums<T>()
            where T : Enum
        {
            try
            {
                return new(
                    StringEnum.GetValueFromString<T>(OldValue),
                    StringEnum.GetValueFromString<T>(NewValue)
                );
            }
            catch (ArgumentException e)
            {
                throw new ConversionException(e);
            }
        }

        public DeviceAttributeChangeEventArgs<Color> GetEventArgsForColor()
        {
            try
            {
                static Color StringToColor(string s)
                {
                    var arr = JsonConvert.DeserializeObject<int[]>(s);
                    return new Color(arr[0] / 255f, arr[1] / 255f, arr[2] / 255f);
                }
                return new(StringToColor(OldValue), StringToColor(NewValue));
            }
            catch (Exception e) when (e is JsonException || e is ArgumentOutOfRangeException)
            {
                throw new ConversionException(e);
            }
        }
    }

    public class DeviceAttributeChangeEventArgs<TAttribute> : EventArgs
    {
        public readonly TAttribute OldValue;
        public readonly TAttribute NewValue;

        public DeviceAttributeChangeEventArgs(TAttribute oldValue, TAttribute newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
