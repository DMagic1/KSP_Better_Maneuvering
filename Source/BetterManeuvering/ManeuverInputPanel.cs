#region license
/*The MIT License (MIT)

ManeuverInputPanel - Controls the manual deltaV input panel

Copyright (c) 2016 DMagic

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using BetterManeuvering.Unity;
using KSP.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace BetterManeuvering
{
	public class ManeuverInputPanel : MonoBehaviour
	{
		private const string controlLock = "MNE_ControlLock";

		public class OnMouseEnter : EventTrigger.TriggerEvent { }
		public class OnMouseExit : EventTrigger.TriggerEvent { }

		private OnMouseEnter MouseEnter = new OnMouseEnter();
		private OnMouseExit MouseExit = new OnMouseExit();

		private ManeuverNode _node;
		private ManeuverGizmo _gizmo;
		private int _index;
		private bool _locked;
		private bool _hover;
		private bool _showLines;
		private bool _stickyFlight;

		private ManeuverInput _inputPanel;

		private float _progradeIncrement = 0.1f;
		private float _normalIncrement = 0.1f;
		private float _radialIncrement = 0.1f;

		private static float _persProgradeIncrement = 0.1f;
		private static float _persNormalIncrement = 0.1f;
		private static float _persRadialIncrement = 0.1f;

		private Vector3d _startDeltaV;

		private bool _isVisible;
		private UIWorldPointer _pointer;

		private Button _inputButton;
		private RectTransform _buttonRect;
		private RectTransform _panelRect;

		private void Update()
		{
			if (!_isVisible)
				return;

			if (_gizmo == null && _locked)
			{
				if (_node == null || _node.scaledSpaceTarget == null)
					DestroyImmediate(this);
			}

			if (!_stickyFlight && !MapView.MapIsEnabled)
				DestroyImmediate(this);

			if (_pointer == null)
				return;

			if ((_hover || !_locked) && MapView.MapIsEnabled)
			{
				if (!_pointer.gameObject.activeSelf)
					_pointer.gameObject.SetActive(true);
			}
			else if (_pointer.gameObject.activeSelf)
				_pointer.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			if (_inputPanel != null)
				Destroy(_inputPanel.gameObject);

			if (_pointer != null)
				_pointer.Terminate();

			if (ManeuverController.Instance != null)
				ManeuverController.Instance.RemoveInputPanel(this);

			InputLockManager.RemoveControlLock(controlLock);
		}

		public bool Locked
		{
			get { return _locked; }
		}

		public bool IsVisible
		{
			get { return _isVisible; }
		}

		public ManeuverNode Node
		{
			get { return _node; }
		}

		public void setup(ManeuverNode node, ManeuverGizmo gizmo, int i, bool replace, bool lines, bool stickyFlight)
		{
			if (node == null)
				return;

			_node = node;
			_gizmo = gizmo;
			_index = i;
			_showLines = lines;
			_stickyFlight = stickyFlight;

			if (replace)
			{
				_progradeIncrement = _persProgradeIncrement;
				_normalIncrement = _persNormalIncrement;
				_radialIncrement = _persRadialIncrement;
			}

			if (_inputButton == null)
				_inputButton = Instantiate<Button>(gizmo.minusOrbitbtn);

			_inputButton.transform.SetParent(gizmo.minusOrbitbtn.transform.parent);

			_buttonRect = _inputButton.GetComponent<RectTransform>();

			RectTransform oldRect = gizmo.minusOrbitbtn.GetComponent<RectTransform>();

			_buttonRect.anchoredPosition3D = oldRect.anchoredPosition3D;
			_buttonRect.position = oldRect.position;
			_buttonRect.localScale = oldRect.localScale;
			_buttonRect.rotation = oldRect.rotation;
			_buttonRect.localRotation = oldRect.localRotation;

			_inputButton.navigation = new Navigation() { mode = Navigation.Mode.None };

			_inputButton.onClick.RemoveAllListeners();
			_inputButton.onClick.AddListener(new UnityAction(ToggleUI));
			_inputButton.interactable = true;

			EventTrigger events = _inputButton.gameObject.AddComponent<EventTrigger>();

			events.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

			events.triggers.Add(new EventTrigger.Entry()
			{
				eventID = EventTriggerType.PointerEnter,
				callback = MouseEnter
			});

			events.triggers.Add(new EventTrigger.Entry()
			{
				eventID = EventTriggerType.PointerExit,
				callback = MouseExit
			});

			MouseEnter.AddListener(new UnityAction<UnityEngine.EventSystems.BaseEventData>(TriggerOnMouseEnter));
			MouseExit.AddListener(new UnityAction<UnityEngine.EventSystems.BaseEventData>(TriggerOnMouseExit));

			Selectable inputSelect = _inputButton.GetComponent<Selectable>();

			inputSelect.image.sprite = ManeuverLoader.InputButtonNormal;

			SpriteState state = inputSelect.spriteState;
			state.highlightedSprite = ManeuverLoader.InputButtonHighlight;
			state.pressedSprite = ManeuverLoader.InputButtonActive;
			state.disabledSprite = ManeuverLoader.InputButtonActive;
			inputSelect.spriteState = state;

			gizmo.minusOrbitbtn.onClick.RemoveAllListeners();
			oldRect.localScale = new Vector3(0.000001f, 0.000001f, 0.000001f);

			if (_pointer != null)
				_pointer.worldTransform = _buttonRect;
		}

		public void UpdateDeltaV()
		{
			if (!_isVisible)
				return;

			double magnitude = _node.DeltaV.magnitude;

			if (magnitude < 100000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, magnitude));
			else if (magnitude < 1000000000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}km/s", _index + 1, magnitude / 1000));
			else
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N0}Mm/s", _index + 1, magnitude / 1000000));

			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.x));
		}

		public void CloseGizmo()
		{
			_gizmo = null;

			if (_pointer == null)
				return;

			_pointer.worldTransform = _node.scaledSpaceTarget.transform;
		}

		public void ToggleUI()
		{
			if (_gizmo != null)
				_gizmo.SetMouseOverGizmo(true);

			if (_isVisible)
            {
                _isVisible = false;

                if (_inputPanel != null)
					Destroy(_inputPanel.gameObject);

				_inputPanel = null;

				if (_pointer != null)
					_pointer.Terminate();

				InputLockManager.RemoveControlLock(controlLock);

				_locked = false;
			}
			else
            {
                _isVisible = true;
                
                if (_inputPanel != null)
                    DestroyImmediate(_inputPanel.gameObject);
                
                openUI();

				_startDeltaV = _node.DeltaV;

				attachUI();

				reposition();

				if (_showLines)
					attachPointer();

				UpdateIncrements();
            }
		}

		private void attachPointer()
		{
			_pointer = UIWorldPointer.Create(_panelRect, _buttonRect, PlanetariumCamera.Camera, ManeuverLoader.LineMaterial);
			_pointer.chamferDistance = ManeuverLoader.LineCornerRadius;
			_pointer.lineColor = ManeuverLoader.LineColor;
			_pointer.lineWidth = ManeuverLoader.LineWidth;
			_pointer.transform.SetParent(_inputPanel.TitleTransform, true);
		}

		private void openUI()
		{
			if (ManeuverLoader.InputPrefab == null)
				return;

			_inputPanel = Instantiate(ManeuverLoader.InputPrefab).GetComponent<ManeuverInput>();

			if (_inputPanel == null)
				return;

			_inputPanel.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

			_inputPanel.gameObject.SetActive(true);
		}

		private void attachUI()
		{
			if (_inputPanel == null)
				return;

			_inputPanel.ProgradeDownButton.onClick.AddListener(new UnityAction(ProgradeDown));
			_inputPanel.ProgradeUpButton.onClick.AddListener(new UnityAction(ProgradeUp));
			_inputPanel.NormalDownButton.onClick.AddListener(new UnityAction(NormalDown));
			_inputPanel.NormalUpButton.onClick.AddListener(new UnityAction(NormalUp));
			_inputPanel.RadialDownButton.onClick.AddListener(new UnityAction(RadialDown));
			_inputPanel.RadialUpButton.onClick.AddListener(new UnityAction(RadialUp));
			_inputPanel.ResetButton.onClick.AddListener(new UnityAction(ResetManeuver));

			_inputPanel.ProgradeIncDownButton.onClick.AddListener(new UnityAction(ProIncrementDown));
			_inputPanel.ProgradeIncUpButton.onClick.AddListener(new UnityAction(ProIncrementUp));
			_inputPanel.NormalIncDownButton.onClick.AddListener(new UnityAction(NormIncrementDown));
			_inputPanel.NormalIncUpButton.onClick.AddListener(new UnityAction(NormIncrementUp));
			_inputPanel.RadialIncDownButton.onClick.AddListener(new UnityAction(RadIncrementDown));
			_inputPanel.RadialIncUpButton.onClick.AddListener(new UnityAction(RadIncrementUp));

			_inputPanel.DragEvent.AddListener(new UnityAction<RectTransform>(clampToScreen));
			_inputPanel.MouseOverEvent.AddListener(new UnityAction<bool>(SetMouseOverGizmo));
			_inputPanel.InputMouseEvent.AddListener(new UnityAction<bool>(SetControlLocks));

			_inputPanel.WindowToggle.onValueChanged.AddListener(new UnityAction<bool>(SetLockedMode));

			_inputPanel.ProgradeText.OnValueChange.AddListener(new UnityAction<string>(SetProgradeInput));
			_inputPanel.NormalText.OnValueChange.AddListener(new UnityAction<string>(SetNormalInput));
			_inputPanel.RadialText.OnValueChange.AddListener(new UnityAction<string>(SetRadialInput));

			double magnitude = _node.DeltaV.magnitude;

			if (magnitude < 100000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, magnitude));
			else if (magnitude < 1000000000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}km/s", _index + 1, magnitude / 1000));
			else
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}Mm/s", _index + 1, magnitude / 1000000));

			double startMagnitude = _startDeltaV.magnitude;

			if (startMagnitude < 100000)
				_inputPanel.ResetText.OnTextUpdate.Invoke(string.Format("{0:N2}m/s", _index + 1, startMagnitude));
			else
				_inputPanel.ResetText.OnTextUpdate.Invoke(string.Format("{0:N1}km/s", _index + 1, startMagnitude / 1000));
			
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.x));
		}

		private void reposition()
		{
			if (_inputPanel == null)
				return;

			_panelRect = _inputPanel.GetComponent<RectTransform>();

			Vector2 cam = PlanetariumCamera.Camera.WorldToScreenPoint(_buttonRect.position);

			cam.x -= (Screen.width / 2);
			cam.y -= (Screen.height / 2);

			cam.x -= 275;
			cam.y -= 50;

			_panelRect.position = cam;

			clampToScreen(_panelRect);
		}

		private void clampToScreen(RectTransform rect)
		{
			UIMasterController.ClampToWindow(UIMasterController.Instance.appCanvas.GetComponent<RectTransform>(), rect, Vector2.zero);
		}

		private void SetMouseOverGizmo(bool isOver)
		{
			_hover = isOver;

			if (_gizmo == null)
				return;

			_gizmo.SetMouseOverGizmo(isOver);
		}

		private void SetLockedMode(bool isOn)
		{
			_locked = isOn;

			if (_gizmo == null && !isOn)
			{
				SetMouseOverGizmo(false);
				DestroyImmediate(this);
			}
		}

		private void SetControlLocks(bool isOn)
		{
			if (isOn)
				InputLockManager.SetControlLock(controlLock);
			else
				InputLockManager.RemoveControlLock(controlLock);
		}

		private void SetProgradeInput(string input)
		{
			float value = 0;

			if (!float.TryParse(input, out value))
				return;

			if (_gizmo != null)
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, value);
				_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
			else
			{
				_node.DeltaV = new Vector3d(_node.DeltaV.x, _node.DeltaV.y, value);
				_node.solver.UpdateFlightPlan();
			}

			double magnitude = _node.DeltaV.magnitude;
			
			if (magnitude < 100000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, magnitude));
			else if (magnitude < 1000000000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}km/s", _index + 1, magnitude / 1000));
			else
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}Mm/s", _index + 1, magnitude / 1000000));
		}

		private void SetNormalInput(string input)
		{
			float value = 0;

			if (!float.TryParse(input, out value))
				return;

			if (_gizmo != null)
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, value, _node.DeltaV.z);
				_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
			else
			{
				_node.DeltaV = new Vector3d(_node.DeltaV.x, value, _node.DeltaV.z);
				_node.solver.UpdateFlightPlan();
			}

			double magnitude = _node.DeltaV.magnitude;

			if (magnitude < 100000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, magnitude));
			else if (magnitude < 1000000000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}km/s", _index + 1, magnitude / 1000));
			else
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}Mm/s", _index + 1, magnitude / 1000000));
		}

		private void SetRadialInput(string input)
		{
			float value = 0;

			if (!float.TryParse(input, out value))
				return;

			if (_gizmo != null)
			{
				_gizmo.DeltaV = new Vector3d(value, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
				_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
			else
			{
				_node.DeltaV = new Vector3d(value, _node.DeltaV.y, _gizmo.DeltaV.z);
				_node.solver.UpdateFlightPlan();
			}

			double magnitude = _node.DeltaV.magnitude;

			if (magnitude < 100000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, magnitude));
			else if (magnitude < 1000000000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}km/s", _index + 1, magnitude / 1000));
			else
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}Mm/s", _index + 1, magnitude / 1000000));
		}

		public void TriggerOnMouseEnter(UnityEngine.EventSystems.BaseEventData eventData)
		{
			if (_gizmo != null && eventData is PointerEventData)
				_gizmo.SetMouseOverGizmo(true);
		}

		public void TriggerOnMouseExit(UnityEngine.EventSystems.BaseEventData eventData)
		{
			if (_gizmo != null && eventData is PointerEventData)
				_gizmo.SetMouseOverGizmo(false);
		}

		private void ProgradeDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
				ManeuverController.Instance.OnRetrogradeUpdate(_progradeIncrement, _node, true, _gizmo != null);
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z - _progradeIncrement);
					_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_node.DeltaV.x, _node.DeltaV.y, _node.DeltaV.z - _progradeIncrement);
					_node.solver.UpdateFlightPlan();
				}
			}

			UpdateDeltaV();
		}

		private void ProgradeUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
				ManeuverController.Instance.OnProgradeUpdate(_progradeIncrement, _node, true, _gizmo != null);
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z + _progradeIncrement);
					_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_node.DeltaV.x, _node.DeltaV.y, _node.DeltaV.z + _progradeIncrement);
					_node.solver.UpdateFlightPlan();
				}
			}

			UpdateDeltaV();
		}

		private void NormalDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
				ManeuverController.Instance.OnAntiNormalUpdate(_normalIncrement, _node, true, _gizmo != null);
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y - _normalIncrement, _gizmo.DeltaV.z);
					_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_node.DeltaV.x, _node.DeltaV.y - _normalIncrement, _node.DeltaV.z);
					_node.solver.UpdateFlightPlan();
				}
			}

			UpdateDeltaV();
		}

		private void NormalUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
				ManeuverController.Instance.OnNormalUpdate(_normalIncrement, _node, true, _gizmo != null);
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y + _normalIncrement, _gizmo.DeltaV.z);
					_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_node.DeltaV.x, _node.DeltaV.y + _normalIncrement, _node.DeltaV.z);
					_node.solver.UpdateFlightPlan();
				}
			}

			UpdateDeltaV();
		}

		private void RadialDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
				ManeuverController.Instance.OnRadialInUpdate(_radialIncrement, _node, true, _gizmo != null);
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x - _radialIncrement, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
					_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_node.DeltaV.x - _radialIncrement, _node.DeltaV.y, _node.DeltaV.z);
					_node.solver.UpdateFlightPlan();
				}
			}

			UpdateDeltaV();
		}

		private void RadialUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
				ManeuverController.Instance.OnRadialOutUpdate(_radialIncrement, _node, true, _gizmo != null);
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x + _radialIncrement, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
					_node.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_node.DeltaV.x + _radialIncrement, _node.DeltaV.y, _node.DeltaV.z);
					_node.solver.UpdateFlightPlan();
				}
			}

			UpdateDeltaV();
		}

		private void ResetManeuver()
		{
			if (_gizmo == null)
			{
				_node.DeltaV = _startDeltaV;
				_node.solver.UpdateFlightPlan();
				UpdateDeltaV();
			}
			else
			{
				_node.OnGizmoUpdated(_startDeltaV, _node.UT);
				_gizmo.DeltaV = _startDeltaV;
				UpdateDeltaV();
			}

			double magnitude = _node.DeltaV.magnitude;

			if (magnitude < 100000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, magnitude));
			else if (magnitude < 1000000000)
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}km/s", _index + 1, magnitude / 1000));
			else
				_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}Mm/s", _index + 1, magnitude / 1000000));

			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("{0:F2}", _node.DeltaV.x));
		}


		private void ProIncrementDown()
		{
			_progradeIncrement /= 10;

			if (_progradeIncrement < 0.01f)
				_progradeIncrement = 0.01f;

			_persProgradeIncrement = _progradeIncrement;

			UpdateIncrements();
		}

		private void ProIncrementUp()
		{
			_progradeIncrement *= 10;

			if (_progradeIncrement > 100f)
				_progradeIncrement = 100f;

			_persProgradeIncrement = _progradeIncrement;

			UpdateIncrements();
		}

		private void NormIncrementDown()
		{
			_normalIncrement /= 10;

			if (_normalIncrement < 0.01f)
				_normalIncrement = 0.01f;

			_persNormalIncrement = _normalIncrement;

			UpdateIncrements();
		}

		private void NormIncrementUp()
		{
			_normalIncrement *= 10;

			if (_normalIncrement > 100f)
				_normalIncrement = 100f;

			_persNormalIncrement = _normalIncrement;

			UpdateIncrements();
		}

		private void RadIncrementDown()
		{
			_radialIncrement /= 10;

			if (_radialIncrement < 0.01f)
				_radialIncrement = 0.01f;

			_persRadialIncrement = _radialIncrement;

			UpdateIncrements();
		}

		private void RadIncrementUp()
		{
			_radialIncrement *= 10;

			if (_radialIncrement > 100f)
				_radialIncrement = 100f;
						
			_persRadialIncrement = _radialIncrement;

			UpdateIncrements();
		}

		private void UpdateIncrements()
		{
			if (_inputPanel == null)
				return;

			string units = "F0";

			if (_progradeIncrement < 0.09f)
				units = "F2";
			else if (_progradeIncrement < 0.9f)
				units = "F1";

			_inputPanel.ProIncrementText.OnTextUpdate.Invoke(_progradeIncrement.ToString(units));

			if (_normalIncrement < 0.09f)
				units = "F2";
			else if (_normalIncrement < 0.9f)
				units = "F1";
			else
				units = "F0";

			_inputPanel.NormIncrementText.OnTextUpdate.Invoke(_normalIncrement.ToString(units));

			if (_radialIncrement < 0.09f)
				units = "F2";
			else if (_radialIncrement < 0.9f)
				units = "F1";
			else
				units = "F0";

			_inputPanel.RadIncrementText.OnTextUpdate.Invoke(_radialIncrement.ToString(units));
		}

	}
}
