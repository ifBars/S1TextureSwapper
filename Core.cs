using MelonLoader;
using UnityEngine;
using System;
using System.IO;
using MelonLoader.Utils;
using TextureSwapper.Config;
using TextureSwapper.Runtime;
using TextureSwapper.IO;
using TextureSwapper.UI;

[assembly: MelonInfo(typeof(TextureSwapper.Core), "TextureSwapper", "0.1.0", "Bars")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace TextureSwapper
{
	public class Core : MelonMod
	{
		public static Core Instance { get; private set; }
		public static string ModFolderPath { get; private set; }
		public static string ExportsFolderPath { get; private set; }

		internal ReplacementIndex ReplacementIndex { get; private set; }
		internal MaterialScanner MaterialScanner { get; private set; }
		internal InspectorUI InspectorUI { get; private set; }
		internal FileWatcher FileWatcher { get; private set; }


		private GameObject managerGameObject;
		private bool isInitialized;

		public override void OnInitializeMelon()
		{
			Instance = this;
			Preferences.Initialize();

			try
			{
				ModFolderPath = Path.Combine(MelonEnvironment.ModsDirectory, "TextureSwapper");
				ExportsFolderPath = Path.Combine(ModFolderPath, "_Exports");
				Directory.CreateDirectory(ModFolderPath);
				Directory.CreateDirectory(ExportsFolderPath);

				LoggerInstance.Msg($"TextureSwapper ready. Waiting for Menu scene to initialize.");
				if (Preferences.DebugEnabled)
				{
					LoggerInstance.Msg($"[Debug] ModsDir={MelonEnvironment.ModsDirectory}");
					LoggerInstance.Msg($"[Debug] ModFolderPath={ModFolderPath}");
					LoggerInstance.Msg($"[Debug] ExportsFolderPath={ExportsFolderPath}");
				}
			}
			catch (Exception ex)
			{
				LoggerInstance.Error($"Basic setup error: {ex.Message}");
			}
		}

		public override void OnSceneWasInitialized(int buildIndex, string sceneName)
		{
			if (!Preferences.Enabled.Value) return;
			if (Preferences.DebugEnabled)
				LoggerInstance.Msg($"[Debug] SceneInitialized: {sceneName} ({buildIndex})");

			// Initialize on first Menu scene load
			if (!isInitialized && sceneName == "Menu")
			{
				try
				{
					managerGameObject = new GameObject("TextureSwapper_Manager");
					UnityEngine.Object.DontDestroyOnLoad(managerGameObject);

					ReplacementIndex = new ReplacementIndex(ModFolderPath);
					MaterialScanner = managerGameObject.AddComponent<MaterialScanner>();
					InspectorUI = managerGameObject.AddComponent<InspectorUI>();

					if (MaterialScanner != null) MaterialScanner.Initialize(ReplacementIndex);
					if (InspectorUI != null) InspectorUI.Initialize(ReplacementIndex, MaterialScanner);

					FileWatcher = new FileWatcher(ModFolderPath);
					FileWatcher.Start();

					LoggerInstance.Msg($"Initialized. Mod folder: {ModFolderPath}");
					isInitialized = true;
					// Single delayed scan for initialization - no redundant warmup
					MelonCoroutines.Start(DelayedSceneScan(1.0f, "Initial"));
				}
				catch (Exception ex)
				{
					LoggerInstance.Error($"Initialization error: {ex.Message}");
					LoggerInstance.Error(ex.StackTrace);
				}
			}
			else if (MaterialScanner != null)
			{
				// Regular scene change - single delayed rescan
				MelonCoroutines.Start(DelayedSceneScan(0.5f, "Scene change"));
			}
		}

		private System.Collections.IEnumerator DelayedSceneScan(float delaySeconds, string scanType)
		{
			if (Preferences.DebugEnabled)
				LoggerInstance.Msg($"Waiting {delaySeconds}s for {scanType} scan...");
			
			// Wait for scene to fully load and cameras to be ready
			yield return new WaitForSeconds(delaySeconds);
			
			if (MaterialScanner != null)
			{
				if (Preferences.DebugEnabled)
					LoggerInstance.Msg($"Executing {scanType} material scan");
				MaterialScanner.RequestFullRescan();
			}
		}

		public override void OnUpdate()
		{
			if (!Preferences.Enabled.Value || !isInitialized) return;

			// Handle file changes (debounced)
			var changedPaths = FileWatcher?.DrainChangedPaths();
			if (changedPaths != null && changedPaths.Count > 0 && ReplacementIndex != null)
			{
				if (Preferences.DebugEnabled)
					LoggerInstance.Msg($"[Debug] File changes detected: {changedPaths.Count}");
				ReplacementIndex.ReloadChangedFiles(changedPaths);
				if (MaterialScanner != null) MaterialScanner.RequestFullRescan();
			}

			// Hotkeys
			if (InspectorUI != null && Input.GetKeyDown(Preferences.InspectorToggleKey))
			{
				InspectorUI.ToggleVisibility();
				if (Preferences.DebugEnabled) LoggerInstance.Msg("[Debug] Inspector toggle pressed");
			}
			if (InspectorUI != null && Input.GetKeyDown(Preferences.ExportKey))
			{
				InspectorUI.ExportCurrentSelection();
				if (Preferences.DebugEnabled) LoggerInstance.Msg("[Debug] Export hotkey pressed");
			}
		}
	}
}


