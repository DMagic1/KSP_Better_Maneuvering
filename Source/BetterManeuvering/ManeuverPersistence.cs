#region license
/*The MIT License (MIT)

ManeuverPersistence - Script for saving and loading settings file from disk

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
		public int selectionTolerance = 10;
		[Persistent]
		public bool alignToOrbit = true;
		[Persistent]
		public int accuracy = 2;
		[Persistent]
		public bool replaceGizmoButtons = true;
		[Persistent]
		public bool rightClickClose = false;
		[Persistent]
		public bool rememberManualInput = true;
		[Persistent]
		public bool showManeuverCycle = true;
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
				selectionTolerance = settings.selectionTolerance;
				alignToOrbit = settings.alignToOrbit;
				accuracy = settings.accuracy;
				replaceGizmoButtons = settings.replaceGizmoButtons;
				rightClickClose = settings.rightClickClose;
				rememberManualInput = settings.rememberManualInput;
				showManeuverCycle = settings.showManeuverCycle;
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
