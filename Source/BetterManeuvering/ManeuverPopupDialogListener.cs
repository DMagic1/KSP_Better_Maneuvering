using UnityEngine;

namespace BetterManeuvering
{
	public class ManeuverPopupDialogListener : MonoBehaviour
	{
		private PopupDialog dialog;

		private void Start()
		{
			dialog = gameObject.GetComponent<PopupDialog>();

			if (dialog == null)
				Destroy(gameObject);

			ManeuverController.onPopupSpawn.Fire(dialog);
		}

		private void OnDestroy()
		{
			if (dialog != null)
				ManeuverController.onPopupDestroy.Fire(dialog);
		}
	}
}
