using MelonLoader;
using UnityEngine;
using System;

namespace TextureSwapper.Config
{
	public static class Preferences
	{
		public static MelonPreferences_Category Category { get; private set; }
		public static MelonPreferences_Entry<bool> Enabled { get; private set; }
		public static MelonPreferences_Entry<bool> LiveReload { get; private set; }
		public static MelonPreferences_Entry<string> InspectorKeyPref { get; private set; }
		public static MelonPreferences_Entry<string> ExportKeyPref { get; private set; }
		public static MelonPreferences_Entry<int> ScanBatchSize { get; private set; }
		public static MelonPreferences_Entry<bool> DebugLogging { get; private set; }
		public static MelonPreferences_Entry<bool> OnlyScanActive { get; private set; }
		public static MelonPreferences_Entry<bool> OnlyScanVisible { get; private set; }
		public static MelonPreferences_Entry<float> MaxScanDistance { get; private set; }
		public static MelonPreferences_Entry<float> RescanIntervalSeconds { get; private set; }

		public static void Initialize()
		{
			Category = MelonPreferences.CreateCategory("TextureSwapper");
			Enabled = Category.CreateEntry("Enabled", true, "Enable the TextureSwapper mod");
			LiveReload = Category.CreateEntry("LiveReload", true, "Auto-reload textures when files change");
			InspectorKeyPref = Category.CreateEntry("InspectorKey", "F8", "Toggle inspector overlay key");
			ExportKeyPref = Category.CreateEntry("ExportKey", "F9", "Export current target info key");
			ScanBatchSize = Category.CreateEntry("ScanBatchSize", 96, "Renderer scan batch size per frame");
			DebugLogging = Category.CreateEntry("DebugLogging", false, "Enable verbose debug logging for troubleshooting");
			OnlyScanActive = Category.CreateEntry("OnlyScanActive", true, "Scan only active renderers (exclude inactive)");
			OnlyScanVisible = Category.CreateEntry("OnlyScanVisible", true, "Scan only renderers currently visible to a camera");
			MaxScanDistance = Category.CreateEntry("MaxScanDistance", 500f, "Max distance from camera for scanning (0 = unlimited)");
			RescanIntervalSeconds = Category.CreateEntry("RescanIntervalSeconds", 0f, "Periodic rescan interval in seconds (0 = scan only on scene changes and file updates)");
		}

		public static KeyCode InspectorToggleKey => ParseKeyCode(InspectorKeyPref.Value, KeyCode.F7);
		public static KeyCode ExportKey => ParseKeyCode(ExportKeyPref.Value, KeyCode.F8);
		public static bool DebugEnabled => DebugLogging != null && DebugLogging.Value;

		private static KeyCode ParseKeyCode(string value, KeyCode fallback)
		{
			if (string.IsNullOrWhiteSpace(value)) return fallback;
			if (Enum.TryParse<KeyCode>(value.Trim(), true, out var key)) return key;
			return fallback;
		}
	}
}


