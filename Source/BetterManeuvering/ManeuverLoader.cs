﻿#region license
/*The MIT License (MIT)

ManeuverLoader - Script for processing UI prefab and loading files from disk

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
using UnityEngine.UI;
using TMPro;
using BetterManeuvering.Unity;

namespace BetterManeuvering
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ManeuverLoader : MonoBehaviour
	{
		private const string bundleName = "/better_maneuver_prefabs";

		private static bool loaded;
		private static bool TMPLoaded;
		private static bool UILoaded;
		private static bool TexturesLoaded;

		private static GameObject[] loadedPrefabs;

		private static GameObject _inputPrefab;
		private static GameObject _snapPrefab;
        private static GameObject _snapTabPrefab;

		private static Sprite _inputButtonNormal;
		private static Sprite _inputButtonHighlight;
		private static Sprite _inputButtonActive;

		private static Sprite _snapButtonNormal;
		private static Sprite _snapButtonHighlight;
		private static Sprite _snapButtonActive;

		private static Sprite _nextButtonNormal;
		private static Sprite _nextButtonHighlight;
		private static Sprite _nextButtonActive;
		private static Sprite _nextButtonInactive;

		private static Sprite _prevButtonNormal;
		private static Sprite _prevButtonHighlight;
		private static Sprite _prevButtonActive;
		private static Sprite _prevButtonInactive;

		private static Sprite _ButtonNormal;
		private static Sprite _ButtonHighlight;
		private static Sprite _ButtonActive;
		private static Sprite _ButtonInactive;

		private static Sprite _ToggleNormal;
		private static Sprite _ToggleHightlight;
		private static Sprite _ToggleActive;
		private static Sprite _ToggleInactive;
		private static Sprite _ToggleCheckmark;
		
		private static Sprite _WindowBackground;
		private static Color _WindowColor;

		private static Sprite _TitleBackground;
		private static Color _TitleColor;

        private static Sprite _TabPanelBackground;
        private static Sprite _TabButtonBackground;
        private static Sprite _TabTextBackground;

        private static Sprite _TabOrbitButtonNormal;
        private static Sprite _TabOrbitButtonHighlight;
        private static Sprite _TabOrbitButtonActive;
        private static Sprite _TabOrbitButtonInactive;

        private static Sprite _SnapPanelTabIconOn;
        private static Sprite _SnapPanelTabIconOff;

        private static Material UIMaterial;

		private static Color _lineColor;
		private static float _lineCornerRadius;
		private static float _lineWidth;
		private static Material _lineMaterial;

		public static GameObject InputPrefab
		{
			get { return _inputPrefab; }
		}

		public static GameObject SnapPrefab
		{
			get { return _snapPrefab; }
		}

        public static GameObject SnapTabPrefab
        {
            get { return _snapTabPrefab; }
        }

        public static Color LineColor
		{
			get { return _lineColor; }
		}

		public static float LineCornerRadius
		{
			get { return _lineCornerRadius; }
		}

		public static float LineWidth
		{
			get { return _lineWidth; }
		}

		public static Material LineMaterial
		{
			get { return _lineMaterial; }
		}

		public static Sprite InputButtonNormal
		{
			get { return _inputButtonNormal; }
		}

		public static Sprite InputButtonHighlight
		{
			get { return _inputButtonHighlight; }
		}

		public static Sprite InputButtonActive
		{
			get { return _inputButtonActive; }
		}

		public static Sprite SnapButtonNormal
		{
			get { return _snapButtonNormal; }
		}

		public static Sprite SnapButtonHighlight
		{
			get { return _snapButtonHighlight; }
		}

		public static Sprite SnapButtonActive
		{
			get { return _snapButtonActive; }
		}

		public static Sprite NextButtonNormal
		{
			get { return _nextButtonNormal; }
		}

		public static Sprite NextButtonHighlight
		{
			get { return _nextButtonHighlight; }
		}

		public static Sprite NextButtonActive
		{
			get { return _nextButtonActive; }
		}

		public static Sprite NextButtonInactive
		{
			get { return _nextButtonInactive; }
		}

		public static Sprite PrevButtonNormal
		{
			get { return _prevButtonNormal; }
		}

		public static Sprite PrevButtonHighlight
		{
			get { return _prevButtonHighlight; }
		}

		public static Sprite PrevButtonActive
		{
			get { return _prevButtonActive; }
		}

		public static Sprite PrevButtonInactive
		{
			get { return _prevButtonInactive; }
		}

        public static Sprite SnapPanelTabIconOn
        {
            get { return _SnapPanelTabIconOn; }
        }

        public static Sprite SnapPanelTabIconOff
        {
            get { return _SnapPanelTabIconOff; }
        }

        private IEnumerator Start()
		{
			if (loaded)
			{
				Destroy(gameObject);
				yield break;
			}

			if (loadedPrefabs == null)
			{
				string path = KSPUtil.ApplicationRootPath + "GameData/ManeuverNodeEvolved/Resources";

				AssetBundle prefabs = AssetBundle.LoadFromFile(path + bundleName);

				if (prefabs != null)
					loadedPrefabs = prefabs.LoadAllAssets<GameObject>();
			}

			if (loadedPrefabs != null)
			{
				if (!TMPLoaded)
					processTMPPrefabs();

                while (UIPartActionController.Instance == null)
                    yield return null;

                while (ManeuverNodeEditorManager.Instance == null)
                    yield return null;

                //while (!ManeuverNodeEditorManager.Instance.IsReady)
                //    yield return null;

                if (!UILoaded)
					processUIPrefabs();
			}

			if (!TexturesLoaded)
			{
				loadTextures();
			}

			if (TMPLoaded && UILoaded && TexturesLoaded)
				loaded = true;

			Destroy(gameObject);
		}

		private void loadTextures()
		{
			Texture2D inputNormal = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Input_Normal", false);
			Texture2D inputHighlight = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Input_Highlight", false);
			Texture2D inputActive = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Input_Active", false);

			Texture2D snapNormal = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Snap_Normal", false);
			Texture2D snapHighlight = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Snap_Highlight", false);
			Texture2D snapActive = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Snap_Active", false);

			Texture2D nextNormal = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Next_Normal", false);
			Texture2D nextHighlight = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Next_Highlight", false);
			Texture2D nextActive = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Next_Active", false);
			Texture2D nextInactive = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Next_InActive", false);

			Texture2D prevNormal = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Previous_Normal", false);
			Texture2D prevHighlight = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Previous_Highlight", false);
			Texture2D prevActive = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Previous_Active", false);
			Texture2D prevInactive = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/Previous_InActive", false);

            Texture2D snapOnIcon = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/SnapPanel_OnIcon", false);
            Texture2D snapOffIcon = GameDatabase.Instance.GetTexture("ManeuverNodeEvolved/Resources/SnapPanel_OffIcon", false);

            if (inputNormal != null && inputHighlight != null && inputActive != null)
			{
				_inputButtonNormal = Sprite.Create(inputNormal, new Rect(0, 0, inputNormal.width, inputNormal.height), new Vector2(0.5f, 0.5f));
				_inputButtonHighlight = Sprite.Create(inputHighlight, new Rect(0, 0, inputHighlight.width, inputHighlight.height), new Vector2(0.5f, 0.5f));
				_inputButtonActive = Sprite.Create(inputActive, new Rect(0, 0, inputActive.width, inputActive.height), new Vector2(0.5f, 0.5f));
			}

			if (snapNormal != null && snapHighlight != null && snapActive != null)
			{
				_snapButtonNormal = Sprite.Create(snapNormal, new Rect(0, 0, snapNormal.width, snapNormal.height), new Vector2(0.5f, 0.5f));
				_snapButtonHighlight = Sprite.Create(snapHighlight, new Rect(0, 0, snapHighlight.width, snapHighlight.height), new Vector2(0.5f, 0.5f));
				_snapButtonActive = Sprite.Create(snapActive, new Rect(0, 0, snapActive.width, snapActive.height), new Vector2(0.5f, 0.5f));
			}

			if (nextNormal != null && nextHighlight != null && nextActive != null && nextInactive != null)
			{
				_nextButtonNormal = Sprite.Create(nextNormal, new Rect(0, 0, inputNormal.width, inputNormal.height), new Vector2(0.5f, 0.5f));
				_nextButtonHighlight = Sprite.Create(nextHighlight, new Rect(0, 0, inputHighlight.width, inputHighlight.height), new Vector2(0.5f, 0.5f));
				_nextButtonActive = Sprite.Create(nextActive, new Rect(0, 0, inputActive.width, inputActive.height), new Vector2(0.5f, 0.5f));
				_nextButtonInactive = Sprite.Create(nextInactive, new Rect(0, 0, inputActive.width, inputActive.height), new Vector2(0.5f, 0.5f));
			}

			if (prevNormal != null && prevHighlight != null && prevActive != null && prevInactive != null)
			{
				_prevButtonNormal = Sprite.Create(prevNormal, new Rect(0, 0, snapNormal.width, snapNormal.height), new Vector2(0.5f, 0.5f));
				_prevButtonHighlight = Sprite.Create(prevHighlight, new Rect(0, 0, snapHighlight.width, snapHighlight.height), new Vector2(0.5f, 0.5f));
				_prevButtonActive = Sprite.Create(prevActive, new Rect(0, 0, snapActive.width, snapActive.height), new Vector2(0.5f, 0.5f));
				_prevButtonInactive = Sprite.Create(prevInactive, new Rect(0, 0, snapActive.width, snapActive.height), new Vector2(0.5f, 0.5f));
			}

            if (snapOnIcon != null && snapOffIcon != null)
            {
                _SnapPanelTabIconOn = Sprite.Create(snapOnIcon, new Rect(0, 0, snapOnIcon.width, snapOnIcon.height), new Vector2(0.5f, 0.5f));
                _SnapPanelTabIconOff = Sprite.Create(snapOffIcon, new Rect(0, 0, snapOffIcon.width, snapOffIcon.height), new Vector2(0.5f, 0.5f));
            }

            if (_inputButtonNormal != null && _inputButtonHighlight != null && _inputButtonActive != null && _snapButtonNormal != null && _snapButtonHighlight != null && _snapButtonActive != null)
				TexturesLoaded = true;
		}

		private void processTMPPrefabs()
		{
			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

                if (o.name == "Input_Panel")
                    _inputPrefab = o;
                else if (o.name == "Warp_Panel")
                    _snapPrefab = o;
                else if (o.name == "ManeuverSnapPanel")
                {
                    _snapTabPrefab = o;
                    _snapTabPrefab.AddComponent<ManeuverSnapTab>();
                }

				if (o != null)
				{
					processTMP(o);
					processInputFields(o);
				}
			}

			TMPLoaded = true;
		}

		private void processTMP(GameObject obj)
		{
			TextHandler[] handlers = obj.GetComponentsInChildren<TextHandler>(true);

			if (handlers == null)
				return;

			for (int i = 0; i < handlers.Length; i++)
				TMProFromText(handlers[i]);
		}

		private void TMProFromText(TextHandler handler)
		{
			if (handler == null)
				return;

			Text text = handler.GetComponent<Text>();

			if (text == null)
				return;

			string t = text.text;
			Color c = text.color;
			int i = text.fontSize;
			bool r = text.raycastTarget;
			FontStyles sty = TMPProUtil.FontStyle(text.fontStyle);
			TextAlignmentOptions align = TMPProUtil.TextAlignment(text.alignment);
			float spacing = text.lineSpacing;
			GameObject obj = text.gameObject;

			MonoBehaviour.DestroyImmediate(text);

			ManeuverOrbitTextMeshProHolder tmp = obj.AddComponent<ManeuverOrbitTextMeshProHolder>();

			tmp.text = t;
			tmp.color = c;
			tmp.fontSize = i;
			tmp.raycastTarget = r;
			tmp.alignment = align;
			tmp.fontStyle = sty;
			tmp.lineSpacing = spacing;

			tmp.font = UISkinManager.TMPFont;
			tmp.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;

			tmp.enableWordWrapping = true;
			tmp.isOverlay = false;
			tmp.richText = true;
		}

		private static void processInputFields(GameObject obj)
		{
			InputHandler[] handlers = obj.GetComponentsInChildren<InputHandler>(true);

			if (handlers == null)
				return;

			for (int i = 0; i < handlers.Length; i++)
				TMPInputFromInput(handlers[i]);
		}

		private static void TMPInputFromInput(InputHandler handler)
		{
			if (handler == null)
				return;

			InputField input = handler.GetComponent<InputField>();

			if (input == null)
				return;

			int limit = input.characterLimit;
			TMP_InputField.ContentType content = GetTMPContentType(input.contentType);
			float caretBlinkRate = input.caretBlinkRate;
			int caretWidth = input.caretWidth;
			Color selectionColor = input.selectionColor;
			GameObject obj = input.gameObject;

			RectTransform viewport = handler.GetComponentInChildren<RectMask2D>().rectTransform;
			ManeuverOrbitTextMeshProHolder textComponent = handler.GetComponentsInChildren<ManeuverOrbitTextMeshProHolder>()[0];

			if (viewport == null || textComponent == null)
				return;

			MonoBehaviour.DestroyImmediate(input);

			ManeuverTMPInputField tmp = obj.AddComponent<ManeuverTMPInputField>();

			tmp.textViewport = viewport;
			tmp.placeholder = null;
			tmp.textComponent = textComponent;

			tmp.characterLimit = limit;
			tmp.contentType = content;
			tmp.caretBlinkRate = caretBlinkRate;
			tmp.caretWidth = caretWidth;
			tmp.selectionColor = selectionColor;

			tmp.readOnly = false;
			tmp.shouldHideMobileInput = false;

			tmp.fontAsset = UISkinManager.TMPFont;
		}

		private static TMP_InputField.ContentType GetTMPContentType(InputField.ContentType type)
		{
			switch (type)
			{
				case InputField.ContentType.Alphanumeric:
					return TMP_InputField.ContentType.Alphanumeric;
				case InputField.ContentType.Autocorrected:
					return TMP_InputField.ContentType.Autocorrected;
				case InputField.ContentType.Custom:
					return TMP_InputField.ContentType.Custom;
				case InputField.ContentType.DecimalNumber:
					return TMP_InputField.ContentType.DecimalNumber;
				case InputField.ContentType.EmailAddress:
					return TMP_InputField.ContentType.EmailAddress;
				case InputField.ContentType.IntegerNumber:
					return TMP_InputField.ContentType.IntegerNumber;
				case InputField.ContentType.Name:
					return TMP_InputField.ContentType.Name;
				case InputField.ContentType.Password:
					return TMP_InputField.ContentType.Password;
				case InputField.ContentType.Pin:
					return TMP_InputField.ContentType.Pin;
				case InputField.ContentType.Standard:
					return TMP_InputField.ContentType.Standard;
				default:
					return TMP_InputField.ContentType.Standard;
			}
		}
		
		private void processUIPrefabs()
		{
			parseUIWindow();

            parseManeuverTab();

			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				if (o != null)
					processUIComponents(o);
			}

			UILoaded = true;
		}

		private void parseUIWindow()
		{
			UIPartActionWindow windowPrefab = UIPartActionController.Instance.windowPrefab;
			UIPartActionButton buttonPrefab = UIPartActionController.Instance.eventItemPrefab;

			if (windowPrefab == null || buttonPrefab == null)
				return;

			_lineColor = windowPrefab.lineColor;
			_lineCornerRadius = windowPrefab.lineCornerRadius;
			_lineMaterial = windowPrefab.lineMaterial;
			_lineWidth = windowPrefab.lineWidth;

			Image Title = windowPrefab.titleBar.gameObject.GetComponent<Image>();
			_TitleBackground = Title.sprite;
			_TitleColor = Title.color;

			UIMaterial = Title.material;

			Image Window = windowPrefab.GetComponentInChildren<Image>(true);
			_WindowBackground = Window.sprite;
			_WindowColor = Window.color;

			Toggle pin = windowPrefab.togglePinned;

			Selectable pinSelect = pin.GetComponent<Selectable>();
			_ToggleNormal = pinSelect.image.sprite;
			_ToggleHightlight = pinSelect.spriteState.highlightedSprite;
			_ToggleActive = pinSelect.spriteState.pressedSprite;
			_ToggleInactive = pinSelect.spriteState.disabledSprite;

			Image checkmark = pin.GetComponentsInChildren<Image>(true)[1];

			_ToggleCheckmark = checkmark.sprite;

			Selectable button = buttonPrefab.button.GetComponent<Selectable>();

			_ButtonNormal = button.image.sprite;
			_ButtonHighlight = button.spriteState.highlightedSprite;
			_ButtonActive = button.spriteState.pressedSprite;
			_ButtonInactive = button.spriteState.disabledSprite;
		}

        private void parseManeuverTab()
        {
            //ManeuverController.maneuverLog("1", logLevels.log);
            for (int i = ManeuverNodeEditorManager.Instance.maneuverNodeEditorTabs.Count - 1; i >= 0; i--)
            {
                //ManeuverController.maneuverLog("tab: {0}", logLevels.log, i.ToString());
                ManeuverNodeEditorTab tab = ManeuverNodeEditorManager.Instance.maneuverNodeEditorTabs[i];

                if (tab is ManeuverNodeEditorTabVectorInput)
                {
                    ManeuverNodeEditorTabVectorInput vectorTab = tab as ManeuverNodeEditorTabVectorInput;
                    //ManeuverController.maneuverLog("2", logLevels.log);
                    _TabPanelBackground = null;// vectorTab.GetComponentInParent<Image>().sprite;
                    //ManeuverController.maneuverLog("3", logLevels.log);
                    _TabButtonBackground = vectorTab.proRetrogradeField.transform.parent.GetComponentInChildren<Image>().sprite;
                    //ManeuverController.maneuverLog("4", logLevels.log);
                    _TabTextBackground = vectorTab.proRetrogradeField.GetComponent<Image>().sprite;
                }
                else if (tab is ManeuverNodeEditorTabVectorHandles)
                {
                    ManeuverNodeEditorTabVectorHandles handleTab = tab as ManeuverNodeEditorTabVectorHandles;
                    //ManeuverController.maneuverLog("Handle Tab: {0}", logLevels.log, handleTab.name);
                    Selectable orbitButton = handleTab.GetComponentInChildren<Selectable>();
                    //ManeuverController.maneuverLog("Orbit Button: {0}", logLevels.log, orbitButton.name);
                    _TabOrbitButtonNormal = orbitButton.image.sprite;
                    _TabOrbitButtonHighlight = orbitButton.spriteState.highlightedSprite;
                    _TabOrbitButtonActive = orbitButton.spriteState.pressedSprite;
                    _TabOrbitButtonInactive = orbitButton.spriteState.disabledSprite;
                }
            }
        }

		private void processUIComponents(GameObject obj)
		{
			ManeuverStyle[] styles = obj.GetComponentsInChildren<ManeuverStyle>(true);

			if (styles == null)
				return;

			for (int i = 0; i < styles.Length; i++)
				processComponents(styles[i]);
		}

		private void processComponents(ManeuverStyle style)
		{
			if (style == null)
				return;

			switch (style.ElementType)
			{
				case ManeuverStyle.ElementTypes.Window:
					style.setImage(_WindowBackground, UIMaterial, _WindowColor);
					break;
				case ManeuverStyle.ElementTypes.Box:
					style.setImage(_TitleBackground, UIMaterial, _TitleColor);
					break;
				case ManeuverStyle.ElementTypes.Button:
					style.setButton(_ButtonNormal, _ButtonHighlight, _ButtonActive, _ButtonInactive);
					break;
				case ManeuverStyle.ElementTypes.Toggle:
					style.setToggle(_ToggleNormal, _ToggleCheckmark);
					break;
                case ManeuverStyle.ElementTypes.TabBackground:
                    style.setImage(_TabPanelBackground);
                    break;
                case ManeuverStyle.ElementTypes.TabButtonBackground:
                    style.setImage(_TabButtonBackground);
                    break;
                case ManeuverStyle.ElementTypes.TabTextBackground:
                    style.setImage(_TabTextBackground);
                    break;
                case ManeuverStyle.ElementTypes.ResetButton:
                    style.setButton(_TabOrbitButtonNormal, _TabOrbitButtonHighlight, _TabOrbitButtonActive, _TabOrbitButtonInactive, Image.Type.Simple);
                    break;
                case ManeuverStyle.ElementTypes.ScrollBar:
                    UISkinDef skin = UISkinManager.defaultSkin;

                    style.setScrollbar(skin.verticalScrollbar.normal.background, skin.verticalScrollbarThumb.normal.background);
                    break;
				default:
					break;
			}
		}
	}
}
