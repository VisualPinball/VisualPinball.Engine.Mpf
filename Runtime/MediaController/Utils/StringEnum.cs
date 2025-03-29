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

// Based on: https://weblogs.asp.net/stefansedich/enum-with-string-values-in-c
using System;
using System.Reflection;

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    /// <summary>
    /// Associates an enum value with a string
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StringValueAttribute : Attribute
    {
        public string StringValue { get; protected set; }

        public StringValueAttribute(string value)
        {
            StringValue = value;
        }
    }

    /// <summary>
    /// Maps enum values to arbitrary strings and back using a custom attribute
    /// </summary>
    public static class StringEnum
    {
        public static string GetStringValue(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            StringValueAttribute attribute = fieldInfo.GetCustomAttribute<StringValueAttribute>();
            return attribute.StringValue;
        }

        public static T GetValueFromString<T>(string value)
            where T : Enum
        {
            return GetValueFromStringUnsafe<T>(value);
        }

        public static T GetValueFromStringUnsafe<T>(string value)
        {
            Type enumType = typeof(T);
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"{enumType} is not an enum type.");
            }

            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (
                    Attribute.GetCustomAttribute(field, typeof(StringValueAttribute))
                    is StringValueAttribute attribute
                )
                {
                    if (attribute.StringValue == value)
                    {
                        return (T)field.GetValue(null);
                    }
                }
            }

            throw new ArgumentException($"No enum value with the string value '{value}' found.");
        }
    }
}
