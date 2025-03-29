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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.Playfield
{
    public class PlayfieldDeviceMessage
        : SpecificDeviceMessageBase,
            IEquatable<PlayfieldDeviceMessage>
    {
        public readonly int AvailableBalls;
        public readonly int BallsRequested;
        public readonly int Balls;

        public PlayfieldDeviceMessage(
            string deviceName,
            int availableBalls,
            int ballsRequested,
            int balls
        )
            : base(deviceName)
        {
            AvailableBalls = availableBalls;
            BallsRequested = ballsRequested;
            Balls = balls;
        }

        public static PlayfieldDeviceMessage FromStateJson(StateJson state, string deviceName)
        {
            return new(deviceName, state.available_balls, state.balls_requested, state.balls);
        }

        public bool Equals(PlayfieldDeviceMessage other)
        {
            return base.Equals(other)
                && AvailableBalls == other.AvailableBalls
                && BallsRequested == other.BallsRequested
                && Balls == other.Balls;
        }

        public class StateJson
        {
            public int available_balls;
            public int balls_requested;
            public int balls;
        }
    }
}
