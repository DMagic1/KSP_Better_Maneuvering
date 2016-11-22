using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BetterManeuvering.Unity;

namespace BetterManeuvering
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ManeuverLoader : MonoBehaviour
	{
		private static bool loaded;
		private static bool TMPLoaded;
		private static bool UILoaded;
		private static bool TexturesLoaded;

		private static GameObject[] loadedPrefabs;

		private static GameObject _inputPrefab;
		private static GameObject _snapPrefab;

		private static Sprite _inputButtonNormal;
		private static Sprite _inputButtonHighlight;
		private static Sprite _inputButtonActive;

		private static Sprite _snapButtonNormal;
		private static Sprite _snapButtonHighlight;
		private static Sprite _snapButtonActive;

		private static Sprite ButtonNormal;
		private static Sprite ButtonHighlight;
		private static Sprite ButtonActive;
		private static Sprite ButtonInactive;

		private static Sprite ToggleNormal;
		private static Sprite ToggleHightlight;
		private static Sprite ToggleActive;
		private static Sprite ToggleInactive;
		
		private static Sprite ToggleImage;
		private static Color ToggleImageColor;

		private static Sprite WindowBackground;
		private static Color WindowColor;

		private static Sprite TitleBackground;
		private static Color TitleColor;

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

		private void Start()
		{
			if (loaded)
				Destroy(gameObject);

			if (loadedPrefabs == null)
			{
				string path = KSPUtil.ApplicationRootPath + "GameData/BetterManeuver/Resources";

				AssetBundle prefabs = AssetBundle.LoadFromFile(path + "/better_maneuver_prefabs.ksp");

				if (prefabs != null)
					loadedPrefabs = prefabs.LoadAllAssets<GameObject>();
			}

			if (loadedPrefabs != null)
			{
				if (!TMPLoaded)
					processTMPPrefabs();

				if (UIPartActionController.Instance != null && !UILoaded)
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
			Texture2D inputNormal = GameDatabase.Instance.GetTexture("BetterManeuver/Resources/Input_Normal", false);
			Texture2D inputHighlight = GameDatabase.Instance.GetTexture("BetterManeuver/Resources/Input_Highlight", false);
			Texture2D inputActive = GameDatabase.Instance.GetTexture("BetterManeuver/Resources/Input_Active", false);

			Texture2D snapNormal = GameDatabase.Instance.GetTexture("BetterManeuver/Resources/Snap_Normal", false);
			Texture2D snapHighlight = GameDatabase.Instance.GetTexture("BetterManeuver/Resources/Snap_Highlight", false);
			Texture2D snapActive = GameDatabase.Instance.GetTexture("BetterManeuver/Resources/Snap_Active", false);

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

				if (o != null)
					processTMP(o);
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
			FontStyles sty = getStyle(text.fontStyle);
			TextAlignmentOptions align = getAnchor(text.alignment);
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

			//Load the TMP Font from disk
			tmp.font = Resources.Load("Fonts/Calibri SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
			tmp.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;

			tmp.enableWordWrapping = true;
			tmp.isOverlay = false;
			tmp.richText = true;
		}

		private FontStyles getStyle(FontStyle style)
		{
			switch (style)
			{
				case FontStyle.Normal:
					return FontStyles.Normal;
				case FontStyle.Bold:
					return FontStyles.Bold;
				case FontStyle.Italic:
					return FontStyles.Italic;
				case FontStyle.BoldAndItalic:
					return FontStyles.Bold;
				default:
					return FontStyles.Normal;
			}
		}

		private TextAlignmentOptions getAnchor(TextAnchor anchor)
		{
			switch (anchor)
			{
				case TextAnchor.UpperLeft:
					return TextAlignmentOptions.TopLeft;
				case TextAnchor.UpperCenter:
					return TextAlignmentOptions.Top;
				case TextAnchor.UpperRight:
					return TextAlignmentOptions.TopRight;
				case TextAnchor.MiddleLeft:
					return TextAlignmentOptions.MidlineLeft;
				case TextAnchor.MiddleCenter:
					return TextAlignmentOptions.Midline;
				case TextAnchor.MiddleRight:
					return TextAlignmentOptions.MidlineRight;
				case TextAnchor.LowerLeft:
					return TextAlignmentOptions.BottomLeft;
				case TextAnchor.LowerCenter:
					return TextAlignmentOptions.Bottom;
				case TextAnchor.LowerRight:
					return TextAlignmentOptions.BottomRight;
				default:
					return TextAlignmentOptions.Center;
			}
		}

		private void processUIPrefabs()
		{
			parseUIWindow();

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
			TitleBackground = Title.sprite;
			TitleColor = Title.color;

			//ManeuverController.maneuverLog("Title Rect Anchored: {0:F4}\nPosition: {1:F4}\nPivot: {2:F4}\nAnchor Min: {3:F4}\nAnchor Max: {4:F4}\nScale: {5:F4}\nRotation: {5:F4}\nLocal Rotation: {6:F4}"
			//	, logLevels.log
			//	, windowPrefab.titleBar.anchoredPosition3D
			//	, windowPrefab.titleBar.position
			//	, windowPrefab.titleBar.pivot
			//	, windowPrefab.titleBar.anchorMin
			//	, windowPrefab.titleBar.anchorMax
			//	, windowPrefab.titleBar.localScale
			//	, windowPrefab.titleBar.rotation.eulerAngles
			//	, windowPrefab.titleBar.localRotation.eulerAngles);

			UIMaterial = Title.material;

			Image Window = windowPrefab.GetComponentInChildren<Image>(true);
			WindowBackground = Window.sprite;
			WindowColor = Window.color;

			Toggle pin = windowPrefab.togglePinned;

			Selectable pinSelect = pin.GetComponent<Selectable>();
			ToggleNormal = pinSelect.image.sprite;
			ToggleHightlight = pinSelect.spriteState.highlightedSprite;
			ToggleActive = pinSelect.spriteState.pressedSprite;
			ToggleInactive = pinSelect.spriteState.disabledSprite;

			Image circle = pin.GetComponentsInChildren<Image>(true)[2];

			ToggleImage = circle.sprite;
			ToggleImageColor = circle.color;
			
			Selectable button = buttonPrefab.button.GetComponent<Selectable>();

			ButtonNormal = button.image.sprite;
			ButtonHighlight = button.spriteState.highlightedSprite;
			ButtonActive = button.spriteState.pressedSprite;
			ButtonInactive = button.spriteState.disabledSprite;
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
					style.setImage(WindowBackground, UIMaterial, WindowColor);
					break;
				case ManeuverStyle.ElementTypes.Box:
					style.setImage(TitleBackground, UIMaterial, TitleColor);
					break;
				case ManeuverStyle.ElementTypes.Button:
					style.setButton(ButtonNormal, ButtonHighlight, ButtonActive, ButtonInactive);
					break;
				case ManeuverStyle.ElementTypes.Toggle:
					style.setToggle(ToggleNormal, ToggleHightlight, ToggleActive, ToggleInactive, ToggleImage, ToggleImageColor);
					break;
				default:
					break;
			}
		}
	}
}
