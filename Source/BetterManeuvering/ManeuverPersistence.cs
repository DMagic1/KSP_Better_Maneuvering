using System;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace BetterManeuvering
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class ManeuverPersistence : MonoBehaviour
	{
		private static ManeuverPersistence instance;

		[Persistent]
		public bool dynamicScaling = true;
		[Persistent]
		public float baseScale = 1.25f;
		[Persistent]
		public float maxScale = 2.5f;
		[Persistent]
		public bool alignToOrbit = true;
		[Persistent]
		public int accuracy = 2;
		[Persistent]
		public bool replaceGizmoButtons = true;
		[Persistent]
		public bool rememberManualInput = true;
		[Persistent]
		public bool useKeyboard = true;
		[Persistent]
		public KeyCode keyboardShortcut = KeyCode.N;

		private const string fileName = "PluginData/Settings.cfg";
		private string fullPath;
		private ManeuverGameParams settings;

		public static ManeuverPersistence Instance
		{
			get { return instance; }
		}

		private void Awake()
		{
			if (instance != null)
				Destroy(gameObject);

			DontDestroyOnLoad(gameObject);

			instance = this;

			fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName).Replace("\\", "/");
			GameEvents.OnGameSettingsApplied.Add(SettingsApplied);

			if (Load())
				ManeuverController.maneuverLog("Settings file loaded", logLevels.log);
		}

		private void OnDestroy()
		{
			GameEvents.OnGameSettingsApplied.Remove(SettingsApplied);
		}

		public void SettingsApplied()
		{
			if (HighLogic.CurrentGame != null)
				settings = HighLogic.CurrentGame.Parameters.CustomParams<ManeuverGameParams>();

			if (settings == null)
				return;

			if (settings.useAsDefault)
			{
				dynamicScaling = settings.dynamicScaling;
				baseScale = settings.baseScale;
				maxScale = settings.maxScale;
				alignToOrbit = settings.alignToOrbit;
				accuracy = settings.accuracy;
				replaceGizmoButtons = settings.replaceGizmoButtons;
				rememberManualInput = settings.rememberManualInput;
				useKeyboard = settings.useKeyboard;

				if (Save())
					ManeuverController.maneuverLog("Settings file saved", logLevels.log);
			}
		}

		public bool Load()
		{
			bool b = false;

			try
			{
				if (File.Exists(fullPath))
				{
					ConfigNode node = ConfigNode.Load(fullPath);
					ConfigNode unwrapped = node.GetNode(GetType().Name);
					ConfigNode.LoadObjectFromConfig(this, unwrapped);
					b = true;
				}
				else
				{
					ManeuverController.maneuverLog("Settings file could not be found [{0}]", logLevels.log, fullPath);
					b = false;
				}
			}
			catch (Exception e)
			{
				ManeuverController.maneuverLog("Error while loading settings file from [{0}]\n{1}", logLevels.log, fullPath, e);
				b = false;
			}

			return b;
		}

		public bool Save()
		{
			bool b = false;

			try
			{
				ConfigNode node = AsConfigNode();
				ConfigNode wrapper = new ConfigNode(GetType().Name);
				wrapper.AddNode(node);
				wrapper.Save(fullPath);
				b = true;
			}
			catch (Exception e)
			{
				ManeuverController.maneuverLog("Error while saving settings file at [{0}]\n{1}", logLevels.log, fullPath, e);
				b = false;
			}

			return b;
		}

		private ConfigNode AsConfigNode()
		{
			try
			{
				ConfigNode node = new ConfigNode(GetType().Name);

				node = ConfigNode.CreateConfigFromObject(this, node);
				return node;
			}
			catch (Exception e)
			{
				ManeuverController.maneuverLog("Failed to generate settings file node...\n{0}", logLevels.log, e);
				return new ConfigNode(GetType().Name);
			}
		}
	}
}
