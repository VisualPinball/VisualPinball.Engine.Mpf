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
using UnityEngine;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerTurnStart;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerVariable
{
    public abstract class PlayerVariableMonitor<VarType>
        : MpfVariableMonitorBase<VarType, PlayerVariableMessage>
        where VarType : IEquatable<VarType>
    {
        [SerializeField]
        private CurrentPlayerMonitor _currentPlayerMonitor;

        protected Dictionary<int, VarType> _varPerPlayer = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            _currentPlayerMonitor.ValueChanged += CurrentPlayerMonitor_ValueChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_currentPlayerMonitor)
                _currentPlayerMonitor.ValueChanged -= CurrentPlayerMonitor_ValueChanged;
        }

        private void CurrentPlayerMonitor_ValueChanged(object sender, int currentPlayerNum)
        {
            _varPerPlayer.TryAdd(currentPlayerNum, default);
            VarValue = _varPerPlayer[currentPlayerNum];
        }

        protected override void MessageHandler_Received(object sender, PlayerVariableMessage msg)
        {
            if (base.MatchesMonitoringCriteria(msg))
            {
                VarType var = GetValueFromMessage(msg);
                _varPerPlayer[msg.PlayerNum] = var;
            }
            base.MessageHandler_Received(sender, msg);
        }

        protected override bool MatchesMonitoringCriteria(PlayerVariableMessage msg)
        {
            return base.MatchesMonitoringCriteria(msg)
                && msg.PlayerNum == _currentPlayerMonitor.VarValue;
        }
    }
}
