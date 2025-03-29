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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Monitor
{
    public enum MonitoringCategory
    {
        [StringValue(null)]
        None,

        [StringValue("events")]
        Events,

        [StringValue("devices")]
        Devices,

        [StringValue("machine_vars")]
        MachineVars,

        [StringValue("player_vars")]
        PlayerVars,

        [StringValue("switches")]
        Switches,

        [StringValue("modes")]
        Modes,

        [StringValue("core_events")]
        CoreEvents,
    }
}
