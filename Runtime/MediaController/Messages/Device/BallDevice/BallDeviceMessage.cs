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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.BallDevice
{
    public class BallDeviceMessage : SpecificDeviceMessageBase, IEquatable<BallDeviceMessage>
    {
        public readonly int AvailableBalls;
        public readonly BallDeviceStatus Status;
        public readonly string StatusAsString;
        public readonly int Balls;

        public BallDeviceMessage(
            string deviceName,
            int availableBalls,
            BallDeviceStatus status,
            string statusAsString,
            int balls
        )
            : base(deviceName)
        {
            AvailableBalls = availableBalls;
            Status = status;
            StatusAsString = statusAsString;
            Balls = balls;
        }

        public static BallDeviceMessage FromStateJson(StateJson state, string deviceName)
        {
            var status = StringEnum.GetValueFromString<BallDeviceStatus>(state.state);
            return new(deviceName, state.available_balls, status, state.state, state.balls);
        }

        public bool Equals(BallDeviceMessage other)
        {
            return base.Equals(other)
                && AvailableBalls == other.AvailableBalls
                && Status == other.Status
                && StatusAsString == other.StatusAsString
                && Balls == other.Balls;
        }

        public class StateJson
        {
            public int available_balls;
            public string state;
            public int balls;
        }
    }

    public enum BallDeviceStatus
    {
        [StringValue(null)]
        Unknown,

        [StringValue("idle")]
        Idle,

        [StringValue("waiting_for_ball")]
        WaitingForBall,

        [StringValue("waiting_for_target_ready")]
        WaitingForTargetReady,

        [StringValue("ejecting")]
        Ejecting,

        [StringValue("eject_broken")]
        EjectBroken,

        [StringValue("ball_left")]
        BallLeft,

        [StringValue("failed_confirm")]
        FailedConfirm,
    }
}
