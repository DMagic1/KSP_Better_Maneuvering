
namespace BetterManeuvering
{
	public class ManeuverGameParams : GameParameters.CustomParameterNode
	{
		[GameParameters.CustomParameterUI("Dynamic Gizmo Scaling", toolTip = "Scale the maneuver node gizmo with map zoom", autoPersistance = true)]
		public bool dynamicScaling = true;
		[GameParameters.CustomFloatParameterUI("Base Gizmo Scale", toolTip = "The minimum scale for the maneuver node gizmo", minValue = 0.5f, maxValue = 2.5f, displayFormat = "P0", autoPersistance = true)]
		public float baseScale = 1.25f;
		[GameParameters.CustomFloatParameterUI("Max Scale", toolTip = "The maximum scale for the maneuver node gizmo when using dynamic scaling", minValue = 1.5f, maxValue = 5f, displayFormat = "P0", autoPersistance = true)]
		public float maxScale = 2.5f;
		[GameParameters.CustomParameterUI("Rotate Gizmo With Orbit", toolTip = "Align maneuver node gizmo handles and manual DeltaV input with the pose-maneuver node orbit direction", autoPersistance = true)]
		public bool alignToOrbit = true;
		[GameParameters.CustomIntParameterUI("Alignment Accuracy", minValue = 0, maxValue = 2, stepSize = 1, toolTip = "Accuracy vs performance tradeoff when the maneuver node is aligned to orbit", autoPersistance = true)]
		public int accuracy = 2;
		[GameParameters.CustomParameterUI("Replace Maneuver Gizmo Buttons", toolTip = "Use manual DeltaV input and maneuver reposition windows", autoPersistance = true)]
		public bool replaceGizmoButtons = true;
		[GameParameters.CustomParameterUI("Remember Manual Input Values", toolTip = "New manual DeltaV input windows will always set the last-used DeltaV increment values", autoPersistance = true)]
		public bool rememberManualInput = true;
		[GameParameters.CustomParameterUI("Use Maneuver Node Keyboard Shortcut", toolTip = "Shortcut for opening/closing the currently focused node, the last opened node, or the first node", autoPersistance = true)]
		public bool useKeyboard = true;
		[GameParameters.CustomParameterUI("Use As Default", toolTip = "Save these settings to a file on disk to be used as default for newly created games", autoPersistance = false)]
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
				alignToOrbit = ManeuverPersistence.Instance.alignToOrbit;
				accuracy = ManeuverPersistence.Instance.accuracy;
				replaceGizmoButtons = ManeuverPersistence.Instance.replaceGizmoButtons;
				rememberManualInput = ManeuverPersistence.Instance.rememberManualInput;
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

		public override int SectionOrder
		{
			get { return 2; }
		}

		public override string Title
		{
			get { return "Better Maneuvering"; }
		}

		public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
		{
			if (member.Name == "maxScale")
				return dynamicScaling;

			if (member.Name == "accuracy")
				return alignToOrbit;

			if (member.Name == "rememberManualInput")
				return replaceGizmoButtons;

			return true;
		}
	}
}
