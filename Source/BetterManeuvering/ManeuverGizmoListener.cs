
using System.Collections;
using UnityEngine;

namespace BetterManeuvering
{
	public class ManeuverGizmoListener : MonoBehaviour
	{
		private ManeuverGizmo gizmo;

		public bool secondary;

		private void Start()
		{
			gizmo = gameObject.GetComponent<ManeuverGizmo>();

			if (gizmo == null)
				Destroy(gameObject);

			StartCoroutine(waitForStart());
		}

		private IEnumerator waitForStart()
		{
			while (gizmo.camera == null)
				yield return null;

			ManeuverController.onGizmoSpawn.Fire(gizmo);
		}

		private void OnDestroy()
		{
			if (gizmo != null)
				ManeuverController.onGizmoDestroy.Fire(gizmo);
		}
	}
}
