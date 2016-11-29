#region license
/*The MIT License (MIT)

ManeuverController - Primary addon controller; modifies stock maneuver node behavior

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
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

		private PopupDialog maneuverDialog;
		private ManeuverNode currentNode;
		private ManeuverGizmo currentGizmo;
		private PatchedConicRenderer pcr;
		private float originalScale;

		private bool mouseDown;
		private float holdTime = 0.3f;

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

		private Button cycleForwardButton;
		private Button cycleBackButton;

		private List<ManeuverInputPanel> inputPanels = new List<ManeuverInputPanel>();
		private List<ManeuverSnapPanel> snapPanels = new List<ManeuverSnapPanel>();

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
			GameEvents.OnGameSettingsApplied.Remove(SettingsApplied);
		}

		private void LateUpdate()
		{
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
							{
								if (currentGizmo != null)
								{
									if (snapPanel != null)
									{
										if (snapPanel.Locked)
										{
											snapPanel.CloseGizmo();
											snapPanels.Add(snapPanel);
										}
										else
											Destroy(snapPanel);
									}

									if (inputPanel != null)
									{
										if (inputPanel.Locked)
										{
											inputPanel.CloseGizmo();
											inputPanels.Add(inputPanel);
										}
										else
											Destroy(inputPanel);
									}

									snapPanel = null;
									inputPanel = null;
								}

								currentNode.DetachGizmo();
							}
						}
					}
				}

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

					if (settings.rightClickClose)
					{
						if (pcr == null)
							return;

						if (pcr.MouseOverNodes)
							return;

						if (Input.GetMouseButtonDown(1) && !mouseDown)
						{
							if ((snapPanel != null && snapPanel.IsVisible) || inputPanel != null && inputPanel.IsVisible)
								StartCoroutine(mouseRoutine());
						}
					}
				}
			}
		}

		private IEnumerator mouseRoutine()
		{
			mouseDown = true;

			float timer = Time.realtimeSinceStartup;

			while (Time.realtimeSinceStartup - timer < holdTime)
			{
				if (Input.GetMouseButtonUp(1))
				{
					if (snapPanel != null && snapPanel.IsVisible && !snapPanel.Locked)
						snapPanel.ToggleUI();

					if (inputPanel != null && inputPanel.IsVisible && !inputPanel.Locked)
						inputPanel.ToggleUI();

					mouseDown = false;

					yield break;
				}

				yield return null;
			}

			mouseDown = false;
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

			double minAnomalyTime = refPatch.GetUTforTrueAnomaly((trueAnomaly - settings.selectionTolerance < -180 ? trueAnomaly - settings.selectionTolerance + 360 : trueAnomaly - settings.selectionTolerance) * Mathf.Deg2Rad, 0);
			double maxAnomalyTime = refPatch.GetUTforTrueAnomaly((trueAnomaly + settings.selectionTolerance > 180 ? trueAnomaly + settings.selectionTolerance - 360 : trueAnomaly + settings.selectionTolerance) * Mathf.Deg2Rad, 0);

			if (minAnomalyTime > UT && refPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
				minAnomalyTime -= refPatch.period;

			double minAnomalyTimeWide = refPatch.GetUTforTrueAnomaly((trueAnomaly - (settings.selectionTolerance + 5) < -180 ? trueAnomaly - (settings.selectionTolerance + 5) + 360 : trueAnomaly - (settings.selectionTolerance + 5)) * Mathf.Deg2Rad, 0);
			double maxAnomalyTimeWide = refPatch.GetUTforTrueAnomaly((trueAnomaly + (settings.selectionTolerance + 5) > 180 ? trueAnomaly + (settings.selectionTolerance + 5) - 360 : trueAnomaly + (settings.selectionTolerance + 5)) * Mathf.Deg2Rad, 0);

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

					man.OptionText = "Add Maneuver To Target Asc Node";

					newManeuvers.Add(man);
				}

				if (ManeuverUtilities.parseRelDesc(UT, refPatch, tgt, minAnomalyTime, maxAnomalyTime, out newUT))
				{
					AddManeuver man = new AddManeuver(pcr, newUT);

					man.OptionText = "Add Maneuver To Target Desc Node";

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

						man.OptionText = "Add Maneuver To Closest Approach";

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

		public void RemoveSnapPanel(ManeuverSnapPanel panel)
		{
			if (snapPanels.Contains(panel))
				snapPanels.Remove(panel);
		}

		public void RemoveInputPanel(ManeuverInputPanel panel)
		{
			if (inputPanels.Contains(panel))
				inputPanels.Remove(panel);
		}

		private void gizmoSpawn(ManeuverGizmo gizmo)
		{
			if (gizmo == null)
				return;

			if (currentGizmo != null)
			{
				if (snapPanel != null)
				{
					if (snapPanel.Locked)
					{
						snapPanel.CloseGizmo();
						snapPanels.Add(snapPanel);
					}
					else
						Destroy(snapPanel);
				}

				if (inputPanel != null)
				{
					if (inputPanel.Locked)
					{
						inputPanel.CloseGizmo();
						inputPanels.Add(inputPanel);
					}
					else
						Destroy(inputPanel);
				}

				snapPanel = null;
				inputPanel = null;
			}

			currentGizmo = gizmo;

			originalScale = currentGizmo.screenSize;

			currentGizmo.screenSize = originalScale * settings.baseScale;
			
			for (int i = gizmo.renderer.solver.maneuverNodes.Count - 1; i >= 0; i--)
			{
				ManeuverNode node = gizmo.renderer.solver.maneuverNodes[i];

				if (node.patch != gizmo.patchBefore)
					continue;

				lastManeuverIndex = i;
				currentNode = node;
				break;
			}

			//RectTransform plusRect = currentGizmo.deleteBtn.GetComponent<RectTransform>();
			//maneuverLog("Delete Button Rect Anchored: {0:F4}\nPosition: {1:F4}\nPivot: {2:F4}\nAnchor Min: {3:F4}\nAnchor Max: {4:F4}\nScale: {5:F4}\nRotation: {6:F4}\nLocal Rotation: {7:F4}\nSize: {8:F4}"
			//, logLevels.log
			//, plusRect.anchoredPosition3D
			//, plusRect.position
			//, plusRect.pivot
			//, plusRect.anchorMin
			//, plusRect.anchorMax
			//, plusRect.localScale
			//, plusRect.rotation.eulerAngles
			//, plusRect.localRotation.eulerAngles
			//, plusRect.sizeDelta);

			attachGizmoHandlers();

			if (settings.showManeuverCycle)
				attachCycleButtons();

			if (settings.replaceGizmoButtons)
			{
				attachNewSnapButton();
				attachNewInputButton();
			}
		}

		private void attachNewSnapButton()
		{
			for (int i = snapPanels.Count - 1; i >= 0; i--)
			{
				ManeuverSnapPanel snap = snapPanels[i];

				if (snap == null)
					continue;

				if (snap.Node != currentNode)
					continue;

				snapPanel = snap;
				snapPanel.setup(currentNode, currentGizmo, lastManeuverIndex, false);
				snapPanels.Remove(snap);
				return;
			}

			snapPanel = gameObject.AddComponent<ManeuverSnapPanel>();

			snapPanel.setup(currentNode, currentGizmo, lastManeuverIndex, settings.rememberManualInput);
		}

		private void attachNewInputButton()
		{
			for (int i = inputPanels.Count - 1; i >= 0; i--)
			{
				ManeuverInputPanel input = inputPanels[i];

				if (input == null)
					continue;

				if (input.Node != currentNode)
					continue;

				inputPanel = input;
				inputPanel.setup(currentNode, currentGizmo, lastManeuverIndex, false);
				inputPanels.Remove(input);
				return;
			}

			inputPanel = gameObject.AddComponent<ManeuverInputPanel>();

			inputPanel.setup(currentNode, currentGizmo, lastManeuverIndex, settings.rememberManualInput);
		}

		private void attachCycleButtons()
		{
			RectTransform deleteRect = currentGizmo.deleteBtn.GetComponent<RectTransform>();
			deleteRect.anchoredPosition3D = new Vector3(0, deleteRect.anchoredPosition3D.y, deleteRect.anchoredPosition3D.z);
			deleteRect.localScale = deleteRect.localScale * 0.8f;

			cycleForwardButton = Instantiate<Button>(currentGizmo.plusOrbitBtn);

			cycleForwardButton.transform.SetParent(currentGizmo.plusOrbitBtn.transform.parent);

			RectTransform forwardRect = cycleForwardButton.GetComponent<RectTransform>();

			RectTransform oldRect = currentGizmo.plusOrbitBtn.GetComponent<RectTransform>();

			forwardRect.position = oldRect.position;
			forwardRect.anchoredPosition3D = new Vector3(oldRect.anchoredPosition3D.x + 4, 24, oldRect.anchoredPosition3D.z);
			forwardRect.localScale = oldRect.localScale * 0.8f;
			forwardRect.rotation = oldRect.rotation;
			forwardRect.localRotation = oldRect.localRotation;

			//maneuverLog("Cycle Button Rect Anchored: {0:F4}\nPosition: {1:F4}\nPivot: {2:F4}\nAnchor Min: {3:F4}\nAnchor Max: {4:F4}\nScale: {5:F4}\nRotation: {6:F4}\nLocal Rotation: {7:F4}\nSize: {8:F4}"
			//, logLevels.log
			//, forwardRect.anchoredPosition3D
			//, forwardRect.position
			//, forwardRect.pivot
			//, forwardRect.anchorMin
			//, forwardRect.anchorMax
			//, forwardRect.localScale
			//, forwardRect.rotation.eulerAngles
			//, forwardRect.localRotation.eulerAngles
			//, forwardRect.sizeDelta);

			cycleForwardButton.navigation = new Navigation() { mode = Navigation.Mode.None };

			cycleForwardButton.onClick.RemoveAllListeners();
			cycleForwardButton.onClick.AddListener(new UnityAction(cycleForward));
			cycleForwardButton.interactable = true;

			Selectable nextSelect = cycleForwardButton.GetComponent<Selectable>();

			nextSelect.image.sprite = ManeuverLoader.NextButtonNormal;

			SpriteState state = nextSelect.spriteState;
			state.highlightedSprite = ManeuverLoader.NextButtonHighlight;
			state.pressedSprite = ManeuverLoader.NextButtonActive;
			state.disabledSprite = ManeuverLoader.NextButtonInactive;
			nextSelect.spriteState = state;

			oldRect.localScale = oldRect.localScale * 0.9f;
			oldRect.anchoredPosition3D = new Vector3(oldRect.anchoredPosition3D.x - 4, oldRect.anchoredPosition3D.y - 4, oldRect.anchoredPosition3D.z);

			cycleBackButton = Instantiate<Button>(currentGizmo.minusOrbitbtn);

			cycleBackButton.transform.SetParent(currentGizmo.minusOrbitbtn.transform.parent);

			RectTransform backRect = cycleBackButton.GetComponent<RectTransform>();

			oldRect = currentGizmo.minusOrbitbtn.GetComponent<RectTransform>();

			backRect.position = oldRect.position;
			backRect.anchoredPosition3D = new Vector3(oldRect.anchoredPosition3D.x - 4, 24, oldRect.anchoredPosition3D.z);
			backRect.localScale = oldRect.localScale * 0.8f;
			backRect.rotation = oldRect.rotation;
			backRect.localRotation = oldRect.localRotation;

			cycleBackButton.navigation = new Navigation() { mode = Navigation.Mode.None };

			cycleBackButton.onClick.RemoveAllListeners();
			cycleBackButton.onClick.AddListener(new UnityAction(cycleBackward));
			cycleBackButton.interactable = true;

			Selectable prevSelect = cycleBackButton.GetComponent<Selectable>();

			prevSelect.image.sprite = ManeuverLoader.PrevButtonNormal;

			SpriteState backState = nextSelect.spriteState;
			state.highlightedSprite = ManeuverLoader.PrevButtonHighlight;
			state.pressedSprite = ManeuverLoader.PrevButtonActive;
			state.disabledSprite = ManeuverLoader.PrevButtonInactive;
			prevSelect.spriteState = state;

			oldRect.localScale = oldRect.localScale * 0.9f;
			oldRect.anchoredPosition3D = new Vector3(oldRect.anchoredPosition3D.x + 4, oldRect.anchoredPosition3D.y - 4, oldRect.anchoredPosition3D.z);

			updateCycleButtons();
		}

		private void cycleForward()
		{
			if (pcr == null)
				return;

			int count = pcr.solver.maneuverNodes.Count;

			if (count <= 1)
				return;

			int newIndex = lastManeuverIndex + 1;

			if (newIndex >= count)
				newIndex = 0;

			if (currentGizmo != null)
			{
				if (snapPanel != null)
				{
					if (snapPanel.Locked)
					{
						snapPanel.CloseGizmo();
						snapPanels.Add(snapPanel);
					}
					else
						Destroy(snapPanel);
				}

				if (inputPanel != null)
				{
					if (inputPanel.Locked)
					{
						inputPanel.CloseGizmo();
						inputPanels.Add(inputPanel);
					}
					else
						Destroy(inputPanel);
				}

				snapPanel = null;
				inputPanel = null;
			}

			currentNode.DetachGizmo();
			pcr.solver.maneuverNodes[newIndex].AttachGizmo(MapView.ManeuverNodePrefab, pcr);

			updateCycleButtons();
		}

		private void cycleBackward()
		{
			if (pcr == null)
				return;

			int count = pcr.solver.maneuverNodes.Count;

			if (count <= 1)
				return;

			int newIndex = lastManeuverIndex - 1;

			if (newIndex < 0)
				newIndex = count - 1;

			if (currentGizmo != null)
			{
				if (snapPanel != null)
				{
					if (snapPanel.Locked)
					{
						snapPanel.CloseGizmo();
						snapPanels.Add(snapPanel);
					}
					else
						Destroy(snapPanel);
				}

				if (inputPanel != null)
				{
					if (inputPanel.Locked)
					{
						inputPanel.CloseGizmo();
						inputPanels.Add(inputPanel);
					}
					else
						Destroy(inputPanel);
				}

				snapPanel = null;
				inputPanel = null;
			}

			currentNode.DetachGizmo();
			pcr.solver.maneuverNodes[newIndex].AttachGizmo(MapView.ManeuverNodePrefab, pcr);
			
			updateCycleButtons();
		}

		private void updateCycleButtons()
		{
			if (pcr == null)
				return;

			if (cycleBackButton == null || cycleForwardButton == null)
				return;
			
			if (pcr.solver.maneuverNodes.Count <= 1)
			{
				cycleBackButton.interactable = false;
				cycleForwardButton.interactable = false;
			}
			else
			{
				cycleForwardButton.interactable = true;
				cycleBackButton.interactable = true;
			}
		}

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

			UpdateRotation(currentNode);
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

		private void UpdateRotation(ManeuverNode node)
		{
			Vector3d oldPos = node.patch.getRelativePositionAtUT(node.UT).xzy;
			Vector3d oldVel = node.patch.getOrbitalVelocityAtUT(node.UT).xzy;
			referenceRotation = Quaternion.LookRotation(oldVel, Vector3d.Cross(-oldPos, oldVel));

			Vector3d pos = node.nextPatch.getRelativePositionAtUT(node.UT).xzy;
			Vector3d vel = node.nextPatch.getOrbitalVelocityAtUT(node.UT).xzy;
			orbitRotation = Quaternion.LookRotation(vel, Vector3d.Cross(-pos, vel));
		}

		private void OnProgradeUpdate(float value)
		{
			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			OnProgradeUpdate(value, currentNode, false, true);
		}

		public void OnProgradeUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
					ProgradeUpdate(value, node, ignore, gizmo);
			}
			else
				ProgradeUpdate(value, node, ignore, gizmo);

			if (!ignore)
				UpdateInputPanel();
		}

		private void ProgradeUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation(node);

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

		private void OnRetrogradeUpdate(float value)
		{
			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			OnRetrogradeUpdate(value, currentNode, false, true);
		}

		public void OnRetrogradeUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
					RetrogradeUpdate(value, node, ignore, gizmo);
			}
			else
				RetrogradeUpdate(value, node, ignore, gizmo);

			if (!ignore)
				UpdateInputPanel();
		}

		private void RetrogradeUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation(node);

			Vector3 reference = (referenceRotation * Vector3.forward).normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, -1 * (orbitRotation * Vector3.right).normalized),
				Vector3.Dot(reference, -1 * (orbitRotation * Vector3.up).normalized),
				Vector3.Dot(reference, (orbitRotation * Vector3.forward).normalized));

			if (gizmo)
			{
				currentGizmo.DeltaV -= newDirection * value;

				if (ignore)
					node.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
				else
					cachedRetrograde(0);
			}
			else
			{
				node.DeltaV -= newDirection * value;
				node.solver.UpdateFlightPlan();
			}
		}

		private void OnNormalUpdate(float value)
		{
			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			OnNormalUpdate(value, currentNode, false, true);
		}

		public void OnNormalUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
					NormalUpdate(value, node, ignore, gizmo);
			}
			else
				NormalUpdate(value, node, ignore, gizmo);

			if (!ignore)
				UpdateInputPanel();
		}

		private void NormalUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation(node);

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
			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			OnAntiNormalUpdate(value, currentNode, false, true);
		}

		public void OnAntiNormalUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
					AntiNormalUpdate(value, node, ignore, gizmo);
			}
			else
				AntiNormalUpdate(value, node, ignore, gizmo);

			if (!ignore)
				UpdateInputPanel();
		}

		private void AntiNormalUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation(node);

			Vector3 reference = (referenceRotation * Vector3.up).normalized;

			Vector3 newDirection = new Vector3(0,
				Vector3.Dot(reference, (orbitRotation * Vector3.up).normalized),
				Vector3.Dot(reference, -1 * (orbitRotation * Vector3.forward).normalized));

			if (gizmo)
			{
				currentGizmo.DeltaV -= newDirection * value;

				if (ignore)
					node.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
				else
					cachedAntiNormal(0);
			}
			else
			{
				node.DeltaV -= newDirection * value;
				node.solver.UpdateFlightPlan();
			}
		}

		private void OnRadialInUpdate(float value)
		{
			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			OnRadialInUpdate(value, currentNode, false, true);
		}

		public void OnRadialInUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
					RadialInUpdate(value, node, ignore, gizmo);
			}
			else
				RadialInUpdate(value, node, ignore, gizmo);

			if (!ignore)
				UpdateInputPanel();
		}

		private void RadialInUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation(node);

			Vector3 reference = (referenceRotation * Vector3.right).normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, (orbitRotation * Vector3.right).normalized),
				0,
				Vector3.Dot(reference, -1 * (orbitRotation * Vector3.forward).normalized));

			if (gizmo)
			{
				currentGizmo.DeltaV -= newDirection * value;

				if (ignore)
					node.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
				else
					cachedRadialIn(0);
			}
			else
			{
				node.DeltaV -= newDirection * value;
				node.solver.UpdateFlightPlan();
			}
		}

		private void OnRadialOutUpdate(float value)
		{
			value = Mathf.Pow(Mathf.Abs(value), (float)currentGizmo.sensitivity) * (float)currentGizmo.multiplier * Mathf.Sign(value);

			OnRadialOutUpdate(value, currentNode, false, true);
		}

		public void OnRadialOutUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			if (Mathf.Abs(value) > accuracy)
			{
				float floor = Mathf.Floor(Mathf.Abs(value) / accuracy);

				value = value / floor;

				for (int i = 0; i < floor; i++)
					RadialOutUpdate(value, node, ignore, gizmo);
			}
			else
				RadialOutUpdate(value, node, ignore, gizmo);

			if (!ignore)
				UpdateInputPanel();
		}

		private void RadialOutUpdate(float value, ManeuverNode node, bool ignore, bool gizmo)
		{
			UpdateRotation(node);

			Vector3 reference = (referenceRotation * Vector3.right).normalized;

			Vector3 newDirection = new Vector3(Vector3.Dot(reference, (orbitRotation * Vector3.right).normalized),
				0,
				Vector3.Dot(reference, -1 * (orbitRotation * Vector3.forward).normalized));

			if (gizmo)
			{
				currentGizmo.DeltaV += newDirection * value;

				if (ignore)
					node.OnGizmoUpdated(currentGizmo.DeltaV, currentGizmo.UT);
				else
					cachedRadialOut(0);
			}
			else
			{
				node.DeltaV += newDirection * value;
				node.solver.UpdateFlightPlan();
			}
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
			if (!settings.replaceGizmoButtons)
				return;

			if (inputPanel == null)
				return;

			inputPanel.UpdateDeltaV();
		}

		public static void maneuverLog(string message, logLevels l, params object[] objs)
		{
			message = string.Format(message, objs);
			string log = string.Format("[Maneuver_Node_Evolved] {0}", message);
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
