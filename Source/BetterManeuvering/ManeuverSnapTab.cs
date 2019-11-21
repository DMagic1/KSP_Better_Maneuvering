
using BetterManeuvering.Unity;
using KSP.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace BetterManeuvering
{
    public class ManeuverSnapTab : ManeuverNodeEditorTab
    {
        private Orbit _patch;
        private Vector3d _oldDeltaV;
        private int _index;
        private int _orbitsAdded;

        private double _startUT;
        private double apoUT, periUT, nextOUT, prevOUT, nextPUT, prevPUT, eqAscUT, eqDescUT, relAscUT, relDescUT, clAppUT;
        
        private ManeuverSnapPanelTab _snapTab;

        private ManeuverNodeEditorManager _manager;        

        //private void Awake()
        //{
        //    tabName = "ManeuverSnap";
        //    tabTooltipCaptionActive = "Maneuver position editor";
        //    tabTooltipCaptionInactive = "Maneuver position editor";
        //    tabManagesCaption = true;
        //    tabPosition = ManeuverNodeEditorTabPosition.RIGHT;

        //    tabIconOn = ManeuverLoader.SnapPanelTabIconOn;
        //    tabIconOff = ManeuverLoader.SnapPanelTabIconOff;

        //    ManeuverController.maneuverLog("Tab Awake", logLevels.log);
        //}

        public void SetTabValues()
        {
            tabName = "ManeuverSnap";
            tabTooltipCaptionActive = "Maneuver position editor";
            tabTooltipCaptionInactive = "Maneuver position editor";
            tabManagesCaption = true;
            tabPosition = ManeuverNodeEditorTabPosition.RIGHT;

            tabIconOn = ManeuverLoader.SnapPanelTabIconOn;
            tabIconOff = ManeuverLoader.SnapPanelTabIconOff;

           // ManeuverController.maneuverLog("Tab Set Values", logLevels.log);
        }

        public override void SetInitialValues()
        {
            attachUI();

            setup();

            UpdateUIElements();
        }

        public override void UpdateUIElements()
        {
            checkOrbit();

            UpdateTimers();
        }

        public void setup()
        {
            if (ManeuverNodeEditorManager.Instance == null)
                return;

            _manager = ManeuverNodeEditorManager.Instance;

            if (_manager.SelectedManeuverNode != null)
            {
                SetFields();
            }
        }

        private void attachUI()
        {
            _snapTab = gameObject.GetComponent<ManeuverSnapPanelTab>();

            //ManeuverController.maneuverLog("Attach 1", logLevels.log);

            if (_snapTab == null)
                return;

            ClearListeners();

            //ManeuverController.maneuverLog("Attach 2", logLevels.log);

            _snapTab.NextOrbitButton.onClick.AddListener(new UnityAction(nextOrbit));
            _snapTab.PreviousOrbitButton.onClick.AddListener(new UnityAction(previousOrbit));
            _snapTab.ApoButton.onClick.AddListener(new UnityAction(apoapsis));
            _snapTab.PeriButton.onClick.AddListener(new UnityAction(periapsis));
            _snapTab.NextPatchButton.onClick.AddListener(new UnityAction(nextPatch));
            _snapTab.PreviousPatchButton.onClick.AddListener(new UnityAction(previousPatch));
            _snapTab.EqAscButton.onClick.AddListener(new UnityAction(eqAsc));
            _snapTab.EqDescButton.onClick.AddListener(new UnityAction(eqDesc));
            _snapTab.RelAscButton.onClick.AddListener(new UnityAction(relAsc));
            _snapTab.RelDescButton.onClick.AddListener(new UnityAction(relDesc));
            _snapTab.ClAppButton.onClick.AddListener(new UnityAction(clApp));
            _snapTab.ResetButton.onClick.AddListener(new UnityAction(reset));
        }

        private void ClearListeners()
        {
            if (_snapTab == null)
                return;

            _snapTab.NextOrbitButton.onClick.RemoveAllListeners();
            _snapTab.PreviousOrbitButton.onClick.RemoveAllListeners();
            _snapTab.ApoButton.onClick.RemoveAllListeners();
            _snapTab.PeriButton.onClick.RemoveAllListeners();
            _snapTab.NextPatchButton.onClick.RemoveAllListeners();
            _snapTab.PreviousPatchButton.onClick.RemoveAllListeners();
            _snapTab.EqAscButton.onClick.RemoveAllListeners();
            _snapTab.EqDescButton.onClick.RemoveAllListeners();
            _snapTab.RelAscButton.onClick.RemoveAllListeners();
            _snapTab.RelDescButton.onClick.RemoveAllListeners();
            _snapTab.ClAppButton.onClick.RemoveAllListeners();
            _snapTab.ResetButton.onClick.RemoveAllListeners();
        }

        private void SetFields()
        {
            _patch = _manager.SelectedManeuverNode.patch;
            _oldDeltaV = _manager.SelectedManeuverNode.DeltaV;
            _startUT = _manager.SelectedManeuverNode.UT;
            _index = ManeuverController.Instance.LastManeuverIndex;
            _orbitsAdded = _manager.SelectedManeuverNode.attachedGizmo.orbitsAdded;
        }

        private void UpdateTimers()
        {
            if (_snapTab == null || _manager.SelectedManeuverNode == null)
                return;

            if (_patch == null)
                SetFields();

            double UT = Planetarium.GetUniversalTime();

            //_snapTab.CurrentTime.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: T {1}", _index + 1, KSPUtil.PrintTime(UT - _manager.SelectedManeuverNode.UT, 3, true)));

            double periods = 0;

            if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                periods = (_orbitsAdded * _patch.period);

            if (_snapTab.ApoButton.gameObject.activeSelf)
                _snapTab.ApoTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime((apoUT + periods) - UT, 3, false)));

            if (_snapTab.PeriButton.gameObject.activeSelf)
                _snapTab.PeriTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime((periUT + periods) - UT, 3, false)));

            if (_snapTab.NextOrbitButton.gameObject.activeSelf)
                _snapTab.NOTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(nextOUT - UT, 3, false)));

            if (_snapTab.PreviousOrbitButton.gameObject.activeSelf)
                _snapTab.POTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(prevOUT - UT, 3, false)));

            if (_snapTab.NextPatchButton.gameObject.activeSelf)
                _snapTab.NPTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(nextPUT - UT, 3, false)));

            if (_snapTab.PreviousPatchButton.gameObject.activeSelf)
                _snapTab.PPTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(prevPUT - UT, 3, false)));

            if (_snapTab.EqAscButton.gameObject.activeSelf)
                _snapTab.EqAscTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime((eqAscUT + periods) - UT, 3, false)));

            if (_snapTab.EqDescButton.gameObject.activeSelf)
                _snapTab.EqDescTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime((eqDescUT + periods) - UT, 3, false)));

            if (_snapTab.RelAscButton.gameObject.activeSelf)
                _snapTab.RelAscTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime((relAscUT + periods) - UT, 3, false)));

            if (_snapTab.RelDescButton.gameObject.activeSelf)
                _snapTab.RelDescTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime((relDescUT + periods) - UT, 3, false)));

            if (_snapTab.ClAppButton.gameObject.activeSelf)
                _snapTab.ApproachTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(clAppUT - UT, 3, false)));

            //_snapTab.CurrentTime.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: T {1}", _index + 1, KSPUtil.PrintTime(UT - _manager.SelectedManeuverNode.UT, 3, true)));

            _snapTab.ResetTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(_startUT - UT, 3, false)));
        }

        private void checkOrbit()
        {
            if (_manager.SelectedManeuverNode == null || _snapTab == null)
                return;

            if (_patch == null)
                SetFields();

            double UT = Planetarium.GetUniversalTime();

            //_snapTab.CurrentTime.OnTextUpdate.Invoke(string.Format("Maneuver Node #{0}: T {1}", _index + 1, KSPUtil.PrintTime(UT - _manager.SelectedManeuverNode.UT, 3, true)));
            _snapTab.ResetTime.OnTextUpdate.Invoke(string.Format("{0}", KSPUtil.PrintTime(_startUT - UT, 3, false)));

            if (_patch.eccentricity >= 1)
            {
                if (_manager.SelectedManeuverNode.attachedGizmo != null)
                    _manager.SelectedManeuverNode.attachedGizmo.orbitsAdded = 0;

                _orbitsAdded = 0;

                _snapTab.NextOrbitButton.gameObject.SetActive(false);
                _snapTab.PreviousOrbitButton.gameObject.SetActive(false);

                _snapTab.ApoButton.gameObject.SetActive(false);

                if (_patch.timeToPe < 0)
                    _snapTab.PeriButton.gameObject.SetActive(false);
                else if (_patch.PeR < 0)
                    _snapTab.PeriButton.gameObject.SetActive(false);
                else if (_patch.UTsoi > 0 && _patch.timeToPe + _patch.StartUT > _patch.UTsoi)
                    _snapTab.PeriButton.gameObject.SetActive(false);
                else
                {
                    _snapTab.PeriButton.gameObject.SetActive(true);
                    periUT = _patch.StartUT + _patch.timeToPe;
                }

                if ((_patch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER || _patch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE || _patch.patchEndTransition == Orbit.PatchTransitionType.MANEUVER)
                    && (_patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE))
                {
                    _snapTab.NextPatchButton.gameObject.SetActive(true);

                    if (_patch.nextPatch.nextPatch.UTsoi > 0)
                        nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
                    else if (_patch.nextPatch.nextPatch.eccentricity < 1 && !double.IsNaN(_patch.nextPatch.nextPatch.period) && !double.IsInfinity(_patch.nextPatch.nextPatch.period))
                        nextPUT = _patch.nextPatch.nextPatch.StartUT + (_patch.nextPatch.nextPatch.period / 2);
                    else
                        nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.EndUT - _patch.nextPatch.nextPatch.StartUT) / 2);
                }
                else
                    _snapTab.NextPatchButton.gameObject.SetActive(false);

                if (_patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
                    _snapTab.PreviousPatchButton.gameObject.SetActive(false);
                else
                {
                    _snapTab.PreviousPatchButton.gameObject.SetActive(true);

                    if (_patch.previousPatch.UTsoi > 0)
                        prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
                    else if (_patch.previousPatch.eccentricity < 1 && !double.IsNaN(_patch.previousPatch.period) && !double.IsInfinity(_patch.previousPatch.period))
                        prevPUT = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
                    else
                        prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);
                }

                eqAscUT = ManeuverUtilities.EqAscTime(_patch);

                if (_patch.UTsoi > 0 && eqAscUT > _patch.UTsoi)
                    _snapTab.EqAscButton.gameObject.SetActive(false);
                else if (eqAscUT < UT)
                    _snapTab.EqAscButton.gameObject.SetActive(false);
                else
                    _snapTab.EqAscButton.gameObject.SetActive(true);

                eqDescUT = ManeuverUtilities.EqDescTime(_patch);

                if (_patch.UTsoi > 0 && eqDescUT > _patch.UTsoi)
                    _snapTab.EqDescButton.gameObject.SetActive(false);
                else if (eqDescUT < UT)
                    _snapTab.EqDescButton.gameObject.SetActive(false);
                else
                    _snapTab.EqDescButton.gameObject.SetActive(true);

                ITargetable target = FlightGlobals.fetch.VesselTarget;

                if (target == null)
                {
                    _snapTab.RelAscButton.gameObject.SetActive(false);
                    _snapTab.RelDescButton.gameObject.SetActive(false);
                    _snapTab.ClAppButton.gameObject.SetActive(false);
                }
                else
                {
                    Orbit tgtPatch = target.GetOrbit();

                    if (tgtPatch.referenceBody != _patch.referenceBody)
                    {
                        _snapTab.RelAscButton.gameObject.SetActive(false);
                        _snapTab.RelDescButton.gameObject.SetActive(false);
                        _snapTab.ClAppButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        relAscUT = ManeuverUtilities.RelAscTime(_patch, target);

                        if (_patch.UTsoi > 0 && relAscUT > _patch.UTsoi)
                            _snapTab.RelAscButton.gameObject.SetActive(false);
                        else if (relAscUT < UT)
                            _snapTab.RelAscButton.gameObject.SetActive(false);
                        else
                            _snapTab.RelAscButton.gameObject.SetActive(true);

                        relDescUT = ManeuverUtilities.RelDescTime(_patch, target);

                        if (_patch.UTsoi > 0 && relDescUT > _patch.UTsoi)
                            _snapTab.RelDescButton.gameObject.SetActive(false);
                        else if (relDescUT < UT)
                            _snapTab.RelDescButton.gameObject.SetActive(false);
                        else
                            _snapTab.RelDescButton.gameObject.SetActive(true);

                        if (target.GetVessel() == null)
                            clAppUT = _patch.closestTgtApprUT;
                        else
                            clAppUT = ManeuverUtilities.closestVessel(0, _patch, tgtPatch, true, 0, 0);

                        if (clAppUT <= 0)
                            _snapTab.ClAppButton.gameObject.SetActive(false);
                        else if (_patch.UTsoi > 0 && clAppUT > _patch.UTsoi)
                            _snapTab.ClAppButton.gameObject.SetActive(false);
                        else if (clAppUT < UT)
                            _snapTab.ClAppButton.gameObject.SetActive(false);
                        else
                            _snapTab.ClAppButton.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if (_patch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
                {
                    if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    {
                        _snapTab.NextOrbitButton.gameObject.SetActive(true);
                        nextOUT = _manager.SelectedManeuverNode.UT + _patch.period;
                    }
                    else
                        _snapTab.NextOrbitButton.gameObject.SetActive(false);

                    if (double.IsNaN(_patch.period) || double.IsInfinity(_patch.period) || _manager.SelectedManeuverNode.UT - _patch.period < UT)
                        _snapTab.PreviousOrbitButton.gameObject.SetActive(false);
                    else
                    {
                        _snapTab.PreviousOrbitButton.gameObject.SetActive(true);
                        prevOUT = _manager.SelectedManeuverNode.UT - _patch.period;
                    }

                    _snapTab.ApoButton.gameObject.SetActive(true);
                    apoUT = _patch.StartUT + _patch.timeToAp;

                    _snapTab.PeriButton.gameObject.SetActive(true);
                    periUT = _patch.StartUT + _patch.timeToPe;

                    _snapTab.NextPatchButton.gameObject.SetActive(false);

                    if (_patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
                        _snapTab.PreviousPatchButton.gameObject.SetActive(false);
                    else
                    {
                        _snapTab.PreviousPatchButton.gameObject.SetActive(true);

                        if (_patch.previousPatch.UTsoi > 0)
                            prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
                        else if (_patch.previousPatch.eccentricity < 1 && !double.IsNaN(_patch.previousPatch.period) && !double.IsInfinity(_patch.previousPatch.period))
                            prevPUT = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
                        else
                            prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);
                    }

                    _snapTab.EqAscButton.gameObject.SetActive(true);
                    eqAscUT = ManeuverUtilities.EqAscTime(_patch);

                    _snapTab.EqDescButton.gameObject.SetActive(true);
                    eqDescUT = ManeuverUtilities.EqDescTime(_patch);

                    ITargetable target = FlightGlobals.fetch.VesselTarget;

                    if (target == null)
                    {
                        _snapTab.RelAscButton.gameObject.SetActive(false);
                        _snapTab.RelDescButton.gameObject.SetActive(false);
                        _snapTab.ClAppButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        Orbit tgtPatch = target.GetOrbit();

                        if (tgtPatch.referenceBody != _patch.referenceBody)
                        {
                            _snapTab.RelAscButton.gameObject.SetActive(false);
                            _snapTab.RelDescButton.gameObject.SetActive(false);
                            _snapTab.ClAppButton.gameObject.SetActive(false);
                        }
                        else
                        {
                            _snapTab.RelAscButton.gameObject.SetActive(true);
                            relAscUT = ManeuverUtilities.RelAscTime(_patch, target);

                            _snapTab.RelDescButton.gameObject.SetActive(true);
                            relDescUT = ManeuverUtilities.RelDescTime(_patch, target);

                            if (target.GetVessel() == null)
                                clAppUT = _patch.closestTgtApprUT;
                            else
                                clAppUT = ManeuverUtilities.closestVessel(0, _patch, tgtPatch, true, 0, 0);

                            if (clAppUT <= 0)
                                _snapTab.ClAppButton.gameObject.SetActive(false);
                            else
                                _snapTab.ClAppButton.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    if (_manager.SelectedManeuverNode.attachedGizmo != null)
                        _manager.SelectedManeuverNode.attachedGizmo.orbitsAdded = 0;

                    _orbitsAdded = 0;

                    _snapTab.NextOrbitButton.gameObject.SetActive(false);
                    _snapTab.PreviousOrbitButton.gameObject.SetActive(false);

                    if (_patch.timeToAp < 0)
                        _snapTab.ApoButton.gameObject.SetActive(false);
                    if (_patch.UTsoi > 0 && _patch.timeToAp + _patch.StartUT > _patch.UTsoi)
                        _snapTab.ApoButton.gameObject.SetActive(false);
                    if (_patch.ApA > _patch.referenceBody.sphereOfInfluence)
                        _snapTab.ApoButton.gameObject.SetActive(false);
                    else
                    {
                        _snapTab.ApoButton.gameObject.SetActive(true);
                        apoUT = _patch.StartUT + _patch.timeToAp;
                    }

                    if (_patch.timeToPe < 0)
                        _snapTab.PeriButton.gameObject.SetActive(false);
                    else if (_patch.PeR < 0)
                        _snapTab.PeriButton.gameObject.SetActive(false);
                    else if (_patch.UTsoi > 0 && _patch.timeToPe + _patch.StartUT > _patch.UTsoi)
                        _snapTab.PeriButton.gameObject.SetActive(false);
                    else
                    {
                        _snapTab.PeriButton.gameObject.SetActive(true);
                        periUT = _patch.StartUT + _patch.timeToPe;
                    }

                    if ((_patch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER || _patch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE || _patch.patchEndTransition == Orbit.PatchTransitionType.MANEUVER)
                        && (_patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE))
                    {
                        _snapTab.NextPatchButton.gameObject.SetActive(true);

                        if (_patch.nextPatch.nextPatch.UTsoi > 0)
                            nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
                        else if (_patch.nextPatch.nextPatch.eccentricity < 1 && !double.IsNaN(_patch.nextPatch.nextPatch.period) && !double.IsInfinity(_patch.nextPatch.nextPatch.period))
                            nextPUT = _patch.nextPatch.nextPatch.StartUT + (_patch.nextPatch.nextPatch.period / 2);
                        else
                            nextPUT = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.EndUT - _patch.nextPatch.nextPatch.StartUT) / 2);
                    }
                    else
                        _snapTab.NextPatchButton.gameObject.SetActive(false);

                    if (_patch.patchStartTransition == Orbit.PatchTransitionType.INITIAL || _patch.patchStartTransition == Orbit.PatchTransitionType.MANEUVER)
                        _snapTab.PreviousPatchButton.gameObject.SetActive(false);
                    else
                    {
                        _snapTab.PreviousPatchButton.gameObject.SetActive(true);

                        if (_patch.previousPatch.UTsoi > 0)
                            prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.UTsoi - _patch.previousPatch.StartUT) / 2);
                        else if (_patch.previousPatch.eccentricity < 1 && !double.IsNaN(_patch.previousPatch.period) && !double.IsInfinity(_patch.previousPatch.period))
                            prevPUT = _patch.previousPatch.StartUT + (_patch.previousPatch.period / 2);
                        else
                            prevPUT = _patch.previousPatch.StartUT + ((_patch.previousPatch.EndUT - _patch.previousPatch.StartUT) / 2);
                    }

                    eqAscUT = ManeuverUtilities.EqAscTime(_patch);

                    if (_patch.UTsoi > 0 && eqAscUT > _patch.UTsoi)
                        _snapTab.EqAscButton.gameObject.SetActive(false);
                    else if (eqAscUT < UT)
                        _snapTab.EqAscButton.gameObject.SetActive(false);
                    else
                        _snapTab.EqAscButton.gameObject.SetActive(true);

                    eqDescUT = ManeuverUtilities.EqDescTime(_patch);

                    if (_patch.UTsoi > 0 && eqDescUT > _patch.UTsoi)
                        _snapTab.EqDescButton.gameObject.SetActive(false);
                    else if (eqDescUT < UT)
                        _snapTab.EqDescButton.gameObject.SetActive(false);
                    else
                        _snapTab.EqDescButton.gameObject.SetActive(true);

                    ITargetable target = FlightGlobals.fetch.VesselTarget;

                    if (target == null)
                    {
                        _snapTab.RelAscButton.gameObject.SetActive(false);
                        _snapTab.RelDescButton.gameObject.SetActive(false);
                        _snapTab.ClAppButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        Orbit tgtPatch = target.GetOrbit();

                        if (tgtPatch.referenceBody != _patch.referenceBody)
                        {
                            _snapTab.RelAscButton.gameObject.SetActive(false);
                            _snapTab.RelDescButton.gameObject.SetActive(false);
                            _snapTab.ClAppButton.gameObject.SetActive(false);
                        }
                        else
                        {
                            relAscUT = ManeuverUtilities.RelAscTime(_patch, target);

                            if (_patch.UTsoi > 0 && relAscUT > _patch.UTsoi)
                                _snapTab.RelAscButton.gameObject.SetActive(false);
                            else if (relAscUT < UT)
                                _snapTab.RelAscButton.gameObject.SetActive(false);
                            else
                                _snapTab.RelAscButton.gameObject.SetActive(true);

                            relDescUT = ManeuverUtilities.RelDescTime(_patch, target);

                            if (_patch.UTsoi > 0 && relDescUT > _patch.UTsoi)
                                _snapTab.RelDescButton.gameObject.SetActive(false);
                            else if (relDescUT < UT)
                                _snapTab.RelDescButton.gameObject.SetActive(false);
                            else
                                _snapTab.RelDescButton.gameObject.SetActive(true);

                            if (target.GetVessel() == null)
                                clAppUT = _patch.closestTgtApprUT;
                            else
                                clAppUT = ManeuverUtilities.closestVessel(0, _patch, tgtPatch, true, 0, 0);

                            if (clAppUT <= 0)
                                _snapTab.ClAppButton.gameObject.SetActive(false);
                            else if (_patch.UTsoi > 0 && clAppUT > _patch.UTsoi)
                                _snapTab.ClAppButton.gameObject.SetActive(false);
                            else if (clAppUT < UT)
                                _snapTab.ClAppButton.gameObject.SetActive(false);
                            else
                                _snapTab.ClAppButton.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        private void setNodeTime(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time))
                return;


            if (_manager.SelectedManeuverNode.attachedGizmo != null)
            {
                _manager.SelectedManeuverNode.attachedGizmo.UT = time;
                _manager.SelectedManeuverNode.attachedGizmo.OnGizmoUpdated(_manager.SelectedManeuverNode.DeltaV, time);
            }
            else
            {
                _manager.SelectedManeuverNode.UT = time;
                _manager.SelectedManeuverNode.solver.UpdateFlightPlan();
            }

            _patch = _manager.SelectedManeuverNode.patch;
            _oldDeltaV = _manager.SelectedManeuverNode.DeltaV;

            UpdateUIElements();
        }

        private void nextOrbit()
        {
            if (_patch == null || _patch.eccentricity >= 1)
                return;

            if (double.IsNaN(_patch.period) || double.IsInfinity(_patch.period))
                return;

            if (_manager.SelectedManeuverNode.attachedGizmo != null)
                _manager.SelectedManeuverNode.attachedGizmo.orbitsAdded += 1;

            _orbitsAdded += 1;

            setNodeTime(_manager.SelectedManeuverNode.UT + _patch.period);
        }

        private void previousOrbit()
        {
            if (_patch == null || _patch.eccentricity >= 1)
                return;

            if (double.IsNaN(_patch.period) || double.IsInfinity(_patch.period))
                return;

            double time = _manager.SelectedManeuverNode.UT - _patch.period;

            if (time < Planetarium.GetUniversalTime())
                return;

            if (_manager.SelectedManeuverNode.attachedGizmo != null)
            {
                _manager.SelectedManeuverNode.attachedGizmo.orbitsAdded -= 1;

                if (_manager.SelectedManeuverNode.attachedGizmo.orbitsAdded < 0)
                    _manager.SelectedManeuverNode.attachedGizmo.orbitsAdded = 0;
            }

            _orbitsAdded -= 1;

            if (_orbitsAdded < 0)
                _orbitsAdded = 0;

            setNodeTime(time);
        }

        private void nextPatch()
        {
            if (_patch == null || _patch.patchEndTransition == Orbit.PatchTransitionType.FINAL || _patch.nextPatch == null || _patch.nextPatch.nextPatch == null || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL || _patch.nextPatch.patchEndTransition == Orbit.PatchTransitionType.IMPACT)
                return;

            double time = 0;

            if (_patch.nextPatch.nextPatch.UTsoi > 0)
                time = _patch.nextPatch.nextPatch.StartUT + ((_patch.nextPatch.nextPatch.UTsoi - _patch.nextPatch.nextPatch.StartUT) / 2);
            else if (_patch.nextPatch.nextPatch.eccentricity < 1 && !double.IsNaN(_patch.nextPatch.nextPatch.period) && !double.IsInfinity(_patch.nextPatch.nextPatch.period))
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
            else if (_patch.previousPatch.eccentricity < 1 && !double.IsNaN(_patch.previousPatch.period) && !double.IsInfinity(_patch.previousPatch.period))
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
            {
                double periods = 0;

                if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    periods = (_orbitsAdded * _patch.period);

                setNodeTime(_patch.StartUT + _patch.timeToAp + periods);
            }
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
            {
                double periods = 0;

                if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    periods = (_orbitsAdded * _patch.period);

                setNodeTime(_patch.StartUT + _patch.timeToPe + periods);
            }
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
            {
                double periods = 0;

                if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    periods = (_orbitsAdded * _patch.period);

                setNodeTime(eqAsc + periods);
            }
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
            {
                double periods = 0;

                if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    periods = (_orbitsAdded * _patch.period);

                setNodeTime(eqDesc + periods);
            }
        }

        private void relAsc()
        {
            if (_patch == null || _patch.referenceBody == null)
                return;

            //ManeuverController.maneuverLog("Rel Asc: {0}", logLevels.log, 1);

            ITargetable tgt = FlightGlobals.fetch.VesselTarget;

            if (tgt == null || tgt.GetOrbit() == null || tgt.GetOrbit().referenceBody == null)
                return;

            //ManeuverController.maneuverLog("Rel Asc: {0}", logLevels.log, 2);

            if (_patch.referenceBody != tgt.GetOrbit().referenceBody)
                return;

            //ManeuverController.maneuverLog("Rel Asc: {0}", logLevels.log, 3);

            double relAsc = ManeuverUtilities.RelAscTime(_patch, tgt);

            if (_patch.UTsoi > 0 && relAsc > _patch.UTsoi)
                return;
            else if (relAsc < Planetarium.GetUniversalTime())
                return;
            else
            {
                //ManeuverController.maneuverLog("Rel Asc: {0}", logLevels.log, 4);

                double periods = 0;

                if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    periods = (_orbitsAdded * _patch.period);

                setNodeTime(relAsc + periods);
            }
        }

        private void relDesc()
        {
            if (_patch == null || _patch.referenceBody == null)
                return;

            ITargetable tgt = FlightGlobals.fetch.VesselTarget;

            if (tgt == null || tgt.GetOrbit() == null || tgt.GetOrbit().referenceBody == null)
                return;

            if (_patch.referenceBody != tgt.GetOrbit().referenceBody)
                return;

            double relDesc = ManeuverUtilities.RelDescTime(_patch, tgt);

            if (_patch.UTsoi > 0 && relDesc > _patch.UTsoi)
                return;
            else if (relDesc < Planetarium.GetUniversalTime())
                return;
            else
            {
                double periods = 0;

                if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period))
                    periods = (_orbitsAdded * _patch.period);

                setNodeTime(relDesc + periods);
            }
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

            if (!double.IsNaN(_patch.period) && !double.IsInfinity(_patch.period) && _patch.eccentricity < 1 && _patch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
                _orbitsAdded = (int)((_startUT - _patch.StartUT) / _patch.period);
        }
    }
}
