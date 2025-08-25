using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TextureSwapper.Runtime
{
    public sealed class ReplacementIndex
	{
		private readonly string baseDirectory;
		private readonly string exportsDirectory;
		private readonly Dictionary<string, LoadedTexture> keyToTexture;
		private readonly Dictionary<string, LoadedTexture> materialNameToTexture; // material name → texture for _BaseMap/_MainTex
		private readonly Dictionary<(string materialName, string property), LoadedTexture> materialPropertyToTexture; // (material, property) → texture
		private static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".png", ".jpg", ".jpeg"
		};

		private sealed class LoadedTexture
		{
			public string FilePath;
			public Texture2D Texture;
		}

		private static string NormalizeMaterialName(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			string normalized = name.Trim();
			// Strip common Unity suffixes that prevent exact matches
			if (normalized.EndsWith(" (Instance)", StringComparison.OrdinalIgnoreCase))
				normalized = normalized.Substring(0, normalized.Length - " (Instance)".Length);
			if (normalized.EndsWith(" (Clone)", StringComparison.OrdinalIgnoreCase))
				normalized = normalized.Substring(0, normalized.Length - " (Clone)".Length);
			return normalized.Trim();
		}

		public ReplacementIndex(string baseDirectory)
		{
			this.baseDirectory = baseDirectory;
			exportsDirectory = Path.Combine(baseDirectory, "_Exports");
			Directory.CreateDirectory(this.baseDirectory);
			Directory.CreateDirectory(exportsDirectory);
			keyToTexture = new Dictionary<string, LoadedTexture>(StringComparer.OrdinalIgnoreCase);
			materialNameToTexture = new Dictionary<string, LoadedTexture>(StringComparer.OrdinalIgnoreCase);
			materialPropertyToTexture = new Dictionary<(string materialName, string property), LoadedTexture>();
			LoadAll();
		}

		public bool HasAnyEntries
		{
			get
			{
				return (keyToTexture != null && keyToTexture.Count > 0)
					|| (materialNameToTexture != null && materialNameToTexture.Count > 0)
					|| (materialPropertyToTexture != null && materialPropertyToTexture.Count > 0);
			}
		}

		private static bool IsTextureValid(Texture2D texture)
		{
			return texture != null && !ReferenceEquals(texture, null);
		}

		private bool ValidateOrReloadTexture(LoadedTexture loadedTexture)
		{
			if (loadedTexture == null) return false;
			
			if (IsTextureValid(loadedTexture.Texture))
				return true;

			// Texture was destroyed, try to reload it
			if (string.IsNullOrEmpty(loadedTexture.FilePath) || !File.Exists(loadedTexture.FilePath))
				return false;

			try
			{
				var newTexture = LoadTextureFromFile(loadedTexture.FilePath);
				if (newTexture != null)
				{
					loadedTexture.Texture = newTexture;
					if (Config.Preferences.DebugEnabled)
						MelonLogger.Msg($"[ReplacementIndex] Reloaded destroyed texture from '{loadedTexture.FilePath}'");
					return true;
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[ReplacementIndex] Failed to reload texture from '{loadedTexture.FilePath}': {ex.Message}");
			}

			return false;
		}

		public bool TryGetByTextureName(string textureName, out Texture2D texture)
		{
			texture = null;
			if (string.IsNullOrEmpty(textureName)) return false;
			
			if (keyToTexture.TryGetValue(textureName, out var lt) && ValidateOrReloadTexture(lt))
			{
				texture = lt.Texture;
				return true;
			}
			return false;
		}

		public void ReloadChangedFiles(ICollection<string> filePaths)
		{
			if (filePaths == null || filePaths.Count == 0) return;
			foreach (var path in filePaths)
			{
				if (string.IsNullOrEmpty(path)) continue;
				var ext = Path.GetExtension(path);
				if (!SupportedExtensions.Contains(ext)) continue;

				var key = Path.GetFileNameWithoutExtension(path);
				if (!File.Exists(path))
				{
					// Deleted
					if (keyToTexture.TryGetValue(key, out var removed))
					{
						if (removed.Texture != null)
						{
							UnityEngine.Object.Destroy(removed.Texture);
						}
						keyToTexture.Remove(key);
					}

					// Also remove from material mappings derived from key
					var partsDel = key.Split(new string[] { "__" }, StringSplitOptions.None);
					if (partsDel.Length == 1)
					{
						string matKeyDel = NormalizeMaterialName(partsDel[0]);
						materialNameToTexture.Remove(matKeyDel);
					}
					else if (partsDel.Length >= 2)
					{
						string matKeyDel = NormalizeMaterialName(partsDel[0]);
						string propDel = partsDel[1];
						materialPropertyToTexture.Remove((matKeyDel, propDel));
					}
					if (Config.Preferences.DebugEnabled)
						MelonLogger.Msg($"[ReplacementIndex] Deleted mapping for '{key}'");
					continue;
				}

				LoadOrReplace(key, path);
				if (Config.Preferences.DebugEnabled)
					MelonLogger.Msg($"[ReplacementIndex] Loaded/Updated '{key}' from '{path}'");
			}
		}

		private void LoadAll()
		{
			if (!Directory.Exists(baseDirectory)) return;
			var files = Directory.EnumerateFiles(baseDirectory, "*.*", SearchOption.AllDirectories);
			foreach (var file in files)
			{
				if (file.IndexOf("_Exports", StringComparison.OrdinalIgnoreCase) >= 0) continue;
				var ext = Path.GetExtension(file);
				if (!SupportedExtensions.Contains(ext)) continue;
				var key = Path.GetFileNameWithoutExtension(file);
				LoadOrReplace(key, file);
				if (Config.Preferences.DebugEnabled)
					MelonLogger.Msg($"[ReplacementIndex] Preloaded '{key}' from '{file}'");
			}

			// Summary debug log after loading
			if (Config.Preferences.DebugEnabled)
			{
				MelonLogger.Msg($"[ReplacementIndex] Finished loading. Keys={keyToTexture.Count} materialNames={materialNameToTexture.Count} materialProperties={materialPropertyToTexture.Count}");
			}
		}

		private void LoadOrReplace(string key, string filePath)
		{
			try
			{
				var tex = LoadTextureFromFile(filePath);
				if (tex == null) return;

				if (keyToTexture.TryGetValue(key, out var existing) && existing != null)
				{
					if (existing.Texture != null)
					{
						UnityEngine.Object.Destroy(existing.Texture);
					}
					existing.FilePath = filePath;
					existing.Texture = tex;
				}
				else
				{
					keyToTexture[key] = new LoadedTexture { FilePath = filePath, Texture = tex };
				}

				// Build material-name and material+property mappings
				// Naming conventions:
				// 1) MaterialName.png → apply to primary texture (_BaseMap/_MainTex)
				// 2) MaterialName__Property.png → apply to specific property (e.g., MyMat__EmissionMap.png)
				var loadedTexture = keyToTexture[key]; // Use the same LoadedTexture instance
				var parts = key.Split(new string[] { "__" }, StringSplitOptions.None);
				if (parts.Length == 1)
				{
					string matKey = NormalizeMaterialName(parts[0]);
					materialNameToTexture[matKey] = loadedTexture;
				}
				else if (parts.Length >= 2)
				{
					string matName = NormalizeMaterialName(parts[0]);
					string propName = parts[1];
					materialPropertyToTexture[(matName, propName)] = loadedTexture;
				}
				if (Config.Preferences.DebugEnabled)
					MelonLogger.Msg($"[ReplacementIndex] Indexed key='{key}' mat='{(parts.Length>0?NormalizeMaterialName(parts[0]):"-")}' prop='{(parts.Length>1?parts[1]:"-")}'");
			}
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"[ReplacementIndex] Failed to load '{filePath}': {ex.Message}");
			}
		}

		private static Texture2D LoadTextureFromFile(string filePath)
		{
			try
			{
				byte[] data = File.ReadAllBytes(filePath);
				// Use RGBA32 with mipmaps for general use; sRGB default is fine for albedo
				Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
				if (!tex.LoadImage(data, markNonReadable: false))
				{
					UnityEngine.Object.Destroy(tex);
					return null;
				}
				tex.name = Path.GetFileNameWithoutExtension(filePath);
				tex.wrapMode = TextureWrapMode.Repeat;
				tex.filterMode = FilterMode.Bilinear;
				
				// Prevent texture from being destroyed during scene changes
				UnityEngine.Object.DontDestroyOnLoad(tex);
				
				return tex;
			}
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"[ReplacementIndex] Error reading '{filePath}': {ex.Message}");
				return null;
			}
		}

		public bool TryGetByMaterialName(string materialName, out Texture2D texture)
		{
			texture = null;
			if (string.IsNullOrEmpty(materialName)) return false;
			string key = NormalizeMaterialName(materialName);
			if (materialNameToTexture.TryGetValue(key, out var lt) && ValidateOrReloadTexture(lt))
			{
				texture = lt.Texture;
				return true;
			}
			return false;
		}

		public bool TryGetByMaterialProperty(string materialName, string propertyName, out Texture2D texture)
		{
			texture = null;
			if (string.IsNullOrEmpty(materialName) || string.IsNullOrEmpty(propertyName)) return false;
			string key = NormalizeMaterialName(materialName);
			if (materialPropertyToTexture.TryGetValue((key, propertyName), out var lt) && ValidateOrReloadTexture(lt))
			{
				texture = lt.Texture;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Validates all loaded textures and reloads any that have been destroyed.
		/// Call this after scene changes to ensure texture persistence.
		/// </summary>
		public void ValidateAndReloadAllTextures()
		{
			int reloadedCount = 0;
			int totalCount = 0;

			// Validate keyToTexture entries
			var keysToRemove = new List<string>();
			foreach (var kvp in keyToTexture)
			{
				totalCount++;
				bool wasValid = IsTextureValid(kvp.Value?.Texture);
				if (!ValidateOrReloadTexture(kvp.Value))
				{
					keysToRemove.Add(kvp.Key);
				}
				else if (!wasValid && IsTextureValid(kvp.Value.Texture))
				{
					// Texture was reloaded
					reloadedCount++;
				}
			}

			// Remove entries that couldn't be reloaded
			foreach (var key in keysToRemove)
			{
				keyToTexture.Remove(key);
			}

			// Validate materialNameToTexture entries
			var materialKeysToRemove = new List<string>();
			foreach (var kvp in materialNameToTexture)
			{
				totalCount++;
				bool wasValid = IsTextureValid(kvp.Value?.Texture);
				if (!ValidateOrReloadTexture(kvp.Value))
				{
					materialKeysToRemove.Add(kvp.Key);
				}
				else if (!wasValid && IsTextureValid(kvp.Value.Texture))
				{
					reloadedCount++;
				}
			}

			foreach (var key in materialKeysToRemove)
			{
				materialNameToTexture.Remove(key);
			}

			// Validate materialPropertyToTexture entries
			var propertyKeysToRemove = new List<(string, string)>();
			foreach (var kvp in materialPropertyToTexture)
			{
				totalCount++;
				bool wasValid = IsTextureValid(kvp.Value?.Texture);
				if (!ValidateOrReloadTexture(kvp.Value))
				{
					propertyKeysToRemove.Add(kvp.Key);
				}
				else if (!wasValid && IsTextureValid(kvp.Value.Texture))
				{
					reloadedCount++;
				}
			}

			foreach (var key in propertyKeysToRemove)
			{
				materialPropertyToTexture.Remove(key);
			}

			if (Config.Preferences.DebugEnabled)
			{
				MelonLogger.Msg($"[ReplacementIndex] Validated {totalCount} textures, reloaded {reloadedCount}, removed {keysToRemove.Count + materialKeysToRemove.Count + propertyKeysToRemove.Count} invalid entries");
			}
		}
	}
}


