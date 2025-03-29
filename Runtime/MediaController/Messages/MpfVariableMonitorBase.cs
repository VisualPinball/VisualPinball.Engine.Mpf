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
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages
{
    public abstract class MpfVariableMonitorBase<TVar, TMessage> : MonitorBase<TVar, TMessage>
        where TVar : IEquatable<TVar>
        where TMessage : MpfVariableMessageBase
    {
        protected string _varName;

        protected MpfVariableMonitorBase(BcpInterface bcpInterface, string varName)
            : base(bcpInterface)
        {
            _varName = varName;
        }

        protected override bool MatchesMonitoringCriteria(TMessage msg)
        {
            return base.MatchesMonitoringCriteria(msg) && msg.Name == _varName;
        }

        protected override TVar GetValueFromMessage(TMessage msg)
        {
            try
            {
                return (TVar)Convert.ChangeType(msg.Value, typeof(TVar));
            }
            catch (Exception e)
                when (e is InvalidCastException
                    || e is FormatException
                    || e is OverflowException
                    || e is ArgumentNullException
                )
            {
                throw new ParameterException(MpfVariableMessageBase.ValueParamName, null, e);
            }
        }
    }
}
