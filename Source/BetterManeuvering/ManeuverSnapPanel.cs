﻿using System;
using System.Collections.Generic;
using BetterManeuvering.Unity;
using KSP.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BetterManeuvering
{
	public class ManeuverSnapPanel : MonoBehaviour
	{
		private Orbit _patch;
		private ManeuverNode _node;
		private ManeuverGizmo _gizmo;
		private Vector3d _oldDeltaV;

		private double _startUT;
		private double apoUT, periUT, nextOUT, prevOUT, nextPUT, prevPUT, eqAscUT, eqDescUT, relAscUT, relDescUT, clAppUT;
		private bool _isVisible;
		private UIWorldPointer _pointer;
		private Transform _lineEndPoint;

		private Button _snapButton;
		private RectTransform _buttonRect;
		private RectTransform _panelRect;

		private ManeuverSnap _snapPanel;

		private double _lastUpdate;

		private void Awake()
		{

		}

		private void OnDestroy()
		{
			if (_snapPanel != null)
				Destroy(_snapPanel);

			if (_pointer != null)
				_pointer.Terminate();
		}

		private void Update()
		{
			if (!_isVisible)
				return;

			if (Math.Abs(_oldDeltaV.magnitude - _node.DeltaV.magnitude) > 10)
			{
				checkOrbit();
				UpdateTimers();
				return;
			}

			if (Time.time - _lastUpdate < 0.5)
				return;

			_lastUpdate = Time.time;
			UpdateTimers();
		}

		public void setup(ManeuverNode node, ManeuverGizmo gizmo)
		{
			if (node == null)
				return;

			_node = node;
			_gizmo = gizmo;
			_patch = _node.patch;
			_startUT = _node.UT;
			_oldDeltaV = _node.DeltaV;

			_snapButton = Instantiate<Button>(gizmo.plusOrbitBtn);
			_snapButton.transform.SetParent(gizmo.plusOrbitBtn.transform.parent);

			_buttonRect = _snapButton.GetComponent<RectTransform>();

			RectTransform oldRect = gizmo.plusOrbitBtn.GetComponent<RectTransform>();

			_buttonRect.anchoredPosition3D = oldRect.anchoredPosition3D;
			_buttonRect.position = oldRect.position;
			_buttonRect.localScale = oldRect.localScale;
			_buttonRect.rotation = oldRect.rotation;
			_buttonRect.localRotation = oldRect.localRotation;

			_snapButton.navigation = new Navigation() { mode = Navigation.Mode.None };

			_snapButton.onClick.AddListener(new UnityAction(ToggleUI));
			_snapButton.interactable = true;

			gizmo.plusOrbitBtn.onClick.RemoveAllListeners();
			oldRect.localScale = new Vector3(0.000001f, 0.000001f, 0.000001f);
		}

		private void ToggleUI()
		{
			if (_isVisible)
			{
				if (_snapPanel != null)
					Destroy(_snapPanel.gameObject);

				_snapPanel = null;

				if (_pointer != null)
					_pointer.Terminate();

				_isVisible = false;
			}
			else
			{
				openUI();

				_startUT = _node.UT;

				attachUI();

				checkOrbit();

				UpdateTimers();

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
			_pointer.transform.SetParent(_snapPanel.TitleTransform, true);
		}

		private void openUI()
		{
			if (ManeuverLoader.SnapPrefab == null)
				return;

			GameObject obj = Instantiate(ManeuverLoader.SnapPrefab);

			if (obj == null)
				return;

			obj.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

			_snapPanel = obj.GetComponent<ManeuverSnap>();

			if (_snapPanel == null)
				return;

			obj.SetActive(true);
		}

		private void attachUI()
		{
			if (_snapPanel == null)
				return;

			_snapPanel.NextOrbitButton.onClick.AddListener(new UnityAction(nextOrbit));
			_snapPanel.PreviousOrbitButton.onClick.AddListener(new UnityAction(previousOrbit));
			_snapPanel.ApoButton.onClick.AddListener(new UnityAction(apoapsis));
			_snapPanel.PeriButton.onClick.AddListener(new UnityAction(periapsis));
			_snapPanel.NextPatchButton.onClick.AddListener(new UnityAction(nextPatch));
			_snapPanel.PreviousPatchButton.onClick.AddListener(new UnityAction(previousPatch));
			_snapPanel.EqAscButton.onClick.AddListener(new UnityAction(eqAsc));
			_snapPanel.EqDescButton.onClick.AddListener(new UnityAction(eqDesc));
			_snapPanel.RelAscButton.onClick.AddListener(new UnityAction(relAsc));
			_snapPanel.RelDescButton.onClick.AddListener(new UnityAction(relDesc));
			_snapPanel.ClAppButton.onClick.AddListener(new UnityAction(clApp));
			_snapPanel.ResetButton.onClick.AddListener(new UnityAction(reset));

			_snapPanel.DragEvent.AddListener(new UnityAction<RectTransform>(clampToScreen));
			_snapPanel.MouseOverEvent.AddListener(new UnityAction<bool>(SetMouseOverGizmo));

			double UT = Planetarium.GetUniversalTime();

			_snapPanel.CurrentTime.OnTextUpdate.Invoke(string.Format("Maneuver Node In: T {0}", KSPUtil.PrintTime(UT - _node.UT, 3, true)));
			_snapPanel.ResetTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(_startUT - UT, 3, false)));
		}

		private void reposition()
		{
			if (_snapPanel == null)
				return;

			_panelRect = _snapPanel.GetComponent<RectTransform>();

			Vector2 cam = PlanetariumCamera.Camera.WorldToScreenPoint(_buttonRect.position);
			
			cam.x -= (Screen.width / 2);
			cam.y -= (Screen.height / 2);

			cam.x += 75;
			cam.y -= ((_panelRect.sizeDelta.y / 2) + 50);

			_panelRect.position = cam;

			clampToScreen(_panelRect);

			_lineEndPoint = new GameObject().transform;

			_lineEndPoint.position = FlightCamera.fetch.mainCamera.ScreenToWorldPoint(Input.mousePosition);
			_lineEndPoint.SetParent(_snapButton.transform, true);
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

		private void UpdateTimers()
		{
			double UT = Planetarium.GetUniversalTime();

			_snapPanel.CurrentTime.OnTextUpdate.Invoke(string.Format("Maneuver Node In: T {0}", KSPUtil.PrintTime(UT - _node.UT, 3, true)));
			_snapPanel.ResetTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(_startUT - UT, 3, false)));

			if (_snapPanel.Apo.activeSelf)
				_snapPanel.ApoTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(apoUT - UT, 3, false)));

			if (_snapPanel.Peri.activeSelf)
				_snapPanel.PeriTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(periUT - UT, 3, false)));

			if (_snapPanel.NextOrbit.activeSelf)
				_snapPanel.NOTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(nextOUT - UT, 3, false)));

			if (_snapPanel.PreviousOrbit.activeSelf)
				_snapPanel.POTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(prevOUT - UT, 3, false)));

			if (_snapPanel.NextPatch.activeSelf)
				_snapPanel.NPTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(nextPUT - UT, 3, false)));

			if (_snapPanel.PreviousPatch.activeSelf)
				_snapPanel.PPTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(prevPUT - UT, 3, false)));

			if (_snapPanel.EqAsc.activeSelf)
				_snapPanel.EqAscTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(eqAscUT - UT, 3, false)));

			if (_snapPanel.EqDesc.activeSelf)
				_snapPanel.EqDescTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(eqDescUT - UT, 3, false)));

			if (_snapPanel.RelAsc.activeSelf)
				_snapPanel.RelAscTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(relAscUT - UT, 3, false)));

			if (_snapPanel.RelDesc.activeSelf)
				_snapPanel.RelDescTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(relDescUT - UT, 3, false)));

			if (_snapPanel.ClApp.activeSelf)
				_snapPanel.ApproachTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(clAppUT - UT, 3, false)));

		}

		private void checkOrbit()
		{
			if (_patch == null || _snapPanel == null)
				return;

			double UT = Planetarium.GetUniversalTime();

			_snapPanel.CurrentTime.OnTextUpdate.Invoke(string.Format("Maneuver Node In: T {0}", KSPUtil.PrintTime(UT - _node.UT, 3, true)));
			_snapPanel.ResetTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(_startUT - UT, 3, false)));

			if (_patch.eccentricity >= 1)
			{
				_snapPanel.Apo.SetActive(false);
				_snapPanel.NextOrbit.SetActive(false);
				_snapPanel.PreviousOrbit.SetActive(false);

				if (_patch.timeToPe < 0)
					_snapPanel.Peri.SetActive(false);
				else if (_patch.PeR < 0)
					_snapPanel.Peri.SetActive(false);
				else if (_patch.UTsoi > 0 && _patch.timeToPe + _patch.StartUT > _patch.UTsoi)
					_snapPanel.Peri.SetActive(false);
				else
				{
					_snapPanel.Peri.SetActive(true);
					periUT = _patch.StartUT + _patch.timeToPe;
				}

				eqAscUT = ManeuverUtilities.EqAscTime(_patch);

				if (_patch.UTsoi > 0 && eqAscUT > _patch.UTsoi)
					_snapPanel.EqAsc.SetActive(false);
				else if (eqAscUT < UT)
					_snapPanel.EqAsc.SetActive(false);
				else
					_snapPanel.EqAsc.SetActive(true);

				eqDescUT = ManeuverUtilities.EqDescTime(_patch);

				if (_patch.UTsoi > 0 && eqDescUT > _patch.UTsoi)
					_snapPanel.EqDesc.SetActive(false);
				else if (eqDescUT < UT)
					_snapPanel.EqDesc.SetActive(false);
				else
					_snapPanel.EqDesc.SetActive(true);

				if (_patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
					_snapPanel.NextPatch.SetActive(false);
				else
				{
					_snapPanel.NextPatch.SetActive(true);

					if (_patch.nextPatch.nextPatch.UTsoi > 0)
						nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
					else if (_patch.nextPatch.nextPatch.eccentricity < 1)
						nextPUT = _patch.nextPatch.nextPatch.StartUT + (_patch.nextPatch.nextPatch.period / 2);
					else
						nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.EndUT - _patch.nextPatch.nextPatch.StartUT) / 2);
				}

				if (_patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
					_snapPanel.PreviousPatch.SetActive(false);
				else
				{
					_snapPanel.PreviousPatch.SetActive(true);

					if (_patch.previousPatch.UTsoi > 0)
						prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
					else if (_patch.previousPatch.eccentricity < 1)
						prevPUT = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
					else
						prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);
				}

				ITargetable target = FlightGlobals.fetch.VesselTarget;

				if (target == null)
				{
					_snapPanel.RelAsc.SetActive(false);
					_snapPanel.RelDesc.SetActive(false);
					_snapPanel.ClApp.SetActive(false);
				}
				else
				{
					Orbit tgtPatch = target.GetOrbit();

					if (tgtPatch.referenceBody != _patch.referenceBody)
					{
						_snapPanel.RelAsc.SetActive(false);
						_snapPanel.RelDesc.SetActive(false);
						_snapPanel.ClApp.SetActive(false);
					}
					else
					{
						relAscUT = ManeuverUtilities.RelAscTime(_patch, target);

						if (_patch.UTsoi > 0 && relAscUT > _patch.UTsoi)
							_snapPanel.RelAsc.SetActive(false);
						else if (relAscUT < UT)
							_snapPanel.RelAsc.SetActive(false);
						else
							_snapPanel.RelAsc.SetActive(true);

						relDescUT = ManeuverUtilities.RelDescTime(_patch, target);

						if (_patch.UTsoi > 0 && relDescUT > _patch.UTsoi)
							_snapPanel.RelDesc.SetActive(false);
						else if (relDescUT < UT)
							_snapPanel.RelDesc.SetActive(false);
						else
							_snapPanel.RelDesc.SetActive(true);

						if (target.GetVessel() == null)
							clAppUT = _patch.closestTgtApprUT;
						else
							clAppUT = ManeuverUtilities.closestVessel(0, _patch, tgtPatch, true, 0, 0);

						if (clAppUT <= 0)
							_snapPanel.ClApp.SetActive(false);
						else if (_patch.UTsoi > 0 && clAppUT > _patch.UTsoi)
							_snapPanel.ClApp.SetActive(false);
						else if (clAppUT < UT)
							_snapPanel.ClApp.SetActive(false);
						else
							_snapPanel.ClApp.SetActive(true);
					}
				}
			}
			else
			{
				if (_patch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
				{
					_snapPanel.NextOrbit.SetActive(true);
					nextOUT = _node.UT + _patch.period;

					_snapPanel.Apo.SetActive(true);
					apoUT = _patch.StartUT + _patch.timeToAp;

					_snapPanel.Peri.SetActive(true);
					periUT = _patch.StartUT + _patch.timeToPe;

					_snapPanel.EqAsc.SetActive(true);
					eqAscUT = ManeuverUtilities.EqAscTime(_patch);

					_snapPanel.EqDesc.SetActive(true);
					eqDescUT = ManeuverUtilities.EqDescTime(_patch);

					if (_node.UT - _patch.period < UT)
						_snapPanel.PreviousOrbit.SetActive(false);
					else
					{
						_snapPanel.PreviousOrbit.SetActive(true);
						prevOUT = _node.UT - _patch.period;
					}

					if (_patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER)
					{
						_snapPanel.NextPatch.SetActive(true);

						if (_patch.nextPatch.nextPatch.UTsoi > 0)
							nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
						else if (_patch.nextPatch.nextPatch.eccentricity < 1)
							nextPUT = _patch.nextPatch.nextPatch.StartUT + (_patch.nextPatch.nextPatch.period / 2);
						else
							nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.EndUT - _patch.nextPatch.nextPatch.StartUT) / 2);
					}
					else
						_snapPanel.NextPatch.SetActive(false);

					if (_patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
						_snapPanel.PreviousPatch.SetActive(false);
					else
					{
						_snapPanel.PreviousPatch.SetActive(true);

						if (_patch.previousPatch.UTsoi > 0)
							prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
						else if (_patch.previousPatch.eccentricity < 1)
							prevPUT = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
						else
							prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);
					}

					ITargetable target = FlightGlobals.fetch.VesselTarget;

					if (target == null)
					{
						_snapPanel.RelAsc.SetActive(false);
						_snapPanel.RelDesc.SetActive(false);
						_snapPanel.ClApp.SetActive(false);
					}
					else
					{
						Orbit tgtPatch = target.GetOrbit();

						if (tgtPatch.referenceBody != _patch.referenceBody)
						{
							_snapPanel.RelAsc.SetActive(false);
							_snapPanel.RelDesc.SetActive(false);
							_snapPanel.ClApp.SetActive(false);
						}
						else
						{
							_snapPanel.RelAsc.SetActive(true);
							relAscUT = ManeuverUtilities.RelAscTime(_patch, target);

							_snapPanel.RelDesc.SetActive(true);
							relDescUT = ManeuverUtilities.RelDescTime(_patch, target);

							if (target.GetVessel() == null)
								clAppUT = _patch.closestTgtApprUT;
							else
								clAppUT = ManeuverUtilities.closestVessel(0, _patch, tgtPatch, true, 0, 0);

							if (clAppUT <= 0)
								_snapPanel.ClApp.SetActive(false);
							else
								_snapPanel.ClApp.SetActive(true);
						}
					}
				}
				else
				{
					_snapPanel.NextOrbit.SetActive(false);
					_snapPanel.PreviousOrbit.SetActive(false);

					if (_patch.timeToAp < 0)
						_snapPanel.Apo.SetActive(false);
					if (_patch.UTsoi > 0 && _patch.timeToAp + _patch.StartUT > _patch.UTsoi)
						_snapPanel.Apo.SetActive(false);
					if (_patch.ApA > _patch.referenceBody.sphereOfInfluence)
						_snapPanel.Apo.SetActive(false);
					else
					{
						_snapPanel.Apo.SetActive(true);
						apoUT = _patch.StartUT + _patch.timeToAp;
					}

					if (_patch.timeToPe < 0)
						_snapPanel.Peri.SetActive(false);
					else if (_patch.PeR < 0)
						_snapPanel.Peri.SetActive(false);
					else if (_patch.UTsoi > 0 && _patch.timeToPe + _patch.StartUT > _patch.UTsoi)
						_snapPanel.Peri.SetActive(false);
					else
					{
						_snapPanel.Peri.SetActive(true);
						periUT = _patch.StartUT + _patch.timeToPe;
					}

					eqAscUT = ManeuverUtilities.EqAscTime(_patch);

					if (_patch.UTsoi > 0 && eqAscUT > _patch.UTsoi)
						_snapPanel.EqAsc.SetActive(false);
					else if (eqAscUT < UT)
						_snapPanel.EqAsc.SetActive(false);
					else
						_snapPanel.EqAsc.SetActive(true);

					eqDescUT = ManeuverUtilities.EqDescTime(_patch);

					if (_patch.UTsoi > 0 && eqDescUT > _patch.UTsoi)
						_snapPanel.EqDesc.SetActive(false);
					else if (eqDescUT < UT)
						_snapPanel.EqDesc.SetActive(false);
					else
						_snapPanel.EqDesc.SetActive(true);

					if (_patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE)
					{
						_snapPanel.NextPatch.SetActive(true);

						if (_patch.nextPatch.nextPatch.UTsoi > 0)
							nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
						else if (_patch.nextPatch.nextPatch.eccentricity < 1)
							nextPUT = _patch.nextPatch.nextPatch.StartUT + (_patch.nextPatch.nextPatch.period / 2);
						else
							nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.EndUT - _patch.nextPatch.nextPatch.StartUT) / 2);
					}
					else
						_snapPanel.NextPatch.SetActive(false);

					if (_patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
						_snapPanel.PreviousPatch.SetActive(false);
					else
					{
						_snapPanel.PreviousPatch.SetActive(true);

						if (_patch.previousPatch.UTsoi > 0)
							prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
						else if (_patch.previousPatch.eccentricity < 1)
							prevPUT = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
						else
							prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);
					}

					ITargetable target = FlightGlobals.fetch.VesselTarget;

					if (target == null)
					{
						_snapPanel.RelAsc.SetActive(false);
						_snapPanel.RelDesc.SetActive(false);
						_snapPanel.ClApp.SetActive(false);
					}
					else
					{
						Orbit tgtPatch = target.GetOrbit();

						if (tgtPatch.referenceBody != _patch.referenceBody)
						{
							_snapPanel.RelAsc.SetActive(false);
							_snapPanel.RelDesc.SetActive(false);
							_snapPanel.ClApp.SetActive(false);
						}
						else
						{
							relAscUT = ManeuverUtilities.RelAscTime(_patch, target);

							if (_patch.UTsoi > 0 && relAscUT > _patch.UTsoi)
								_snapPanel.RelAsc.SetActive(false);
							else if (relAscUT < UT)
								_snapPanel.RelAsc.SetActive(false);
							else
								_snapPanel.RelAsc.SetActive(true);

							relDescUT = ManeuverUtilities.RelDescTime(_patch, target);

							if (_patch.UTsoi > 0 && relDescUT > _patch.UTsoi)
								_snapPanel.RelDesc.SetActive(false);
							else if (relDescUT < UT)
								_snapPanel.RelDesc.SetActive(false);
							else
								_snapPanel.RelDesc.SetActive(true);

							if (target.GetVessel() == null)
								clAppUT = _patch.closestTgtApprUT;
							else
								clAppUT = ManeuverUtilities.closestVessel(0, _patch, tgtPatch, true, 0, 0);

							if (clAppUT <= 0)
								_snapPanel.ClApp.SetActive(false);
							else if (_patch.UTsoi > 0 && clAppUT > _patch.UTsoi)
								_snapPanel.ClApp.SetActive(false);
							else if (clAppUT < UT)
								_snapPanel.ClApp.SetActive(false);
							else
								_snapPanel.ClApp.SetActive(true);
						}
					}
				}
			}
		}

		private void setNodeTime(double time)
		{
			_gizmo.UT = time;
			_gizmo.OnGizmoUpdated(_node.DeltaV, time);
			_patch = _node.patch;
			_oldDeltaV = _node.DeltaV;

			checkOrbit();

			UpdateTimers();
		}

		private void nextOrbit()
		{
			if (_patch == null || _patch.eccentricity >= 1)
				return;

			setNodeTime(_node.UT + _patch.period);
		}

		private void previousOrbit()
		{
			if (_patch == null || _patch.eccentricity >= 1)
				return;

			double time = _node.UT - _patch.period;

			if (time < Planetarium.GetUniversalTime())
				return;

			setNodeTime(time);
		}

		private void nextPatch()
		{
			if (_patch == null || _patch.nextPatch == null || _patch.nextPatch.nextPatch == null || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.IMPACT)
				return;

			double time = 0;

			if (_patch.nextPatch.nextPatch.UTsoi > 0)
				time = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
			else if (_patch.nextPatch.nextPatch.eccentricity < 1)
				time = _patch.nextPatch.nextPatch.StartUT + (_patch.nextPatch.nextPatch.period / 2);
			else
				time = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.EndUT - _patch.nextPatch.nextPatch.StartUT) / 2);

			setNodeTime(time);
		}

		private void previousPatch()
		{
			if (_patch == null || _patch.previousPatch == null || _patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
				return;

			double time = 0;

			if (_patch.previousPatch.UTsoi > 0)
				time = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
			else if (_patch.previousPatch.eccentricity < 1)
				time = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
			else
				time = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);

			setNodeTime(time);
		}

		private void apoapsis()
		{
			if (_patch == null || _patch.eccentricity >= 1)
				return;

			if (_patch.timeToAp < 0)
				return;
			if (_patch.UTsoi > 0 && _patch.timeToAp + _patch.StartUT > _patch.UTsoi)
				return;
			if (_patch.ApA > _patch.referenceBody.sphereOfInfluence)
				return;
			else
				setNodeTime(_patch.StartUT + _patch.timeToAp);
		}

		private void periapsis()
		{
			if (_patch == null || _patch.PeR < 0 || _patch.timeToPe < 0)
				return;


			if (_patch.timeToPe < 0)
				return;
			else if (_patch.PeR < 0)
				return;
			else if (_patch.UTsoi > 0 && _patch.timeToPe + _patch.StartUT > _patch.UTsoi)
				return;
			else
				setNodeTime(_patch.StartUT + _patch.timeToPe);
		}

		private void eqAsc()
		{
			if (_patch == null)
				return;

			double eqAsc = ManeuverUtilities.EqAscTime(_patch);
			
			if (_patch.UTsoi > 0 && eqAsc > _patch.UTsoi)
				return;
			else if (eqAsc < Planetarium.GetUniversalTime())
				return;
			else
				setNodeTime(eqAsc);
		}

		private void eqDesc()
		{
			if (_patch == null)
				return;

			double eqDesc = ManeuverUtilities.EqDescTime(_patch);

			if (_patch.UTsoi > 0 && eqDesc > _patch.UTsoi)
				return;
			else if (eqDesc < Planetarium.GetUniversalTime())
				return;
			else
				setNodeTime(eqDesc);
		}

		private void relAsc()
		{
			if (_patch == null)
				return;

			ITargetable tgt = FlightGlobals.fetch.VesselTarget;

			if (tgt == null)
				return;

			if (_patch.referenceBody != tgt.GetOrbit().referenceBody)
				return;

			double relAsc = ManeuverUtilities.RelAscTime(_patch, tgt);

			if (_patch.UTsoi > 0 && relAsc > _patch.UTsoi)
				return;
			else if (relAsc < Planetarium.GetUniversalTime())
				return;
			else
				setNodeTime(relAsc);
		}

		private void relDesc()
		{
			if (_patch == null)
				return;

			ITargetable tgt = FlightGlobals.fetch.VesselTarget;

			if (tgt == null)
				return;

			if (_patch.referenceBody != tgt.GetOrbit().referenceBody)
				return;

			double relDesc = ManeuverUtilities.RelDescTime(_patch, tgt);

			if (_patch.UTsoi > 0 && relDesc > _patch.UTsoi)
				return;
			else if (relDesc < Planetarium.GetUniversalTime())
				return;
			else
				setNodeTime(relDesc);
		}

		private void clApp()
		{
			if (_patch == null)
				return;

			ITargetable tgt = FlightGlobals.fetch.VesselTarget;

			if (tgt == null)
				return;

			double clApp = 0;

			if (tgt.GetVessel() == null)
				clApp = _patch.closestTgtApprUT;
			else
				clApp = ManeuverUtilities.closestVessel(0, _patch, tgt.GetOrbit(), true, 0, 0);

			if (clApp <= 0)
				return;

			if (_patch.UTsoi > 0 && clApp > _patch.UTsoi)
				return;
			else if (clApp < Planetarium.GetUniversalTime())
				return;
			else
				setNodeTime(clApp);
		}

		private void reset()
		{
			if (_patch == null)
				return;

			setNodeTime(_startUT);
		}
	}
}
