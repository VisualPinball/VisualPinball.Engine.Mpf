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
using System.Linq;
using Mpf.Vpe;
using NLog;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
	[Serializable]
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Visual Pinball/Game Logic Engine/Mission Pinball Framework")]
	public class MpfGamelogicEngine : MonoBehaviour, IGamelogicEngine
	{
		public string Name { get; } = "Mission Pinball Framework";

		public GamelogicEngineSwitch[] AvailableSwitches => availableSwitches;
		public GamelogicEngineCoil[] AvailableCoils => availableCoils;
		public GamelogicEngineLamp[] AvailableLamps => availableLamps;

		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<LampColorEventArgs> OnLampColorChanged;

		public event EventHandler<CoilEventArgs> OnCoilChanged;

		[NonSerialized]
		private MpfApi _api;

		public string machineFolder;

		[SerializeField] private GamelogicEngineSwitch[] availableSwitches = new GamelogicEngineSwitch[0];
		[SerializeField] private GamelogicEngineCoil[] availableCoils = new GamelogicEngineCoil[0];
		[SerializeField] private GamelogicEngineLamp[] availableLamps = new GamelogicEngineLamp[0];

		private Player _player;
		private Dictionary<string, int> _switchIds = new Dictionary<string, int>();
		private Dictionary<string, string> _switchNames = new Dictionary<string, string>();
		private Dictionary<string, string> _coilNames = new Dictionary<string, string>();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public void OnInit(Player player, TableApi tableApi, BallManager ballManager)
		{
			_player = player;
			_switchIds.Clear();
			foreach (var sw in availableSwitches) {
				_switchIds[sw.Id] = sw.InternalId;
				_switchNames[sw.InternalId.ToString()] = sw.Id;
			}
			_coilNames.Clear();
			foreach (var coil in availableCoils) {
				_coilNames[coil.InternalId.ToString()] = coil.Id;
			}
			_api = new MpfApi(machineFolder);
			_api.Launch();

			_api.Client.OnEnableCoil += OnEnableCoil;
			_api.Client.OnDisableCoil += OnDisableCoil;
			_api.Client.OnPulseCoil += OnPulseCoil;
			_api.Client.OnConfigureHardwareRule += OnConfigureHardwareRule;
			_api.Client.OnRemoveHardwareRule += OnRemoveHardwareRule;
			_api.Client.OnFadeLight += OnFadeLight;

			_api.StartGame(player.SwitchStatusesClosed);
		}

		public void Switch(string id, bool isClosed)
		{
			if (_switchIds.ContainsKey(id)) {
				_api.Switch(_switchIds[id].ToString(), isClosed);
			} else {
				Logger.Error("Unmapped MPF switch " + id);
			}
		}

		public void GetMachineDescription()
		{
			var md = MpfApi.GetMachineDescription(machineFolder);
			availableSwitches = md.GetSwitches().ToArray();
			availableCoils = md.GetCoils().ToArray();
			availableLamps = md.GetLights().ToArray();
		}

		private void OnEnableCoil(object sender, EnableCoilRequest e)
		{
			if (_coilNames.ContainsKey(e.CoilNumber)) {
				OnCoilChanged?.Invoke(this, new CoilEventArgs(_coilNames[e.CoilNumber], true));
			} else {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
			}
		}

		private void OnDisableCoil(object sender, DisableCoilRequest e)
		{
			if (_coilNames.ContainsKey(e.CoilNumber)) {
				OnCoilChanged?.Invoke(this, new CoilEventArgs(_coilNames[e.CoilNumber], false));
			} else {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
			}
		}

		private void OnPulseCoil(object sender, PulseCoilRequest e)
		{
			Logger.Info($"PULSING COIL {e.CoilNumber}");
		}

		private void OnFadeLight(object sender, FadeLightRequest e)
		{
			Logger.Info("FADING SOME LIGHTS");
		}

		private void OnConfigureHardwareRule(object sender, ConfigureHardwareRuleRequest e)
		{
			if (!_switchNames.ContainsKey(e.SwitchNumber)) {
				Logger.Error("Unmapped MPF switch " + e.SwitchNumber);
				return;
			}
			if (!_coilNames.ContainsKey(e.CoilNumber)) {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
				return;
			}

			_player.AddDynamicWire(_switchNames[e.SwitchNumber], _coilNames[e.CoilNumber]);
			Logger.Info($"Added new hardware rule: {_switchNames[e.SwitchNumber]} -> {_coilNames[e.CoilNumber]}.");
		}

		private void OnRemoveHardwareRule(object sender, RemoveHardwareRuleRequest e)
		{
			if (!_switchNames.ContainsKey(e.SwitchNumber)) {
				Logger.Error("Unmapped MPF coil " + e.SwitchNumber);
				return;
			}
			if (!_coilNames.ContainsKey(e.CoilNumber)) {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
				return;
			}

			_player.RemoveDynamicWire(_switchNames[e.SwitchNumber], _coilNames[e.CoilNumber]);
			Logger.Info($"Removed hardware rule: {_switchNames[e.SwitchNumber]} -> {_coilNames[e.CoilNumber]}.");
		}

		private void OnDestroy()
		{
			_api.Client.OnEnableCoil -= OnEnableCoil;
			_api.Dispose();
		}
	}
}
