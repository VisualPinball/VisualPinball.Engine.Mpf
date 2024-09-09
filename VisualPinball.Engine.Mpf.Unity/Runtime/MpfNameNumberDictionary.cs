// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
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
using UnityEngine;
using NLog;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
    [Serializable]
    public class MpfNameNumberDictionary : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _names = new();
        [SerializeField] private List<string> _numbers = new();

        // Unity doesn't know how to serialize a Dictionary
        private Dictionary<string, string> _namesByNumber = new();
        private Dictionary<string, string> _numbersByName = new();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MpfNameNumberDictionary() { }

        public MpfNameNumberDictionary(Dictionary<string, string> namesByNumber)
        {
            Init(namesByNumber);
        }

        public void Init(Dictionary<string, string> numbersByName)
        {
            _namesByNumber.Clear();
            _numbersByName.Clear();

            foreach (var kvp in numbersByName)
            {
                _numbersByName[kvp.Key] = kvp.Value;
                _namesByNumber[kvp.Value] = kvp.Key;
            }
        }

        public void OnBeforeSerialize()
        {
            _names.Clear();
            _numbers.Clear();

            foreach (var kvp in _numbersByName)
            {
                _names.Add(kvp.Key);
                _numbers.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            _namesByNumber.Clear();
            _numbersByName.Clear();

            if (_names.Count != _numbers.Count)
            {
                Logger.Warn("Mismatch between number of serialized names and numbers of coils, " +
                    "switches, or lamps in machine description. Update the machine description " +
                    "by clicking 'Get Machine Description' in the Inspector of the MpfGameLogicEngine component.");
            }

            for (int i = 0; i != System.Math.Min(_names.Count, _numbers.Count); i++)
            {
                _namesByNumber.Add(_numbers[i], _names[i]);
                _numbersByName.Add(_names[i], _numbers[i]);
            }
        }

        public string GetNameByNumber(string number)
        {
            return _namesByNumber[number];
        }

        public string GetNumberByName(string name)
        {
            return _numbersByName[name];    
        }

        public bool ContainsName(string name)
        {
            return _numbersByName.ContainsKey(name);
        }

        public bool ContainsNumber(string number)
        {
            return _namesByNumber.ContainsKey(number);
        }
    }
}