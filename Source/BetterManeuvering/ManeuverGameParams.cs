
namespace BetterManeuvering
{
	public class ManeuverGameParams : GameParameters.CustomParameterNode
	{
		[GameParameters.CustomParameterUI("Dynamic Gizmo Scaling", toolTip = "Scale the maneuver node gizmo with map zoom", autoPersistance = true)]
		public bool dynamicScaling = true;
		[GameParameters.CustomFloatParameterUI("Base Gizmo Scale", minValue = 0.5f, maxValue = 2.5f, displayFormat = "P0", autoPersistance = true)]
		public float baseScale = 1.25f;
		[GameParameters.CustomFloatParameterUI("Max Scale", minValue = 1.5f, maxValue = 5f, displayFormat = "P0", autoPersistance = true)]
		public float maxScale = 2.5f;
		[GameParameters.CustomParameterUI("Rotate Gizmo With Orbit", autoPersistance = true)]
		public bool alignToOrbit = true;
		[GameParameters.CustomIntParameterUI("Alignment Accuracy", minValue = 0, maxValue = 2, stepSize = 1, toolTip = "Accuracy vs performance tradeoff when the maneuver node is aligned to orbit", autoPersistance = true)]
		public int accuracy = 2;
		[GameParameters.CustomParameterUI("Show Manual Control Button", autoPersistance = true)]
		public bool manualControls = true;
		[GameParameters.CustomParameterUI("Show Maneuver Snap Button", autoPersistance = true)]
		public bool maneuverSnap = true;
		[GameParameters.CustomParameterUI("Use Keyboard Shortcut", autoPersistance = true)]
		public bool useKeyboard = true;
		[GameParameters.CustomParameterUI("Use As Default", autoPersistance = false)]
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
				manualControls = ManeuverPersistence.Instance.manualControls;
				maneuverSnap = ManeuverPersistence.Instance.maneuverSnap;
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

			return true;
		}
	}
}
