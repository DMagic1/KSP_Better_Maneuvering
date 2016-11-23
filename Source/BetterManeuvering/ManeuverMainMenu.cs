#region license
/*The MIT License (MIT)

ManeuverMainMenu - Monobehaviour to attach settings file persistence logic to new game creation

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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace BetterManeuvering
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class ManeuverMainMenu : MonoBehaviour
	{
		private MainMenu menu;
		private Callback newGameTap;

		private void Start()
		{
			menu = GameObject.FindObjectOfType<MainMenu>();

			if (menu == null)
				return;

			newGameTap = menu.newGameBtn.onTap;

			menu.newGameBtn.onTap = new Callback(onTap);
		}

		private void onTap()
		{
			newGameTap.Invoke();

			StartCoroutine(AddListener());
		}

		private IEnumerator AddListener()
		{
			yield return new WaitForSeconds(0.5f);

			var buttons = GameObject.FindObjectsOfType<Button>();

			for (int i = buttons.Length - 1; i >= 0; i--)
			{
				Button button = buttons[i];

				if (button == null)
					continue;

				TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>();

				if (tmp == null)
					continue;

				if (tmp.text != "Start!")
					continue;

				button.onClick.AddListener(new UnityAction(onSettingsApply));
			}
		}

		private void onSettingsApply()
		{
			StartCoroutine(ApplySettings());
		}

		private IEnumerator ApplySettings()
		{
			while (HighLogic.CurrentGame == null)
				yield return null;

			if (ManeuverPersistence.Instance == null)
				yield break;

			ManeuverPersistence.Instance.SettingsApplied();
		}
	}
}
