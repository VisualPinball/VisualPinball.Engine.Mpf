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

using System.Collections.Generic;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Ball;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Error;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Goodbye;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Hello;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.MachineVariable;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Mode;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerAdded;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerTurnStart;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.PlayerVariable;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Reset;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Settings;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Switch;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Trigger;

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    /// <summary>
    /// Central hub for accessing all message handlers.
    /// </summary>
    public class BcpMessageHandlers
    {
        public BcpMessageHandlers(BcpInterface bcpInterface)
        {
            BallEnd = new BallEndMessageHandler(bcpInterface);
            BallStart = new BallStartMessageHandler(bcpInterface);
            Device = new DeviceMessageHandler(bcpInterface);
            Error = new ErrorMessageHandler(bcpInterface);
            Goodbye = new GoodbyeMessageHandler(bcpInterface);
            Hello = new HelloMessageHandler(bcpInterface);
            MachineVariable = new MachineVariableMessageHandler(bcpInterface);
            ModeList = new ModeListMessageHandler(bcpInterface);
            ModeStart = new ModeStartMessageHandler(bcpInterface);
            ModeStop = new ModeStopMessageHandler(bcpInterface);
            PlayerAdded = new PlayerAddedMessageHandler(bcpInterface);
            PlayerTurnStart = new PlayerTurnStartMessageHandler(bcpInterface);
            PlayerVariable = new PlayerVariableMessageHandler(bcpInterface);
            Reset = new ResetMessageHandler(bcpInterface);
            Settings = new SettingsMessageHandler(bcpInterface);
            Switch = new SwitchMessageHandler(bcpInterface);
            Trigger = new TriggerMessageHandler(bcpInterface);

            Handlers = new Dictionary<string, IBcpMessageHandler>()
            {
                { BallEnd.Command, BallEnd },
                { BallStart.Command, BallStart },
                { Device.Command, Device },
                { Error.Command, Error },
                { Goodbye.Command, Goodbye },
                { Hello.Command, Hello },
                { MachineVariable.Command, MachineVariable },
                { ModeList.Command, ModeList },
                { ModeStart.Command, ModeStart },
                { ModeStop.Command, ModeStop },
                { PlayerAdded.Command, PlayerAdded },
                { PlayerTurnStart.Command, PlayerTurnStart },
                { PlayerVariable.Command, PlayerVariable },
                { Reset.Command, Reset },
                { Settings.Command, Settings },
                { Switch.Command, Switch },
                { Trigger.Command, Trigger },
            };
        }

        public readonly Dictionary<string, IBcpMessageHandler> Handlers;

        public readonly BallEndMessageHandler BallEnd;
        public readonly BallStartMessageHandler BallStart;
        public readonly DeviceMessageHandler Device;
        public readonly ErrorMessageHandler Error;
        public readonly GoodbyeMessageHandler Goodbye;
        public readonly HelloMessageHandler Hello;
        public readonly MachineVariableMessageHandler MachineVariable;
        public readonly ModeListMessageHandler ModeList;
        public readonly ModeStartMessageHandler ModeStart;
        public readonly ModeStopMessageHandler ModeStop;
        public readonly PlayerAddedMessageHandler PlayerAdded;
        public readonly PlayerTurnStartMessageHandler PlayerTurnStart;
        public readonly PlayerVariableMessageHandler PlayerVariable;
        public readonly ResetMessageHandler Reset;
        public readonly SettingsMessageHandler Settings;
        public readonly SwitchMessageHandler Switch;
        public readonly TriggerMessageHandler Trigger;
    }
}
