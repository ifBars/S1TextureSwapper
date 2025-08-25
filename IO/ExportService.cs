using System;
using System.IO;
using System.Text;
using UnityEngine;
using TextureSwapper.Runtime;

namespace TextureSwapper.IO
{
	public static class ExportService
	{
		public static void ExportRenderer(GameObject target)
		{
			if (target == null) return;
			var renderer = target.GetComponentInChildren<Renderer>();
			if (renderer == null) return;
			var materials = renderer.sharedMaterials;
			if (materials == null || materials.Length == 0) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"Target: {target.name}");
			for (int i = 0; i < materials.Length; i++)
			{
				var mat = materials[i];
				if (mat == null) continue;
				sb.AppendLine($"Material[{i}]: {mat.name}");
				for (int p = 0; p < PropertyNames.TexturePropertyNames.Length; p++)
				{
					string prop = PropertyNames.TexturePropertyNames[p];
					if (!mat.HasProperty(prop)) continue;
					var tex = mat.GetTexture(prop) as Texture2D;
					if (tex == null) continue;
					sb.AppendLine($"  {prop} → {tex.name}");
				}
			}

			try
			{
				var file = $"{Sanitize(target.name)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
				var path = Path.Combine(Core.ExportsFolderPath, file);
				File.WriteAllText(path, sb.ToString());
				MelonLoader.MelonLogger.Msg($"Exported: {path}");
				if (Config.Preferences.DebugEnabled)
					MelonLoader.MelonLogger.Msg($"[ExportService] Exported target='{target.name}' materials={materials.Length}");
			}
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"Export failed: {ex.Message}");
			}
		}

		private static string Sanitize(string s)
		{
			if (string.IsNullOrEmpty(s)) return "Object";
			foreach (var c in Path.GetInvalidFileNameChars())
			{
				s = s.Replace(c, '_');
			}
			return s;
		}
	}
}


