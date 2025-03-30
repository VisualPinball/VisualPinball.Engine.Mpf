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
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Trigger;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.ObjectToggle
{
    /// <summary>
    /// Toggle the parent game object when the specified enable and disable events occur in MPF.
    /// </summary>
    [AddComponentMenu("Pinball/MPF Media Controller/Toggle On MPF Event")]
    public class ToggleOnEvent : MonoBehaviour
    {
        [SerializeField] private bool _enabledOnStart;
        [SerializeField] private string _enableEvent;
        [SerializeField] private string _disableEvent;

        private MpfEventListener _enableEventListener;
        private MpfEventListener _disableEventListener;
        private MpfEventListener _toggleEventListener;


        private void Awake()
        {
            if (!MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface)) return;

            if (_enableEvent == _disableEvent)
            {
                _toggleEventListener = new MpfEventListener(bcpInterface, _enableEvent);
                _toggleEventListener.Triggered += OnToggleEvent;
            }
            else
            {
                _enableEventListener = new MpfEventListener(bcpInterface, _enableEvent);
                _enableEventListener.Triggered += OnEnableEvent;
                _disableEventListener = new MpfEventListener(bcpInterface, _disableEvent);
                _disableEventListener.Triggered += OnDisableEvent;
            }
        }

        private void Start()
        {
            // This is done in Start to give other components like this one attached to children of this game object a
            // chance to run their Awake functions.
            gameObject.SetActive(_enabledOnStart);
        }

        private void OnDestroy()
        {
            if (_toggleEventListener != null)
            {
                _toggleEventListener.Triggered -= OnToggleEvent;
                _toggleEventListener.Dispose();
            }

            if (_enableEventListener != null)
            {
                _enableEventListener.Triggered -= OnEnableEvent;
                _enableEventListener.Dispose();
            }

            if (_disableEventListener != null)
            {
                _disableEventListener.Triggered -= OnDisableEvent;
                _disableEventListener.Dispose();
            }
        }

        private void OnEnableEvent(object sender, EventArgs eventArgs)
        {
            gameObject.SetActive(true);
        }

        private void OnDisableEvent(object sender, EventArgs eventArgs)
        {
            gameObject.SetActive(false);
        }

        private void OnToggleEvent(object sender, EventArgs eventArgs)
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}