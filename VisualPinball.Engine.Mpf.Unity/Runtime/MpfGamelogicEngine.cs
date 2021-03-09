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
using System.Linq;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;

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
		public MpfClient Client = new MpfClient();

		public string machineFolder;

		[SerializeField] private GamelogicEngineSwitch[] availableSwitches = new GamelogicEngineSwitch[0];
		[SerializeField] private GamelogicEngineCoil[] availableCoils = new GamelogicEngineCoil[0];
		[SerializeField] private GamelogicEngineLamp[] availableLamps = new GamelogicEngineLamp[0];

		public void OnInit(Player player, TableApi tableApi, BallManager ballManager)
		{
		}

		public void Switch(string id, bool isClosed)
		{
		}

		public void GetMachineDescription()
		{
			var md = MpfApi.GetMachineDescription(machineFolder);
			availableSwitches = md.GetSwitches().ToArray();
			availableCoils = md.GetCoils().ToArray();
			availableLamps = md.GetLights().ToArray();
		}
	}
}
