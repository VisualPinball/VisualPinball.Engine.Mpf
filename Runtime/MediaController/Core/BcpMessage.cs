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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    /// <summary>
    /// The generic form of all BCP messages. Consists of a command and optionally parameters. Can
    /// be parsed into more specific types using implementations of <c>BcpMessageHandler</c>.
    /// </summary>
    public class BcpMessage
    {
        public readonly string Command;
        private readonly JObject _parameters;
        private readonly bool _hasComplexParams;
        private const char CommandParamsSeparator = '?';
        private const char ParamsSeparator = '&';

        public BcpMessage(string command, JObject parameters, bool hasComplexParams = false)
        {
            Command = command;
            this._parameters = parameters;
            this._hasComplexParams = hasComplexParams;
        }

        public BcpMessage(string command)
            : this(command, new()) { }

        public T GetParamValue<T>(string name)
        {
            try
            {
                var token = _parameters[name];

                // If string is requested, but parsed type is different, just return the unparsed
                // JSON string
                if (typeof(T) == typeof(string) && token.Type != JTokenType.String)
                    return (T)Convert.ChangeType(token.ToString(Formatting.None), typeof(T));

                if (typeof(JToken).IsAssignableFrom(typeof(T)))
                    return (T)(object)_parameters[name];

                return (T)Convert.ChangeType(token, typeof(T));
            }
            catch (Exception e)
                when (e is KeyNotFoundException
                    || e is InvalidCastException
                    || e is NullReferenceException
                )
            {
                throw new ParameterException(name, this, e);
            }
        }

        public override string ToString() => ToString(encode: false);

        public string ToString(bool encode)
        {
            var sb = new StringBuilder(Command);
            if (_parameters.Count > 0)
                sb.Append(CommandParamsSeparator);

            if (_hasComplexParams)
            {
                sb.Append($"json={_parameters.ToString(Formatting.None)}");
            }
            else
            {
                var properties = _parameters.Properties();
                for (int i = 0; i < properties.Count(); i++)
                {
                    JProperty prop = properties.ElementAt(i);
                    string propStr = PropertyToParameterString(prop, encode);
                    sb.Append(propStr);
                    bool isLastParam = i == _parameters.Count - 1;
                    if (!isLastParam)
                        sb.Append(ParamsSeparator);
                }
            }
            return sb.ToString();
        }

        public static BcpMessage FromString(string str)
        {
            var parts = str.Split(CommandParamsSeparator, ParamsSeparator);
            var command = parts[0].Trim().ToLower();
            var containsJson = false;
            JObject parameters = new();
            for (int i = 1; i < parts.Length; i++)
            {
                JProperty property = ParameterStringToProperty(parts[i]);
                if (property.Name == "json" && property.Value is JObject)
                {
                    containsJson = true;
                    // Unwrap json params
                    foreach (var subProperty in (property.Value as JObject).Properties())
                    {
                        parameters.Add(subProperty.Name, subProperty.Value);
                    }
                }
                else
                    parameters.Add(property.Name, property.Value);
            }
            return new BcpMessage(command, parameters, containsJson);
        }

        private static string PropertyToParameterString(JProperty property, bool encode)
        {
            var name = property.Name;
            string value;
            if (property.Value.HasValues)
                value = property.Value.ToString(Formatting.None);
            else
                value = (string)property.Value;
            if (encode)
            {
                name = Uri.EscapeDataString(name);
                value = Uri.EscapeDataString(value);
            }

            string typeHint = property.Value.Type switch
            {
                JTokenType.Integer => "int",
                JTokenType.Float => "float",
                JTokenType.Boolean => "bool",
                JTokenType.Null => "NoneType",
                _ => null,
            };

            if (string.IsNullOrEmpty(typeHint))
                return $"{name}={value}";
            return $"{name}={typeHint}:{value}";
        }

        private static JProperty ParameterStringToProperty(string str)
        {
            string[] parts = str.Split('=', 2);
            string name = parts[0].Trim().ToLower(); // Not case sensitive
            name = Uri.UnescapeDataString(name);
            JToken value = null;
            if (parts.Length == 2)
            {
                if (name == "json")
                {
                    string valueStr = parts[1];
                    Uri.UnescapeDataString(valueStr);
                    value = JObject.Parse(valueStr);
                }
                else
                {
                    string typeHint = null;
                    string valueStr;
                    parts = parts[1].Split(':', 2);
                    if (parts.Length == 2)
                    {
                        typeHint = parts[0];
                        valueStr = parts[1];
                    }
                    else
                        valueStr = parts[0];
                    valueStr = Uri.UnescapeDataString(valueStr);
                    value = typeHint switch
                    {
                        "bool" => new JValue((bool)Convert.ChangeType(valueStr, typeof(bool))),
                        "int" => new JValue((int)Convert.ChangeType(valueStr, typeof(int))),
                        "float" => new JValue((float)Convert.ChangeType(valueStr, typeof(float))),
                        "NoneType" => null,
                        _ => new JValue(valueStr),
                    };
                }
            }

            return new JProperty(name, value);
        }
    }

    /// <summary>
    /// Most message types are only received and never sent.
    /// The ones that are sent must implement this interface
    /// </summary>
    public interface ISentMessage
    {
        public BcpMessage ToGenericMessage();
    }
}
