#region license
/*The MIT License (MIT)

ManeuverGameParams - In game settings options

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

namespace BetterManeuvering
{
	public class ManeuverGameParams : GameParameters.CustomParameterNode
	{
		[GameParameters.CustomFloatParameterUI("Base Gizmo Scale", toolTip = "The minimum scale for the maneuver node gizmo", asPercentage = true, minValue = 0.5f, maxValue = 2.5f, displayFormat = "N1", autoPersistance = true)]
		public float baseScale = 1.25f;
		[GameParameters.CustomParameterUI("Dynamic Gizmo Scaling", toolTip = "Scale the maneuver node gizmo with map zoom", autoPersistance = true)]
		public bool dynamicScaling = true;
		[GameParameters.CustomFloatParameterUI("Max Scale", toolTip = "The maximum scale for the maneuver node gizmo when using dynamic scaling", asPercentage = true, minValue = 1.5f, maxValue = 5f, displayFormat = "N1", autoPersistance = true)]
		public float maxScale = 2.5f;
		[GameParameters.CustomIntParameterUI("Orbit Selection Tolerance", toolTip = "Tolerance for assigning maneuver nodes to specific positions when clicking on the orbit path; in degrees of the orbital ellipse", minValue = 5, maxValue = 25, stepSize = 1, autoPersistance = true)]
		public int selectionTolerance = 10;
		[GameParameters.CustomParameterUI("Rotate Gizmo With Orbit", toolTip = "Align maneuver node gizmo handles and manual DeltaV input with the post-maneuver node orbit orientation", autoPersistance = true)]
		public bool alignToOrbit = true;
		[GameParameters.CustomIntParameterUI("Alignment Accuracy", minValue = 0, maxValue = 2, stepSize = 1, toolTip = "Accuracy vs performance tradeoff when the maneuver node is aligned to orbit", autoPersistance = true)]
		public int accuracy = 2;
		[GameParameters.CustomParameterUI("Replace Maneuver Gizmo Buttons", toolTip = "Use manual DeltaV input and maneuver reposition windows", autoPersistance = true)]
		public bool replaceGizmoButtons = true;
		[GameParameters.CustomParameterUI("Right Click To Close Maneuver Windows", toolTip = "Right click outside of any maneuver input window to close it when not locked", autoPersistance = true)]
		public bool rightClickClose = false;
		[GameParameters.CustomParameterUI("Pinned Windows Open In Flight View", toolTip = "Keep any pinned windows opened when closing map mode", autoPersistance = true)]
		public bool stickyOpenFlight = false;
		[GameParameters.CustomParameterUI("Remember Manual Input Values", toolTip = "New manual DeltaV input windows will always use the last-used DeltaV increment values", autoPersistance = true)]
		public bool rememberManualInput = true;
		[GameParameters.CustomParameterUI("Show Window Lines", toolTip = "Toggle the lines connecting the windows to the buttons", autoPersistance = true)]
		public bool showWindowLines = true;
		[GameParameters.CustomParameterUI("Force Maneuver Node Open", toolTip = "Forces the current maneuver node to stay open when clicking outside of the node", autoPersistance = true)]
		public bool forceOpen = true;
		[GameParameters.CustomParameterUI("Show Next/Previous Maneuver Node Buttons", toolTip = "Show buttons to cycle through multiple maneuver nodes on the maneuver button circle", autoPersistance = true)]
		public bool showManeuverCycle = true;
		[GameParameters.CustomParameterUI("Change Focus to New Maneuver Node", toolTip = "Change the camera focus when using the next/previous maneuver node buttons", autoPersistance = true)]
		public bool focusOnNextManeuver = true;
		[GameParameters.CustomParameterUI("Use Maneuver Node Keyboard Shortcut", toolTip = "Shortcut for opening/closing the currently focused node, the last opened node, or the first node", autoPersistance = true)]
		public bool useKeyboard = true;
		[GameParameters.CustomParameterUI("Use As Default", toolTip = "Save these settings to a file on disk to be used as defaults for newly created games", autoPersistance = false)]
		public bool useAsDefault;

		public ManeuverGameParams()
		{
			if (HighLogic.LoadedScene == GameScenes.MAINMENU)
			{
				if (ManeuverPersistence.Instance == null)
					return;

				dynamicScaling = ManeuverPersistence.Instance.dynamicScaling;
				baseScale = ManeuverPersistence.Instance.baseScale;
				maxScale = ManeuverPersistence.Instance.maxScale;
				forceOpen = ManeuverPersistence.Instance.forceOpen;
				selectionTolerance = ManeuverPersistence.Instance.selectionTolerance;
				alignToOrbit = ManeuverPersistence.Instance.alignToOrbit;
				accuracy = ManeuverPersistence.Instance.accuracy;
				replaceGizmoButtons = ManeuverPersistence.Instance.replaceGizmoButtons;
				rightClickClose = ManeuverPersistence.Instance.rightClickClose;
				stickyOpenFlight = ManeuverPersistence.Instance.stickyOpenFlight;
				showWindowLines = ManeuverPersistence.Instance.showWindowLines;
				rememberManualInput = ManeuverPersistence.Instance.rememberManualInput;
				showManeuverCycle = ManeuverPersistence.Instance.showManeuverCycle;
				focusOnNextManeuver = ManeuverPersistence.Instance.focusOnNextManeuver;
				useKeyboard = ManeuverPersistence.Instance.useKeyboard;
			}
		}

		public override GameParameters.GameMode GameMode
		{
			get { return GameParameters.GameMode.ANY; }
		}

		public override bool HasPresets
		{
			get { return false; }
		}

		public override string Section
		{
			get { return "DMagic Mods"; }
		}

		public override string DisplaySection
		{
			get { return "DMagic Mods"; }
		}

		public override int SectionOrder
		{
			get { return 2; }
		}

		public override string Title
		{
			get { return "Maneuver Node Evolved"; }
		}

		public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
		{
			if (member.Name == "maxScale")
				return dynamicScaling;

			if (member.Name == "accuracy")
				return alignToOrbit;

			if (member.Name == "rememberManualInput")
				return replaceGizmoButtons;

			if (member.Name == "rightClickClose")
				return replaceGizmoButtons;

			if (member.Name == "stickyOpenFlight")
				return replaceGizmoButtons;

			if (member.Name == "showWindowLines")
				return replaceGizmoButtons;

			if (member.Name == "focusOnNextManeuver")
				return showManeuverCycle;

			return true;
		}
	}
}
