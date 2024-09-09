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
#if UNITY_EDITOR
using UnityEditor;
#endif
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using Logger = NLog.Logger;
using System.IO;

namespace VisualPinball.Engine.Mpf.Unity
{
	[Serializable]
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Visual Pinball/Game Logic Engine/Mission Pinball Framework")]
	public class MpfGamelogicEngine : MonoBehaviour, IGamelogicEngine
	{
		public string Name { get; } = "Mission Pinball Framework";

		public GamelogicEngineSwitch[] RequestedSwitches => requiredSwitches;
		public GamelogicEngineCoil[] RequestedCoils => requiredCoils;
		public GamelogicEngineLamp[] RequestedLamps => requiredLamps;
		public GamelogicEngineWire[] AvailableWires => availableWires;

		public event EventHandler<EventArgs> OnStarted;
		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<CoilEventArgs> OnCoilChanged;
		public event EventHandler<RequestedDisplays> OnDisplaysRequested;
		public event EventHandler<string> OnDisplayClear;
		public event EventHandler<DisplayFrameData> OnDisplayUpdateFrame;
		public event EventHandler<SwitchEventArgs2> OnSwitchChanged;

		[NonSerialized]
		private MpfApi _api;

		public string MachineFolder
		{
			get
			{
				if (_machineFolder != null && _machineFolder.Contains("StreamingAssets/"))
				{
					return Path.Combine(Application.streamingAssetsPath, _machineFolder.Split("StreamingAssets/")[1]);
				}
				return _machineFolder;
			}
			set
			{
#if UNITY_EDITOR
                Undo.RecordObject(this, "Set machine folder");
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
                if (value.Contains("StreamingAssets/"))
				{
					_machineFolder = "./StreamingAssets/" + value.Split("StreamingAssets/")[1];
				}
				else
				{
					_machineFolder = value;
				}
			}
		}
		[SerializeField] private string _machineFolder;

		[SerializeField] private SerializedGamelogicEngineSwitch[] requiredSwitches = Array.Empty<SerializedGamelogicEngineSwitch>();
		[SerializeField] private SerializedGamelogicEngineCoil[] requiredCoils = Array.Empty<SerializedGamelogicEngineCoil>();
		[SerializeField] private SerializedGamelogicEngineLamp[] requiredLamps = Array.Empty<SerializedGamelogicEngineLamp>();
		[SerializeField] private GamelogicEngineWire[] availableWires = Array.Empty<GamelogicEngineWire>();

		// MPF uses names and numbers (for hardware mapping) to identify switches, coils, and lamps.
		// VPE only uses names, which is why the classes in the arrays above do not store the numbers.
		// These dictionaries store the numbers externally to make communication with MPF possible.
		[SerializeField] private MpfNameNumberDictionary _mpfSwitchNumbers = new();
		[SerializeField] private MpfNameNumberDictionary _mpfCoilNumbers = new();
		[SerializeField] private MpfNameNumberDictionary _mpfLampNumbers = new();

        private Player _player;

        private bool _displaysAnnounced;

		private readonly Queue<Action> _dispatchQueue = new Queue<Action>();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public void OnInit(Player player, TableApi tableApi, BallManager ballManager)
		{
			_player = player;
			_switchIds.Clear();
			foreach (var sw in requiredSwitches) {
				_switchNames[sw.Id] = sw.Id;
			}
			_coilNames.Clear();
			foreach (var coil in requiredCoils) {
				_coilNames[coil.Id] = coil.Id;
			}
			_lampNames.Clear();
			foreach (var lamp in requiredLamps) {
				_lampNames[lamp.Id] = lamp.Id;
			}
			_api = new MpfApi(MachineFolder);
			_api.Launch(new MpfConsoleOptions {
				ShowLogInsteadOfConsole = false,
				VerboseLogging = true,
				UseMediaController = true,
			});

			_api.Client.OnEnableCoil += OnEnableCoil;
			_api.Client.OnDisableCoil += OnDisableCoil;
			_api.Client.OnPulseCoil += OnPulseCoil;
			_api.Client.OnConfigureHardwareRule += OnConfigureHardwareRule;
			_api.Client.OnRemoveHardwareRule += OnRemoveHardwareRule;
			_api.Client.OnFadeLight += OnFadeLight;
			_api.Client.OnDmdFrame += OnDmdFrame;

			// map initial switches
			var mappedSwitchStatuses = new Dictionary<string, bool>();
			foreach (var swName in player.SwitchStatuses.Keys) {
				if (_mpfSwitchNumbers.ContainsName(swName)) {
					mappedSwitchStatuses[_mpfSwitchNumbers.GetNumberByName(swName)] = player.SwitchStatuses[swName].IsSwitchClosed;
				} else {
					Logger.Warn($"Unknown intial switch name \"{swName}\".");
				}
			}
			_api.StartGame(mappedSwitchStatuses);

			OnStarted?.Invoke(this, EventArgs.Empty);
			Logger.Info("Game started.");
		}

		private void Update()
		{
			lock (_dispatchQueue) {
				while (_dispatchQueue.Count > 0) {
					_dispatchQueue.Dequeue().Invoke();
				}
			}
		}

		public void Switch(string id, bool isClosed)
		{
			if (_mpfSwitchNumbers.ContainsName(id)) {
				var number = _mpfSwitchNumbers.GetNumberByName(id);
				Logger.Info($"--> switch {id} ({number}): {isClosed}");
				_api.Switch(number, isClosed);
			} else {
				Logger.Error("Unmapped MPF switch " + id);
			}

			OnSwitchChanged?.Invoke(this, new SwitchEventArgs2(id, isClosed));
		}

		public void GetMachineDescription()
		{
			MachineDescription md = null;

			try {
				md = MpfApi.GetMachineDescription(MachineFolder);
			}

			catch (Exception e) {
				Logger.Error($"Unable to get machine description. Check maching config. {e.Message}");
			}

			if (md != null) {
#if UNITY_EDITOR
				Undo.RecordObject(this, "Get machine description");
				PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
				requiredSwitches = md.GetSwitches().ToArray();
				requiredCoils = md.GetCoils().ToArray();
				requiredLamps = md.GetLights().ToArray();
				_mpfSwitchNumbers.Init(md.GetSwitchNumbersByNameDict());
				_mpfCoilNumbers.Init(md.GetCoilNumbersByNameDict());
				_mpfLampNumbers.Init(md.GetLampNumbersByNameDict());
			}
		}

		public void SetCoil(string id, bool isEnabled)
		{
			OnCoilChanged?.Invoke(this, new CoilEventArgs(id, isEnabled));
		}

		public void SetLamp(string id, float value, bool isCoil = false, LampSource source = LampSource.Lamp)
		{
			OnLampChanged?.Invoke(this, new LampEventArgs(id, value, isCoil, source));
		}

		public LampState GetLamp(string id)
		{
			return _player.LampStatuses.ContainsKey(id) ? _player.LampStatuses[id] : LampState.Default;
		}

		public bool GetSwitch(string id)
		{
			return _player.SwitchStatuses.ContainsKey(id) && _player.SwitchStatuses[id].IsSwitchEnabled;
		}

		public bool GetCoil(string id)
		{
			return _player.CoilStatuses.ContainsKey(id) && _player.CoilStatuses[id];
		}

		private void OnEnableCoil(object sender, EnableCoilRequest e)
		{
			if (_mpfCoilNumbers.ContainsNumber(e.CoilNumber)) {
				var coilName = _mpfCoilNumbers.GetNameByNumber(e.CoilNumber);
				Logger.Info($"<-- coil {e.CoilNumber} ({coilName}): true");
				_player.ScheduleAction(1, () => OnCoilChanged?.Invoke(this, new CoilEventArgs(coilName, true)));
			} else {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
			}
		}

		private void OnDisableCoil(object sender, DisableCoilRequest e)
		{
			if (_mpfCoilNumbers.ContainsNumber(e.CoilNumber)) {
                var coilName = _mpfCoilNumbers.GetNameByNumber(e.CoilNumber);
                Logger.Info($"<-- coil {e.CoilNumber} ({coilName}): false");
				_player.ScheduleAction(1, () => OnCoilChanged?.Invoke(this, new CoilEventArgs(coilName, false)));
			} else {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
			}
		}

		private void OnPulseCoil(object sender, PulseCoilRequest e)
		{
            if (_mpfCoilNumbers.ContainsNumber(e.CoilNumber)) {
                var coilName = _mpfCoilNumbers.GetNameByNumber(e.CoilNumber);
                _player.ScheduleAction(e.PulseMs * 10, () => {
					Logger.Info($"<-- coil {coilName} ({e.CoilNumber}): false (pulse)");
					OnCoilChanged?.Invoke(this, new CoilEventArgs(coilName, false));
				});
				Logger.Info($"<-- coil {e.CoilNumber} ({coilName}): true (pulse {e.PulseMs}ms)");
				_player.ScheduleAction(1, () => OnCoilChanged?.Invoke(this, new CoilEventArgs(coilName, true)));
			} else {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
			}
		}

		private void OnFadeLight(object sender, FadeLightRequest e)
		{
			var args = new List<LampEventArgs>();
			foreach (var fade in e.Fades) {
				if (_mpfLampNumbers.ContainsNumber(fade.LightNumber)) {
					var lampName = _mpfLampNumbers.GetNameByNumber(fade.LightNumber);
					args.Add(new LampEventArgs(lampName, fade.TargetBrightness));
				} else {
					Logger.Error("Unmapped MPF lamp " + fade.LightNumber);
				}
			}
			_player.ScheduleAction(1, () => {
				OnLampsChanged?.Invoke(this, new LampsEventArgs(args.ToArray()));
			});
		}

		private void OnConfigureHardwareRule(object sender, ConfigureHardwareRuleRequest e)
		{
			if (!_mpfSwitchNumbers.ContainsNumber(e.SwitchNumber)) {
				Logger.Error("Unmapped MPF switch " + e.SwitchNumber);
				return;
			}
			if (!_mpfCoilNumbers.ContainsNumber(e.CoilNumber)) {
				Logger.Error("Unmapped MPF coil " + e.CoilNumber);
				return;
			}

			var switchName = _mpfSwitchNumbers.GetNameByNumber(e.SwitchNumber);
			var coilName = _mpfCoilNumbers.GetNameByNumber(e.CoilNumber);
			_player.ScheduleAction(1, () => _player.AddHardwareRule(switchName, coilName));
			Logger.Info($"<-- new hardware rule: {switchName} -> {coilName}.");
		}

		private void OnRemoveHardwareRule(object sender, RemoveHardwareRuleRequest e)
		{
            if (!_mpfSwitchNumbers.ContainsNumber(e.SwitchNumber)) {
                Logger.Error("Unmapped MPF switch " + e.SwitchNumber);
                return;
            }
            if (!_mpfCoilNumbers.ContainsNumber(e.CoilNumber)) {
                Logger.Error("Unmapped MPF coil " + e.CoilNumber);
                return;
            }

            var switchName = _mpfSwitchNumbers.GetNameByNumber(e.SwitchNumber);
            var coilName = _mpfCoilNumbers.GetNameByNumber(e.CoilNumber);
            _player.ScheduleAction(1, () => _player.RemoveHardwareRule(switchName, coilName));
			Logger.Info($"<-- remove hardware rule: {switchName} -> {coilName}.");
		}

		private void OnDmdFrame(object sender, SetDmdFrameRequest frame)
		{
			Logger.Info($"<-- dmd frame: {frame.Name}");
			if (!_displaysAnnounced) {
				_displaysAnnounced = true;
				var config = _api.GetMachineDescription();
				Logger.Info($"[MPF] Announcing {config.Dmds} display(s)");
				foreach (var dmd in config.Dmds) {
					Logger.Info($"[MPF] Announcing display \"{dmd.Name}\" @ {dmd.Width}x{dmd.Height}");
					lock (_dispatchQueue) {
						_dispatchQueue.Enqueue(() => OnDisplaysRequested?.Invoke(this,
							new RequestedDisplays(new DisplayConfig(dmd.Name, dmd.Width, dmd.Height, true))));
					}
				}
				Logger.Info("[MPF] Displays announced.");
			}

			lock (_dispatchQueue) {

				_dispatchQueue.Enqueue(() => OnDisplayUpdateFrame?.Invoke(this,
					new DisplayFrameData(frame.Name, DisplayFrameFormat.Dmd24, frame.FrameData())));
			}
		}

		public void DisplayChanged(DisplayFrameData displayFrameData)
		{
		}

		private void OnDestroy()
		{
			if (_api != null) {
				_api.Client.OnEnableCoil -= OnEnableCoil;
				_api.Client.OnDisableCoil -= OnDisableCoil;
				_api.Client.OnPulseCoil -= OnPulseCoil;
				_api.Client.OnConfigureHardwareRule -= OnConfigureHardwareRule;
				_api.Client.OnRemoveHardwareRule -= OnRemoveHardwareRule;
				_api.Client.OnFadeLight -= OnFadeLight;
				_api.Client.OnDmdFrame -= OnDmdFrame;
				_api.Dispose();
			}
		}

	}
}
