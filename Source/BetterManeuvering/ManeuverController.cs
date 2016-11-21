using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI.Screens.Mapview.MapContextMenuOptions;

namespace BetterManeuvering
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ManeuverController : MonoBehaviour
    {
		private static ManeuverController instance;

		public static EventData<PopupDialog> onPopupSpawn = new EventData<PopupDialog>("onPopupSpawn");
		public static EventData<PopupDialog> onPopupDestroy = new EventData<PopupDialog>("onPopupDestroy");
		public static EventData<ManeuverGizmo> onGizmoSpawn = new EventData<ManeuverGizmo>("onGizmoSpawn");
		public static EventData<ManeuverGizmo> onGizmoDestroy = new EventData<ManeuverGizmo>("onGizmoDestroy");

		public ManeuverGameParams settings;
		
		private KeyCode shortcut = KeyCode.N;

		private Camera camera;
		private PopupDialog maneuverDialog;
		private ManeuverNode currentNode;
		private ManeuverGizmo currentGizmo;
		private PatchedConicRenderer pcr;
		private float originalScale;

		private int lastManeuverIndex;

		private int accuracy = 2;

		private ManeuverGizmoHandle.HandleUpdate cachedPrograde;
		private ManeuverGizmoHandle.HandleUpdate cachedRetrograde;
		private ManeuverGizmoHandle.HandleUpdate cachedNormal;
		private ManeuverGizmoHandle.HandleUpdate cachedAntiNormal;
		private ManeuverGizmoHandle.HandleUpdate cachedRadialIn;
		private ManeuverGizmoHandle.HandleUpdate cachedRadialOut;

		private Quaternion referenceRotation;
		private Quaternion orbitRotation;

		private ManeuverInputPanel inputPanel;
		private ManeuverSnapPanel snapPanel;

		public bool ignoreSensitivity;

		public static ManeuverController Instance
		{
			get { return instance; }
		}

		private void Awake()
		{
			if (instance != null)
				Destroy(gameObject);

			instance = this;

			if (ManeuverPersistence.Instance != null)
				shortcut = ManeuverPersistence.Instance.keyboardShortcut;
		}

		private void Start()
		{
			StartCoroutine(attachPopupListener());
			StartCoroutine(attachGizmoListener());
			StartCoroutine(startup());

			onPopupSpawn.Add(popupSpawn);
			onPopupDestroy.Add(popupDestroy);
			onGizmoSpawn.Add(gizmoSpawn);
			onGizmoDestroy.Add(gizmoDestroy);
			GameEvents.onVesselChange.Add(onVesselChange);
			GameEvents.OnMapEntered.Add(enterMap);
			GameEvents.OnMapExited.Add(exitMap);
			GameEvents.OnGameSettingsApplied.Add(SettingsApplied);

			settings = HighLogic.CurrentGame.Parameters.CustomParams<ManeuverGameParams>();

			switch(settings.accuracy)
			{
				case 0:
					accuracy = 4;
					break;
				case 1:
					accuracy = 2;
					break;
				case 2:
					accuracy = 1;
					break;
				default:
					accuracy = 2;
					break;
			}
		}

		private void OnDestroy()
		{
			instance = null;

			onPopupSpawn.Remove(popupSpawn);
			onPopupDestroy.Remove(popupDestroy);
			onGizmoSpawn.Remove(gizmoSpawn);
			onGizmoDestroy.Remove(gizmoDestroy);
			GameEvents.onVesselChange.Remove(onVesselChange);
			GameEvents.OnMapEntered.Remove(enterMap);
			GameEvents.OnMapExited.Remove(exitMap);
			GameEvents.OnGameSettingsApplied.Remove(SettingsApplied);
		}

		private void Update()
		{
			if (snapPanel == null && inputPanel == null)
				return;

			if (currentNode == null || currentNode.attachedGizmo == null)
			{
				Destroy(snapPanel);
				Destroy(inputPanel);

				snapPanel = null;
				inputPanel = null;
			}
		}

		private void LateUpdate()
		{
			//if (currentNode == null)
			//	return;

			if (MapView.MapIsEnabled)
			{
				if (settings.useKeyboard)
				{
					if (Input.GetKeyDown(shortcut) && GameSettings.MODIFIER_KEY.GetKey(false))
					{
						if (pcr != null && pcr.solver != null && pcr.solver.maneuverNodes.Count > 0 && InputLockManager.IsUnlocked(ControlTypes.MAP_UI))
						{
							if (currentNode == null || currentNode.attachedGizmo == null)
							{
								if (MapView.MapCamera.target.type == MapObject.ObjectType.ManeuverNode)
								{
									if (MapView.MapCamera.target.maneuverNode.attachedGizmo == null)
									{
										pcr.SetMouseOverGizmo(true);
										MapView.MapCamera.target.maneuverNode.AttachGizmo(MapView.ManeuverNodePrefab, pcr);
									}
								}
								else if (pcr.solver.maneuverNodes.Count > lastManeuverIndex)
								{
									if (pcr.solver.maneuverNodes[lastManeuverIndex].attachedGizmo == null)
									{
										pcr.SetMouseOverGizmo(true);
										pcr.solver.maneuverNodes[lastManeuverIndex].AttachGizmo(MapView.ManeuverNodePrefab, pcr);
									}
								}
								else
								{
									if (pcr.solver.maneuverNodes[0].attachedGizmo == null)
									{
										pcr.SetMouseOverGizmo(true);
										pcr.solver.maneuverNodes[0].AttachGizmo(MapView.ManeuverNodePrefab, pcr);
									}
								}
							}
							else
								currentNode.DetachGizmo();
						}
					}
				}

				if (camera != null)
				{
					if (currentGizmo != null)
					{
						if (settings.dynamicScaling)
						{
							if (PlanetariumCamera.fetch != null)
							{
								float distance = (PlanetariumCamera.Camera.transform.position - currentGizmo.transform.position).magnitude;
								
								if (distance < 0)
									distance = 0;

								float lerp = 2 / (1 + Mathf.Exp((-1 * distance * 0.0001f)));

								float scale = Mathf.Lerp(settings.baseScale, settings.maxScale < settings.baseScale ? settings.baseScale + 0.1f : settings.maxScale, lerp - 1);

								currentGizmo.screenSize = originalScale * scale;
							}
						}
					}
				}
			}
		}

		private IEnumerator startup()
		{
			while (!FlightGlobals.ready)
				yield return null;

			while (FlightGlobals.ActiveVessel == null)
				yield return null;

			while (FlightGlobals.ActiveVessel.patchedConicRenderer == null)
				yield return null;

			pcr = FlightGlobals.ActiveVessel.patchedConicRenderer;
		}

		private IEnumerator attachPopupListener()
		{
			while (PopupDialogController.Instance == null)
				yield return null;

			if (PopupDialogController.PopupDialogBase == null)
				yield break;

			PopupDialogController.PopupDialogBase.gameObject.AddOrGetComponent<ManeuverPopupDialogListener>();
		}

		private IEnumerator attachGizmoListener()
		{
			while (MapView.fetch == null)
				yield return null;

			if (MapView.ManeuverNodePrefab == null)
				yield break;

			MapView.ManeuverNodePrefab.AddOrGetComponent<ManeuverGizmoListener>();
		}

		public void SettingsApplied()
		{
			settings = HighLogic.CurrentGame.Parameters.CustomParams<ManeuverGameParams>();

			switch (settings.accuracy)
			{
				case 0:
					accuracy = 4;
					break;
				case 1:
					accuracy = 2;
					break;
				case 2:
					accuracy = 1;
					break;
				default:
					accuracy = 2;
					break;
			}
		}

		private void onVesselChange(Vessel v)
		{
			StartCoroutine(startup());
		}

		private void popupSpawn(PopupDialog dialog)
		{
			if (dialog == null)
				return;

			if (dialog.dialogToDisplay == null)
				return;

			maneuverDialog = dialog;

			MultiOptionDialog multi = dialog.dialogToDisplay;

			if (multi.Options.Length != 2)
				return;

			if (multi.Options[0].OptionText != "Add Maneuver")
				return;

			if (!(multi.Options[0] is AddManeuver))
				return;

			double UT = ManeuverUtilities.parseTime(multi.title);

			parseOrbit(UT, multi);
		}

		private void parseOrbit(double UT, MultiOptionDialog multi)
		{
			if (maneuverDialog == null)
				return;

			PatchedConicRenderer pcr = FlightGlobals.ActiveVessel.patchedConicRenderer;

			if (pcr == null)
				return;

			Orbit refPatch = ManeuverUtilities.getRefPatch(UT, pcr.solver);

			if (refPatch == null)
				return;

			List<AddManeuver> newManeuvers = new List<AddManeuver>();

			double newUT = 0;

			double trueAnomaly = refPatch.TrueAnomalyAtUT(UT) * Mathf.Rad2Deg;

			double minAnomalyTime = refPatch.GetUTforTrueAnomaly((trueAnomaly - 10 < -180 ? trueAnomaly -10 + 360 : trueAnomaly - 10) * Mathf.Deg2Rad, 0);
			double maxAnomalyTime = refPatch.GetUTforTrueAnomaly((trueAnomaly + 10 > 180 ? trueAnomaly + 10 - 360 : trueAnomaly + 10) * Mathf.Deg2Rad, 0);

			if (minAnomalyTime > UT && refPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
				minAnomalyTime -= refPatch.period;

			double minAnomalyTimeWide = refPatch.GetUTforTrueAnomaly((trueAnomaly - 15 < -180 ? trueAnomaly - 15 + 360 : trueAnomaly - 15) * Mathf.Deg2Rad, 0);
			double maxAnomalyTimeWide = refPatch.GetUTforTrueAnomaly((trueAnomaly + 15 > 180 ? trueAnomaly + 15 - 360 : trueAnomaly + 15) * Mathf.Deg2Rad, 0);

			if (minAnomalyTimeWide > UT && refPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
				minAnomalyTimeWide -= refPatch.period;

			//maneuverLog("UT: {0:F2} - TA: {1:F2} - MinTA: {2:F2} - MinTAUT: {3:F2} - MaxTA: {4:F2} - MaxTAUT: {5:F2}", logLevels.log, UT, trueAnomaly,  trueAnomaly - 10 < -180 ? trueAnomaly - 10 + 360 : trueAnomaly - 10, minAnomalyTime, trueAnomaly + 10 > 180 ? trueAnomaly + 10 - 360 : trueAnomaly + 10, maxAnomalyTime);

			if (ManeuverUtilities.parseApo(UT, refPatch, minAnomalyTime, maxAnomalyTime, out newUT))
			{
				AddManeuver man = new AddManeuver(pcr, newUT);

				man.OptionText = "Add Maneuver To Apoapsis";

				newManeuvers.Add(man);
			}

			if (ManeuverUtilities.parsePeri(UT, refPatch, minAnomalyTime, maxAnomalyTime, out newUT))
			{
				AddManeuver man = new AddManeuver(pcr, newUT);

				man.OptionText = "Add Maneuver To Periapsis";

				newManeuvers.Add(man);
			}

			if (ManeuverUtilities.parseEqAsc(UT, refPatch, minAnomalyTimeWide, maxAnomalyTimeWide, out newUT))
			{
				AddManeuver man = new AddManeuver(pcr, newUT);

				man.OptionText = "Add Maneuver To Eq Asc Node";

				newManeuvers.Add(man);
			}

			if (ManeuverUtilities.parseEqDesc(UT, refPatch, minAnomalyTimeWide, maxAnomalyTimeWide, out newUT))
			{
				AddManeuver man = new AddManeuver(pcr, newUT);

				man.OptionText = "Add Maneuver To Eq Desc Node";

				newManeuvers.Add(man);
			}

			ITargetable tgt = FlightGlobals.fetch.VesselTarget;

			if (tgt != null)
			{
				if (ManeuverUtilities.parseRelAsc(UT, refPatch, tgt, minAnomalyTime, maxAnomalyTime, out newUT))
				{
					AddManeuver man = new AddManeuver(pcr, newUT);

					man.OptionText = "Add Maneuver To Asc Node";

					newManeuvers.Add(man);
				}

				if (ManeuverUtilities.parseRelDesc(UT, refPatch, tgt, minAnomalyTime, maxAnomalyTime, out newUT))
				{
					AddManeuver man = new AddManeuver(pcr, newUT);

					man.OptionText = "Add Maneuver To Desc Node";

					newManeuvers.Add(man);
				}

				Orbit tgtPatch = null;

				Vessel tgtVessel = tgt.GetVessel();

				if (tgtVessel == null)
					tgtPatch = tgt.GetOrbit();
				else
				{
					if (!tgtVessel.LandedOrSplashed)
					{
						if (tgtVessel.PatchedConicsAttached)
						{
							int l = tgtVessel.patchedConicSolver.patchesAhead;

							for (int i = 0; i <= l; i++)
							{
								Orbit patch = tgtVessel.patchedConicSolver.patches[i];

								if (patch.referenceBody == refPatch.referenceBody)
								{
									tgtPatch = patch;
									break;
								}
							}
						}
						else
							tgtPatch = tgt.GetOrbit();
					}
				}

				if (tgtPatch != null)
				{
					if (ManeuverUtilities.parseApproach(UT, refPatch, tgtPatch, tgtVessel != null, minAnomalyTime, maxAnomalyTime, out newUT))
					{
						AddManeuver man = new AddManeuver(pcr, newUT);

						man.OptionText = "Add Maneuver Closest Approach";

						newManeuvers.Add(man);
					}
				}
			}

			if (newManeuvers.Count > 0)
			{
				List<MapContextMenuOption> options = new List<MapContextMenuOption>();

				options.Add(multi.Options[0] as AddManeuver);
				options[0].OptionText = "Add Maneuver Here";

				for (int i = 0; i < newManeuvers.Count; i++)
					options.Add(newManeuvers[i]);

				options.Add(multi.Options[1] as AutoWarpToUT);

				RectTransform rect = maneuverDialog.GetComponent<RectTransform>();

				rect.position = new Vector3(rect.position.x, rect.position.y, -40);

				MultiOptionDialog newMulti = new MultiOptionDialog("", multi.title, MapView.OrbitIconsTextSkinDef, multi.dialogRect, options.ToArray());

				for (int i = multi.Options.Length - 1; i >= 0; i--)
					DestroyImmediate(multi.Options[i].uiItem);

				Stack<Transform> stack = new Stack<Transform>();

				stack.Push(maneuverDialog.popupWindow.transform);

				newMulti.Create(ref stack, MapView.OrbitIconsTextSkinDef);

				maneuverDialog.popupWindow.GetComponent<LayoutElement>().minHeight = newMulti.dialogRect.height;

				maneuverDialog.dialogToDisplay = newMulti;
				maneuverDialog.dialogToDisplay.Update();
				maneuverDialog.dialogToDisplay.Resize();
			}
		}

		private void popupDestroy(PopupDialog dialog)
		{
			maneuverDialog = null;
		}

		private void gizmoSpawn(ManeuverGizmo gizmo)
		{
			maneuverLog("Spawning Gizmo...", logLevels.log);
			if (gizmo == null)
				return;

			if (currentGizmo != null)
			{
				if (snapPanel != null)
					Destroy(snapPanel);

				if (inputPanel != null)
					Destroy(inputPanel);

				snapPanel = null;
				inputPanel = null;
			}

			currentGizmo = gizmo;

			//currentGizmo.OnMinimize = new Callback(DetachGizmo);

			originalScale = currentGizmo.screenSize;

			currentGizmo.screenSize = originalScale * settings.baseScale;

			//for (int i = currentGizmo.cameraFacingBillboards.Length - 1; i >= 0; i--)
			//{
			//	Transform t = currentGizmo.cameraFacingBillboards[i];

			//	maneuverLog("Camera Facing Billboard: {0} - {1}", logLevels.log, i, t.name);
			//}

			for (int i = gizmo.renderer.solver.maneuverNodes.Count - 1; i >= 0; i--)
			{
				ManeuverNode node = gizmo.renderer.solver.maneuverNodes[i];

				if (node.patch != gizmo.patchBefore)
					continue;

				lastManeuverIndex = i;
				//maneuverLog("Node Found...", logLevels.log);
				currentNode = node;
				break;
			}

			//RectTransform minusRect = currentGizmo.minusOrbitbtn.GetComponent<RectTransform>();

			//maneuverLog("Minus Button Rect Anchored: {0:F4}\nPosition: {1:F4}\nPivot: {2:F4}\nAnchor Min: {3:F4}\nAnchor Max: {4:F4}\nScale: {5:F4}\nRotation: {5:F4}\nLocal Rotation: {6:F4}"
			//	, logLevels.log
			//	, minusRect.anchoredPosition3D
			//	, minusRect.position
			//	, minusRect.pivot
			//	, minusRect.anchorMin
			//	, minusRect.anchorMax
			//	, minusRect.localScale
				//, minusRect.rotation.eulerAngles
				//, minusRect.localRotation.eulerAngles);

			//RectTransform plusRect = currentGizmo.plusOrbitBtn.GetComponent<RectTransform>();

			//maneuverLog("Plus Button Rect Anchored: {0:F4}\nPosition: {1:F4}\nPivot: {2:F4}\nAnchor Min: {3:F4}\nAnchor Max: {4:F4}\nScale: {5:F4}\nRotation: {5:F4}\nLocal Rotation: {6:F4}\nSize: {7:F4}\nImage Height: {8:F4}\nImage Width: {9:F4}"
			//	, logLevels.log
			//	, plusRect.anchoredPosition3D
			//	, plusRect.position
			//	, plusRect.pivot
			//	, plusRect.anchorMin
			//	, plusRect.anchorMax
			//	, plusRect.localScale
			//	, plusRect.rotation.eulerAngles
			//	, plusRect.localRotation.eulerAngles
			//	//, plusRect.sizeDelta
			//	, currentGizmo.plusOrbitBtn.image.sprite.texture.height
			//	, currentGizmo.plusOrbitBtn.image.sprite.texture.width);

			//maneuverLog("Plus Button Transform: {0:F4}", logLevels.log, currentGizmo.plusOrbitBtn.transform.position);
			//maneuverLog("Button Root Transform: {0:F4}", logLevels.log, currentGizmo.buttonRoot.transform.position);
			//maneuverLog("Gizmo Transform: {0:F4}", logLevels.log, currentGizmo.transform.position);

			//RectTransform closeRect = currentGizmo.deleteBtn.GetComponent<RectTransform>();

			//maneuverLog("Close Button Rect Anchored: {0:F4}\nPosition: {1:F4}\nPivot: {2:F4}\nAnchor Min: {3:F4}\nAnchor Max: {4:F4}\nScale: {5:F4}\nRotation: {5:F4}\nLocal Rotation: {6:F4}"
			//	, logLevels.log
			//	, closeRect.anchoredPosition3D
			//	, closeRect.position
			//	, closeRect.pivot
			//	, closeRect.anchorMin
			//	, closeRect.anchorMax
			//	, closeRect.localScale
			//	, closeRect.rotation.eulerAngles
			//	, closeRect.localRotation.eulerAngles);

			attachGizmoHandlers();

			if (settings.maneuverSnap)
				attachNewSnapButton();

			if (settings.manualControls)
				attachNewInputButton();
		}

		private void attachNewSnapButton()
		{
			snapPanel = gameObject.AddComponent<ManeuverSnapPanel>();

			snapPanel.setup(currentNode, currentGizmo);
		}

		private void attachNewInputButton()
		{
			inputPanel = gameObject.AddComponent<ManeuverInputPanel>();

			inputPanel.setup(currentNode, currentGizmo, lastManeuverIndex);
		}

		private void DetachGizmo()
		{
			maneuverLog("Detaching Gizmo...", logLevels.log);

			if (currentNode != null)
			currentNode.DetachGizmo();

			currentGizmo = null;
			currentNode = null;

			if (snapPanel != null)
				Destroy(snapPanel);

			if (inputPanel != null)
				Destroy(inputPanel);

			snapPanel = null;
			inputPanel = null;
		}

		//private void GizmoUpdated(Vector3d deltaV, double UT)
		//{
		//	currentNode.OnGizmoUpdated(deltaV, UT);
		//	currentGizmo.patchBefore = currentNode.patch;
		//	currentGizmo.patchAhead = currentNode.nextPatch;
		//}

		//private void hideAttachedGizmo()
		//{
		//	maneuverLog("1-1", logLevels.log);
		//	currentGizmo = Instantiate<GameObject>(MapView.ManeuverNodePrefab).GetComponent<ManeuverGizmo>();
		//	currentGizmo.GetComponent<ManeuverGizmoListener>().secondary = true;
		//	currentGizmo.gameObject.SetActive(true);
		//	currentGizmo.gameObject.name = "Maneuver Node";
		//	currentGizmo.DeltaV = currentNode.DeltaV;
		//	currentGizmo.UT = currentNode.UT;
		//	currentGizmo.OnGizmoUpdated = new ManeuverGizmo.HandlesUpdatedCallback(GizmoUpdated);
		//	currentGizmo.OnMinimize = new Callback(DetachGizmo);
		//	currentGizmo.OnDelete = new Callback(DeleteGizmo);
		//	currentGizmo.Setup(currentNode, currentGizmo.renderer);
		//	currentGizmo.transform.position = currentGizmo.transform.position;

		//	currentGizmo.screenSize = originalScale * settings.baseScale;
		//}

		private void attachGizmoHandlers()
		{
			cachedPrograde = currentGizmo.handlePrograde.OnHandleUpdate;
			cachedRetrograde = currentGizmo.handleRetrograde.OnHandleUpdate;
			cachedNormal = currentGizmo.handleNormal.OnHandleUpdate;
			cachedAntiNormal = currentGizmo.handleAntiNormal.OnHandleUpdate;
			cachedRadialIn = currentGizmo.handleRadialIn.OnHandleUpdate;
			cachedRadialOut = currentGizmo.handleRadialOut.OnHandleUpdate;

			if (settings.alignToOrbit)
			{
				currentGizmo.handlePrograde.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnProgradeUpdate);
				currentGizmo.handleRetrograde.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnRetrogradeUpdate);
				currentGizmo.handleNormal.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnNormalUpdate);
				currentGizmo.handleAntiNormal.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnAntiNormalUpdate);
				currentGizmo.handleRadialIn.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnRadialInUpdate);
				currentGizmo.handleRadialOut.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnRadialOutUpdate);
			}
			else
			{
				currentGizmo.handlePrograde.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnProgradeUpdateStd);
				currentGizmo.handleRetrograde.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnRetrogradeUpdateStd);
				currentGizmo.handleNormal.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnNormalUpdateStd);
				currentGizmo.handleAntiNormal.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnAntiNormalUpdateStd);
				currentGizmo.handleRadialIn.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnRadialInUpdateStd);
				currentGizmo.handleRadialOut.OnHandleUpdate = new ManeuverGizmoHandle.HandleUpdate(OnRadialOutUpdateStd);
			}

			UpdateRotation();
		}

		private void gizmoDestroy(ManeuverGizmo gizmo)
		{
			if (gizmo != currentGizmo)
				return;

			currentGizmo = null;
			currentNode = null;

			if (snapPanel != null)
				Destroy(snapPanel);

			if (inputPanel != null)
				Destroy(inputPanel);

			snapPanel = null;
			inputPanel = null;
		}

		private void UpdateRotation()
		{
			Vector3d oldPos = currentNode.patch.getRelativePositionAtUT(currentNode.UT).xzy;
			Vector3d oldVel = currentNode.patch.getOrbitalVelocityAtUT(currentNode.UT).xzy;
			referenceRotation = Quaternion.LookRotation(oldVel, Vector3d.Cross(-oldPos, oldVel));
			//transform.rotation = Quaternion.LookRotation(oldVel, Vector3d.Cross(-oldPos, oldVel));

			Vector3d pos = currentNode.nextPatch.getRelativePositionAtUT(currentNode.UT).xzy;
			Vector3d vel = currentNode.nextPatch.getOrbitalVelocityAtUT(currentNode.UT).xzy;
			orbitRotation = Quaternion.LookRotation(vel, Vector3d.Cross(-pos, vel));
			//currentListener.transform.rotation = Quaternion.LookRotation(vel, Vector3d.Cross(-pos, vel));
		}

		private void OnProgradeUpdate(float value)
		{
			//if (ignoreSensitivity)
			//{
			//	if (Mathf.Abs(value) > accuracy)
			//	{
			//		float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

			//		value = value / floor;

			//		for (int i = 0; i < floor; i++)
			//		{
			//			ProgradeUpdate(value, false);
			//		}
			//	}
			//	else
			//		ProgradeUpdate(value, true);
			//}
			//else
			//{

				value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

				//if (Mathf.Abs(endValue) > accuracy)
				//{
				//	float floor = Mathf.Floor(Mathf.Abs(endValue) / accuracy);

				//	endValue = endValue / floor;

				//	for (int i = 0; i < floor; i++)
				//	{
				//		ProgradeUpdate(endValue, false);
				//	}
				//}
				//else
				//	ProgradeUpdate(endValue, false);
			//}

				OnProgradeUpdate(value, currentNode, false, true);

			UpdateInputPanel();

			//ignoreSensitivity = false;
		}

		public void OnProgradeUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
				{
					ProgradeUpdate(value, node, ignore, gizmo);
				}
			}
			else
				ProgradeUpdate(value, node, ignore, gizmo);
		}

		private void ProgradeUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation();

			Vector3 reference = (referenceRotation * Vector3.forward).normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, -1 * (orbitRotation * Vector3.right).normalized),
				Vector3.Dot(reference, -1 * (orbitRotation * Vector3.up).normalized),
				Vector3.Dot(reference, (orbitRotation * Vector3.forward).normalized));

			if (gizmo)
			{
				currentGizmo.DeltaV += newDirection * value;

				if (ignore)
					node.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
				else
					cachedPrograde(0);
			}
			else
			{
				node.DeltaV += newDirection * value;
				node.solver.UpdateFlightPlan();
			}
		}

		//private void ProgradeUpdate(float value, bool ignore)
		//{
		//	UpdateRotation();

		//	Vector3 reference = transform.forward.normalized;

		//	Vector3 newDirection = new Vector3(Vector3.Dot(reference, -1 * currentListener.transform.right.normalized),
		//		Vector3.Dot(reference, -1 * currentListener.transform.up.normalized),
		//		Vector3.Dot(reference, currentListener.transform.forward.normalized));

		//	currentGizmo.DeltaV += newDirection * value;

		//	if (ignore)
		//		currentNode.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
		//	else
		//		cachedPrograde(0);
		//}

		private void OnRetrogradeUpdate(float value)
		{
			if (ignoreSensitivity)
			{
				if (Mathf.Abs(value) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

					value = value / floor;

					for (int i = 0; i < floor; i++)
					{
						RetrogradeUpdate(value, false);
					}
				}
				else
					RetrogradeUpdate(value, true);
			}
			else
			{

				float endValue = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

				if (Mathf.Abs(endValue) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(endValue) / accuracy);

					endValue = endValue / floor;

					for (int i = 0; i < floor; i++)
					{
						RetrogradeUpdate(endValue, false);
					}
				}
				else
					RetrogradeUpdate(endValue, false);
			}

			UpdateInputPanel();

			ignoreSensitivity = false;
		}

		private void RetrogradeUpdate(float value, bool ignore)
		{
			UpdateRotation();

			Vector3 reference = transform.forward.normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, -1 * currentListener.transform.right.normalized),
				Vector3.Dot(reference, -1 * currentListener.transform.up.normalized),
				Vector3.Dot(reference, currentListener.transform.forward.normalized));

			currentGizmo.DeltaV -= newDirection * value;

			if (ignore)
				currentNode.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
			else
				cachedRetrograde(0);
		}

		private void OnNormalUpdate(float value)
		{
			//if (ignoreSensitivity)
			//{
			//	if (Mathf.Abs(value) > accuracy)
			//	{
			//		float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

			//		value = value / floor;

			//		for (int i = 0; i < floor; i++)
			//		{
			//			NormalUpdate(value, false);
			//		}
			//	}
			//	else
			//		NormalUpdate(value, true);
			//}
			//else
			//{

			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			//	if (Mathf.Abs(endValue) > accuracy)
			//	{
			//		float floor = Mathf.Floor(Mathf.Abs(endValue) / accuracy);

			//		endValue = endValue / floor;

			//		for (int i = 0; i < floor; i++)
			//		{
			//			NormalUpdate(endValue, false);
			//		}
			//	}
			//	else
			//		NormalUpdate(endValue, false);
			//}

			//UpdateInputPanel();

				OnNormalUpdate(value, currentNode, false, true);

			//ignoreSensitivity = false;
		}

		public void OnNormalUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
				{
					NormalUpdate(value, node, ignore, gizmo);
				}
			}
			else
				NormalUpdate(value, node, ignore, gizmo);
		}

		//private void NormalUpdate(float value, bool ignore)
		//{
		//	UpdateRotation();

		//	Vector3 reference = transform.up.normalized;

		//	Vector3 newDirection = new Vector3(0 /*Vector3.Dot(reference, currentListener.transform.right.normalized)*/,
		//		Vector3.Dot(reference, currentListener.transform.up.normalized),
		//		Vector3.Dot(reference, -1 * currentListener.transform.forward.normalized));

		//	currentGizmo.DeltaV += newDirection * value;

		//	if (ignore)
		//		currentNode.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
		//	else
		//		cachedNormal(0);
		//}

		private void NormalUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation();

			Vector3 reference = (referenceRotation * Vector3.up).normalized;

			Vector3 newDirection = new Vector3(0,
				Vector3.Dot(reference, (orbitRotation * Vector3.up).normalized),
				Vector3.Dot(reference, -1 * (orbitRotation * Vector3.forward).normalized));

			if (gizmo)
			{
				currentGizmo.DeltaV += newDirection * value;

				if (ignore)
					node.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
				else
					cachedNormal(0);
			}
			else
			{
				node.DeltaV += newDirection * value;
				node.solver.UpdateFlightPlan();
			}
		}

		private void OnAntiNormalUpdate(float value)
		{
			if (ignoreSensitivity)
			{
				if (Mathf.Abs(value) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

					value = value / floor;

					for (int i = 0; i < floor; i++)
					{
						AntiNormalUpdate(value, false);
					}
				}
				else
					AntiNormalUpdate(value, true);
			}
			else
			{

				float endValue = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

				if (Mathf.Abs(endValue) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(endValue) / accuracy);

					endValue = endValue / floor;

					for (int i = 0; i < floor; i++)
					{
						AntiNormalUpdate(endValue, false);
					}
				}
				else
					AntiNormalUpdate(endValue, false);
			}

			UpdateInputPanel();

			ignoreSensitivity = false;
		}

		private void AntiNormalUpdate(float value, bool ignore)
		{
			UpdateRotation();

			Vector3 reference = transform.up.normalized;

			Vector3 newDirection = new Vector3(0/*Vector3.Dot(reference, currentListener.transform.right.normalized)*/,
				Vector3.Dot(reference, currentListener.transform.up.normalized),
				Vector3.Dot(reference, -1 * currentListener.transform.forward.normalized));

			currentGizmo.DeltaV -= newDirection * value;

			if (ignore)
				currentNode.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
			else
				cachedAntiNormal(0);
		}

		private void OnRadialInUpdate(float value)
		{
			if (ignoreSensitivity)
			{
				if (Mathf.Abs(value) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

					value = value / floor;

					for (int i = 0; i < floor; i++)
					{
						RadialInUpdate(value, false);
					}
				}
				else
					RadialInUpdate(value, true);
			}
			else
			{

				float endValue = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

				if (Mathf.Abs(endValue) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(endValue) / accuracy);

					endValue = endValue / floor;

					for (int i = 0; i < floor; i++)
					{
						RadialInUpdate(endValue, false);
					}
				}
				else
					RadialInUpdate(endValue, false);
			}

			UpdateInputPanel();

			ignoreSensitivity = false;
		}

		private void RadialInUpdate(float value, bool ignore)
		{
			UpdateRotation();

			Vector3 reference = -1 * transform.right.normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, -1 * currentListener.transform.right.normalized),
				0/*Vector3.Dot(reference, currentListener.transform.up.normalized)*/,
				Vector3.Dot(reference, currentListener.transform.forward.normalized));

			currentGizmo.DeltaV -= newDirection * value;

			if (ignore)
				currentNode.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
			else
				cachedRadialIn(0);
		}

		private void OnRadialOutUpdate(float value)
		{
			if (ignoreSensitivity)
			{
				if (Mathf.Abs(value) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

					value = value / floor;

					for (int i = 0; i < floor; i++)
					{
						RadialOutUpdate(value, false);
					}
				}
				else
					RadialOutUpdate(value, true);
			}
			else
			{

				float endValue = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

				if (Mathf.Abs(endValue) > accuracy)
				{
					float floor = Mathf.Floor(Mathf.Abs(endValue) / accuracy);

					endValue = endValue / floor;

					for (int i = 0; i < floor; i++)
					{
						RadialOutUpdate(endValue, false);
					}
				}
				else
					RadialOutUpdate(endValue, false);
			}

			UpdateInputPanel();

			ignoreSensitivity = false;
		}

		private void RadialOutUpdate(float value, bool ignore)
		{
			UpdateRotation();

			Vector3 reference = -1 * transform.right.normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, -1 * currentListener.transform.right.normalized),
				0/*Vector3.Dot(reference, currentListener.transform.up.normalized)*/,
				Vector3.Dot(reference, currentListener.transform.forward.normalized));

			currentGizmo.DeltaV += newDirection * value;

			if (ignore)
				currentNode.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
			else
				cachedRadialOut(0);
		}

		private void OnProgradeUpdateStd(float value)
		{
			cachedPrograde(value);

			UpdateInputPanel();
		}

		private void OnRetrogradeUpdateStd(float value)
		{
			cachedRetrograde(value);

			UpdateInputPanel();
		}

		private void OnNormalUpdateStd(float value)
		{
			cachedNormal(value);

			UpdateInputPanel();
		}

		private void OnAntiNormalUpdateStd(float value)
		{
			cachedAntiNormal(value);

			UpdateInputPanel();
		}

		private void OnRadialInUpdateStd(float value)
		{
			cachedRadialIn(value);

			UpdateInputPanel();
		}

		private void OnRadialOutUpdateStd(float value)
		{
			cachedRadialOut(value);

			UpdateInputPanel();
		}

		private void UpdateInputPanel()
		{
			if (!settings.manualControls)
				return;

			if (inputPanel == null)
				return;

			inputPanel.UpdateDeltaV();
		}

		private void enterMap()
		{
			camera = PlanetariumCamera.Camera;
		}

		private void exitMap()
		{
			camera = null;
		}

		public static void maneuverLog(string message, logLevels l, params object[] objs)
		{
			message = string.Format(message, objs);
			string log = string.Format("[Better Maneuvering] {0}", message);
			switch (l)
			{
				case logLevels.log:
					Debug.Log(log);
					break;
				case logLevels.warning:
					Debug.LogWarning(log);
					break;
				case logLevels.error:
					Debug.LogError(log);
					break;
			}
		}
    }

	public enum logLevels
	{
		log = 1,
		warning = 2,
		error = 3,
	}
}
