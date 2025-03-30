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
using System.Collections.ObjectModel;
using System.Linq;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Mode
{
    public class ModeList : IEquatable<ModeList>
    {
        private readonly ReadOnlyCollection<Mode> _list;

        public ModeList(ReadOnlyCollection<Mode> list)
        {
            _list = list;
        }

        public bool Equals(ModeList other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Equals(_list, other._list)) return true;
            if (_list == null || other._list == null) return false;
            return _list.Count == other._list.Count && _list.All(other._list.Contains);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ModeList)obj);
        }

        public override int GetHashCode()
        {
            return _list != null ? _list.GetHashCode() : 0;
        }
    }
}