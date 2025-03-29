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
    public abstract class SpecificDeviceMessageBase
        : EventArgs,
            IEquatable<SpecificDeviceMessageBase>
    {
        public readonly string Name;

        public SpecificDeviceMessageBase(string name)
        {
            Name = name;
        }

        public bool Equals(SpecificDeviceMessageBase other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name;
        }
    }
}
