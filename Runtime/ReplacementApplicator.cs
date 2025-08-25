using UnityEngine;

namespace TextureSwapper.Runtime
{
	public static class ReplacementApplicator
	{
		public static bool ApplyAllTextureProperties(Material material, ReplacementIndex index)
		{
			bool anyApplied = false;
			if (material == null) return false;

			var props = PropertyNames.TexturePropertyNames;
			for (int i = 0; i < props.Length; i++)
			{
				string prop = props[i];
				if (!material.HasProperty(prop)) continue;
				var tex = material.GetTexture(prop) as Texture2D;
				if (tex == null) continue;

				if (index.TryGetByTextureName(tex.name, out var replacement))
				{
					Vector2 scale = material.GetTextureScale(prop);
					Vector2 offset = material.GetTextureOffset(prop);
					material.SetTexture(prop, replacement);
					material.SetTextureScale(prop, scale);
					material.SetTextureOffset(prop, offset);

					// Emission keyword support
					for (int e = 0; e < PropertyNames.EmissionPropertyNames.Length; e++)
					{
						if (prop == PropertyNames.EmissionPropertyNames[e])
						{
							material.EnableKeyword("_EMISSION");
							break;
						}
					}

					anyApplied = true;
					if (Config.Preferences.DebugEnabled)
						MelonLoader.MelonLogger.Msg($"[ApplyAllTextureProperties] {material.name} set {prop} via texture-name mapping");
				}
			}

			return anyApplied;
		}

		public static bool ApplyByMaterialMapping(Renderer renderer, Material material, ReplacementIndex index)
		{
			if (renderer == null || material == null || index == null) return false;
			bool anyApplied = false;

			// Try material+property specific first
			for (int p = 0; p < PropertyNames.TexturePropertyNames.Length; p++)
			{
				string prop = PropertyNames.TexturePropertyNames[p];
				if (!material.HasProperty(prop)) continue;
				if (index.TryGetByMaterialProperty(material.name, prop, out var texByProp) && texByProp != null)
				{
					ApplyTexturePreserveTiling(material, prop, texByProp);
					if (Config.Preferences.DebugEnabled)
						MelonLoader.MelonLogger.Msg($"[ApplyByMaterialMapping] {renderer.gameObject.name}.{material.name} set {prop} via material__property mapping");
					anyApplied = true;
				}
			}

			// Then material-name primary texture
			if (index.TryGetByMaterialName(material.name, out var texByMat) && texByMat != null)
			{
				string primary = FindPrimaryTextureProperty(material);
				if (primary != null)
				{
					ApplyTexturePreserveTiling(material, primary, texByMat);
					if (Config.Preferences.DebugEnabled)
						MelonLoader.MelonLogger.Msg($"[ApplyByMaterialMapping] {renderer.gameObject.name}.{material.name} set {primary} via material mapping");
					anyApplied = true;
				}
			}

			return anyApplied;
		}

		private static void ApplyTexturePreserveTiling(Material material, string prop, Texture2D replacement)
		{
			Vector2 scale = material.GetTextureScale(prop);
			Vector2 offset = material.GetTextureOffset(prop);
			int id = Shader.PropertyToID(prop);
			material.SetTexture(id, replacement);
			material.SetTextureScale(id, scale);
			material.SetTextureOffset(id, offset);

			for (int e = 0; e < PropertyNames.EmissionPropertyNames.Length; e++)
			{
				if (prop == PropertyNames.EmissionPropertyNames[e])
				{
					material.EnableKeyword("_EMISSION");
					break;
				}
			}
		}

		private static string FindPrimaryTextureProperty(Material material)
		{
			// Prefer URP base map
			if (material.HasProperty("_BaseMap")) return "_BaseMap";
			if (material.HasProperty("_MainTex")) return "_MainTex";
			// Fallback: first available texture property
			for (int i = 0; i < PropertyNames.TexturePropertyNames.Length; i++)
			{
				string prop = PropertyNames.TexturePropertyNames[i];
				if (material.HasProperty(prop)) return prop;
			}
			return null;
		}
	}
}


