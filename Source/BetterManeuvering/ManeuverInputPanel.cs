using System;
using System.Collections.Generic;
using BetterManeuvering.Unity;
using KSP.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BetterManeuvering
{
	public class ManeuverInputPanel : MonoBehaviour
	{
		private ManeuverNode _node;
		private ManeuverGizmo _gizmo;
		private int _index;
		private bool _locked;
		private bool _hover;

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

		private void Awake()
		{

		}

		private void Update()
		{
			if (!_isVisible)
				return;

			if (_pointer == null)
				return;

			if (_hover || !_locked)
			{
				if (!_pointer.gameObject.activeSelf)
					_pointer.gameObject.SetActive(true);
			}
			else if (_pointer.gameObject.activeSelf)
				_pointer.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			ManeuverController.Instance.RemoveInputPanel(this);

			if (_inputPanel != null)
				Destroy(_inputPanel);

			if (_pointer != null)
				_pointer.Terminate();
		}

		public bool Locked
		{
			get { return _locked; }
		}

		public ManeuverNode Node
		{
			get { return _node; }
		}

		public void setup(ManeuverNode node, ManeuverGizmo gizmo, int i, bool replace)
		{
			if (node == null)
				return;

			_node = node;
			_gizmo = gizmo;
			_index = i;

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

			_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, _node.DeltaV.magnitude));
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("Prograde: {0:N2}m/s", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("Normal: {0:N2}m/s", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("Radial: {0:N2}m/s", _node.DeltaV.x));
		}

		public void CloseGizmo()
		{
			_gizmo = null;

			if (_pointer == null)
				return;

			_pointer.worldTransform = _node.scaledSpaceTarget.transform;
		}

		private void ToggleUI()
		{
			if (_isVisible)
			{
				if (_inputPanel != null)
					Destroy(_inputPanel);

				_inputPanel = null;

				if (_pointer != null)
					_pointer.Terminate();

				_locked = false;

				_isVisible = false;
			}
			else
			{
				openUI();

				_startDeltaV = _node.DeltaV;

				attachUI();

				reposition();

				attachPointer();

				UpdateIncrements();

				_isVisible = true;
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

			GameObject obj = GameObject.Instantiate(ManeuverLoader.InputPrefab);

			if (obj == null)
				return;

			obj.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

			_inputPanel = obj.GetComponent<ManeuverInput>();

			if (_inputPanel == null)
				return;

			obj.SetActive(true);
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

			_inputPanel.WindowToggle.onValueChanged.AddListener(new UnityAction<bool>(SetLockedMode));

			_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N2}m/s", _index + 1, _node.DeltaV.magnitude));
			_inputPanel.ResetText.OnTextUpdate.Invoke(string.Format("{0:N2}m/s", _startDeltaV.magnitude));
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("Prograde: {0:N2}m/s", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("Normal: {0:N2}m/s", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("Radial: {0:N2}m/s", _node.DeltaV.x));
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
				OnDestroy();
		}

		private void ProgradeDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnRetrogradeUpdate(_progradeIncrement, _node, true, _gizmo != null);

				if (_gizmo == null)
					UpdateDeltaV();
			}
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z - _progradeIncrement);
					_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z - _progradeIncrement);
					_node.solver.UpdateFlightPlan();
					UpdateDeltaV();
				}
			}
		}

		private void ProgradeUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnProgradeUpdate(_progradeIncrement, _node, true, _gizmo != null);

				if (_gizmo == null)
					UpdateDeltaV();
			}
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z + _progradeIncrement);
					_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z + _progradeIncrement);
					_node.solver.UpdateFlightPlan();
					UpdateDeltaV();
				}
			}
		}

		private void NormalDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnAntiNormalUpdate(_normalIncrement, _node, true, _gizmo != null);

				if (_gizmo == null)
					UpdateDeltaV();
			}
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y - _normalIncrement, _gizmo.DeltaV.z);
					_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y - _normalIncrement, _gizmo.DeltaV.z);
					_node.solver.UpdateFlightPlan();
					UpdateDeltaV();
				}
			}
		}

		private void NormalUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnNormalUpdate(_normalIncrement, _node, true, _gizmo != null);

				if (_gizmo == null)
					UpdateDeltaV();
			}
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y + _normalIncrement, _gizmo.DeltaV.z);
					_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y + _normalIncrement, _gizmo.DeltaV.z);
					_node.solver.UpdateFlightPlan();
					UpdateDeltaV();
				}
			}
		}

		private void RadialDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnRadialInUpdate(_radialIncrement, _node, true, _gizmo != null);

				if (_gizmo == null)
					UpdateDeltaV();
			}
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x - _radialIncrement, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
					_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_gizmo.DeltaV.x - _radialIncrement, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
					_node.solver.UpdateFlightPlan();
					UpdateDeltaV();
				}
			}
		}

		private void RadialUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnRadialOutUpdate(_radialIncrement, _node, true, _gizmo != null);

				if (_gizmo == null)
					UpdateDeltaV();
			}
			else
			{
				if (_gizmo != null)
				{
					_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x + _radialIncrement, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
					_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
				}
				else
				{
					_node.DeltaV = new Vector3d(_gizmo.DeltaV.x + _radialIncrement, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
					_node.solver.UpdateFlightPlan();
					UpdateDeltaV();
				}
			}
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

			_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node DeltaV: {0:N1}m/s", _node.DeltaV.magnitude));
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("Prograde: {0:N1}m/s", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("Normal: {0:N1}m/s", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("Radial: {0:N1}m/s", _node.DeltaV.x));
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
