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

		private ManeuverInput _inputPanel;

		private Vector3d _startDeltaV;

		private bool _isVisible;
		private UIWorldPointer _pointer;

		private Button _inputButton;
		private RectTransform _buttonRect;
		private RectTransform _panelRect;

		private void Awake()
		{

		}

		private void OnDestroy()
		{
			if (_inputPanel != null)
				Destroy(_inputPanel);

			if (_pointer != null)
				_pointer.Terminate();
		}

		public void setup(ManeuverNode node, ManeuverGizmo gizmo, int i)
		{
			if (node == null)
				return;

			_node = node;
			_gizmo = gizmo;
			_index = i;

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

			_inputButton.onClick.AddListener(new UnityAction(ToggleUI));
			_inputButton.interactable = true;

			gizmo.minusOrbitbtn.onClick.RemoveAllListeners();
			oldRect.localScale = new Vector3(0.000001f, 0.000001f, 0.000001f);
		}

		public void UpdateDeltaV()
		{
			if (!_isVisible)
				return;

			_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}m/s", _index + 1, _node.DeltaV.magnitude));
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("Prograde: {0:N1}m/s", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("Normal: {0:N1}m/s", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("Radial: {0:N1}m/s", _node.DeltaV.x));
		}

		private void ToggleUI()
		{
			ManeuverController.maneuverLog("Toggle Input", logLevels.log);
			if (_isVisible)
			{
				ManeuverController.maneuverLog("Toggle Input 1", logLevels.log);
				if (_inputPanel != null)
					Destroy(_inputPanel);

				_inputPanel = null;

				if (_pointer != null)
					_pointer.Terminate();

				_isVisible = false;
			}
			else
			{
				ManeuverController.maneuverLog("Toggle Input 2", logLevels.log);
				openUI();

				_startDeltaV = _node.DeltaV;

				attachUI();

				reposition();

				attachPointer();

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

			_inputPanel.DragEvent.AddListener(new UnityAction<RectTransform>(clampToScreen));
			_inputPanel.MouseOverEvent.AddListener(new UnityAction<bool>(SetMouseOverGizmo));

			_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: {1:N1}m/s", _index + 1, _node.DeltaV.magnitude));
			_inputPanel.ResetText.OnTextUpdate.Invoke(string.Format("{0:N1}m/s", _startDeltaV.magnitude));
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("Prograde: {0:N1}m/s", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("Normal: {0:N1}m/s", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("Radial: {0:N1}m/s", _node.DeltaV.x));
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
			if (_gizmo == null)
				return;

			_gizmo.SetMouseOverGizmo(isOver);
		}

		private void ProgradeDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.ignoreSensitivity = true;
				_gizmo.handleRetrograde.OnHandleUpdate(_inputPanel.prograde_increment);
			}
			else
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z - _inputPanel.prograde_increment);
				_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
		}

		private void ProgradeUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnProgradeUpdate(_inputPanel.prograde_increment, _node, true, true);
				//ManeuverController.Instance.ignoreSensitivity = true;
				//_gizmo.handlePrograde.OnHandleUpdate(_inputPanel.prograde_increment);
			}
			else
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y, _gizmo.DeltaV.z + _inputPanel.prograde_increment);
				_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
		}

		private void NormalDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.ignoreSensitivity = true;
				_gizmo.handleAntiNormal.OnHandleUpdate(_inputPanel.normal_increment);
			}
			else
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y - _inputPanel.normal_increment, _gizmo.DeltaV.z);
				_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
		}

		private void NormalUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.OnNormalUpdate(_inputPanel.normal_increment, _node, true, true);
				//ManeuverController.Instance.ignoreSensitivity = true;
				//_gizmo.handleNormal.OnHandleUpdate(_inputPanel.normal_increment);
			}
			else
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x, _gizmo.DeltaV.y + _inputPanel.normal_increment, _gizmo.DeltaV.z);
				_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
		}

		private void RadialDown()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.ignoreSensitivity = true;
				_gizmo.handleRadialIn.OnHandleUpdate(_inputPanel.radial_increment);
			}
			else
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x - _inputPanel.radial_increment, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
				_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
		}

		private void RadialUp()
		{
			if (ManeuverController.Instance.settings.alignToOrbit)
			{
				ManeuverController.Instance.ignoreSensitivity = true;
				_gizmo.handleRadialOut.OnHandleUpdate(_inputPanel.radial_increment);
			}
			else
			{
				_gizmo.DeltaV = new Vector3d(_gizmo.DeltaV.x + _inputPanel.radial_increment, _gizmo.DeltaV.y, _gizmo.DeltaV.z);
				_gizmo.OnGizmoUpdated(_gizmo.DeltaV, _node.UT);
			}
		}

		private void ResetManeuver()
		{
			_node.OnGizmoUpdated(_startDeltaV, _node.UT);
			_gizmo.DeltaV = _startDeltaV;

			_inputPanel.TotaldVText.OnTextUpdate.Invoke(string.Format("Maneuver Node DeltaV: {0:N1}m/s", _node.DeltaV.magnitude));
			_inputPanel.ProgradeText.OnTextUpdate.Invoke(string.Format("Prograde: {0:N1}m/s", _node.DeltaV.z));
			_inputPanel.NormalText.OnTextUpdate.Invoke(string.Format("Normal: {0:N1}m/s", _node.DeltaV.y));
			_inputPanel.RadialText.OnTextUpdate.Invoke(string.Format("Radial: {0:N1}m/s", _node.DeltaV.x));
		}
	}
}
