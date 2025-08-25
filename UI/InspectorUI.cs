using System;
using System.IO;
using System.Text;
using MelonLoader;
using UnityEngine;
using TextureSwapper.Config;
using TextureSwapper.Runtime;
#if IL2CPP
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
#endif

namespace TextureSwapper.UI
{
#if IL2CPP
	[RegisterTypeInIl2Cpp]
#endif
	public sealed class InspectorUI : MonoBehaviour
	{
		#if IL2CPP
		public InspectorUI(IntPtr ptr) : base(ptr) { }
		#endif
		private bool visible;
		private Camera mainCamera;
		private ReplacementIndex index;
		private MaterialScanner scanner;
		private GameObject currentTarget;
		private string lastCopy;
		private Vector2 scrollPosition;
		private float lastInspectTime = 0f;
		private readonly float inspectInterval = 0.12f; // seconds
		private GameObject lastTarget;
		private readonly System.Collections.Generic.List<Renderer> cachedRenderableRenderers = new System.Collections.Generic.List<Renderer>(16);

		private readonly GUIStyle headerStyle = new GUIStyle();
		private readonly GUIStyle smallStyle = new GUIStyle();

		#if IL2CPP
		[HideFromIl2Cpp]
		#endif
		public void Initialize(ReplacementIndex index, MaterialScanner scanner)
		{
			this.index = index;
			this.scanner = scanner;
			mainCamera = Camera.main;
			ConfigureStyles();
		}

		#if IL2CPP
		[HideFromIl2Cpp]
		#endif
		public void ToggleVisibility()
		{
			visible = !visible;
		}

		private void ConfigureStyles()
		{
			headerStyle.fontSize = 16;
			headerStyle.normal.textColor = Color.white;
			smallStyle.fontSize = 12;
			smallStyle.normal.textColor = Color.white;
		}

		private void Update()
		{
			if (!visible) return;
			if (mainCamera == null) mainCamera = Camera.main;
			if (mainCamera == null) return;

			float now = Time.realtimeSinceStartup;
			if (now - lastInspectTime < inspectInterval) return; // throttle
			lastInspectTime = now;

			// Raycast from camera forward (fast); can be switched to mouse-point if desired
			Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
			currentTarget = FindTargetFromRaycast(ray);

			// If target changed, rebuild cached renderers that actually have textured materials
			if (currentTarget != lastTarget)
			{
				cachedRenderableRenderers.Clear();
				if (currentTarget != null)
				{
					var all = currentTarget.GetComponentsInChildren<Renderer>(true);
					if (all != null && all.Length > 0)
					{
						foreach (var r in all)
						{
							if (r == null) continue;
							var mats = r.sharedMaterials;
							if (mats == null || mats.Length == 0) continue;
							bool hasTexture = false;
							foreach (var m in mats)
							{
								if (m == null) continue;
								for (int p = 0; p < PropertyNames.TexturePropertyNames.Length; p++)
								{
									string prop = PropertyNames.TexturePropertyNames[p];
									if (m.HasProperty(prop) && m.GetTexture(prop) != null)
									{
										hasTexture = true;
										break;
									}
								}
								if (hasTexture)
								{
									cachedRenderableRenderers.Add(r);
									break;
								}
							}
						}
					}
				}
				lastTarget = currentTarget;
			}
		}

		private GameObject FindTargetFromRaycast(Ray ray)
		{
			const float maxDistance = 4f;
			RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);

			if (hits.Length == 0) return null;

			// Process hits in order of distance
			foreach (var hit in hits)
			{
				if (hit.collider == null) continue;
				var obj = hit.collider.gameObject;

				// Skip objects with "collider" in the name
				if (obj.name.ToLower().Contains("collider"))
					continue;

				// Check if this object should be skipped (single "Lit" material)
				if (ShouldSkipObject(obj))
				{
					// Try to find a better alternative
					var betterTarget = FindBetterAlternative(obj);
					if (betterTarget != null)
					{
						return betterTarget;
					}
					// If no better alternative, continue to next hit
					continue;
				}

				// This is a good target
				return obj;
			}

			return null;
		}

		private bool ShouldSkipObject(GameObject obj)
		{
			if (obj == null) return true;

			var renderer = obj.GetComponent<Renderer>();
			if (renderer == null) return false; // No renderer = not skippable

			var materials = renderer.sharedMaterials;
			if (materials == null || materials.Length != 1) return false; // Need exactly one material

			var mat = materials[0];
			if (mat == null) return false;

			// Skip if the single material is named "Lit"
			return mat.name == "Lit";
		}

		private GameObject FindBetterAlternative(GameObject original)
		{
			if (original == null) return null;

			// Strategy 1: Check parent for better materials
			var parent = original.transform.parent;
			if (parent != null)
			{
				var parentObj = parent.gameObject;
				if (!ShouldSkipObject(parentObj))
				{
					return parentObj;
				}
			}

			// Strategy 2: Check children for better materials
			var children = original.GetComponentsInChildren<Renderer>(false);
			foreach (var childRenderer in children)
			{
				var childObj = childRenderer.gameObject;
				if (childObj == original) continue; // Skip self

				if (!ShouldSkipObject(childObj))
				{
					return childObj;
				}
			}

			// Strategy 3: Check siblings for better materials
			if (parent != null)
			{
				for (int i = 0; i < parent.childCount; i++)
				{
					var sibling = parent.GetChild(i).gameObject;
					if (sibling == original) continue; // Skip self

					if (!ShouldSkipObject(sibling))
					{
						return sibling;
					}
				}
			}

			return null; // No better alternative found
		}



		private void OnGUI()
		{
			if (!visible) return;

			// Solid, non-transparent background with border
			Rect windowRect = new Rect(20, 20, 600, Screen.height - 40);
			// Border
			GUI.color = Color.black;
			GUI.DrawTexture(new Rect(windowRect.x - 2, windowRect.y - 2, windowRect.width + 4, windowRect.height + 4), Texture2D.whiteTexture);
			// Background (opaque)
			GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
			GUI.DrawTexture(windowRect, Texture2D.whiteTexture);
			GUI.color = Color.white;

			// Begin scrollable content with padding
			GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 10, windowRect.width - 20, windowRect.height - 20));
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

			GUILayout.Label("TextureSwapper Inspector", headerStyle);
			GUILayout.Space(6);

			if (currentTarget == null)
			{
				GUILayout.Label("Look at an object to inspect its materials.", smallStyle);
				GUILayout.EndScrollView();
				GUILayout.EndArea();
				return;
			}

			GUILayout.Label($"Target: {GetPath(currentTarget)}", smallStyle);
			GUILayout.Space(4);
			GUILayout.Label("Drop files named by material or material__Property: e.g. MyMat.png or MyMat__EmissionMap.png", smallStyle);

			// Get all renderers in the hierarchy
			var allRenderers = currentTarget.GetComponentsInChildren<Renderer>(true);
			if (allRenderers == null || allRenderers.Length == 0)
			{
				GUILayout.Label("No Renderers found in hierarchy.", smallStyle);
				GUILayout.EndScrollView();
				GUILayout.EndArea();
				return;
			}

			bool hasAnyMaterials = false;
			for (int r = 0; r < allRenderers.Length; r++)
			{
				var renderer = allRenderers[r];
				if (renderer == null) continue;

				var materials = renderer.sharedMaterials;
				if (materials == null || materials.Length == 0) continue;

				hasAnyMaterials = true;

				// Show object path for this renderer (relative to target)
				string objectPath = GetRelativePath(currentTarget, renderer.gameObject);
				if (objectPath != ".")
				{
					GUILayout.Space(6);
					GUILayout.Label($"Object: {objectPath}", headerStyle);
				}

				for (int i = 0; i < materials.Length; i++)
				{
					var mat = materials[i];
					if (mat == null) continue;
					GUILayout.Space(4);
					GUILayout.Label($"Material [{i}]: {mat.name}", smallStyle);
					if (GUILayout.Button("Copy Material Name", GUILayout.Width(140)))
					{
						GUIUtility.systemCopyBuffer = mat.name;
						lastCopy = mat.name;
					}
					GUILayout.BeginHorizontal();
					GUILayout.Label($"Suggestion: Mods/TextureSwapper/{mat.name}.png", smallStyle);
					if (GUILayout.Button("Copy", GUILayout.Width(60)))
					{
						GUIUtility.systemCopyBuffer = $"{mat.name}.png";
						lastCopy = $"{mat.name}.png";
					}
					GUILayout.EndHorizontal();

					for (int p = 0; p < PropertyNames.TexturePropertyNames.Length; p++)
					{
						string prop = PropertyNames.TexturePropertyNames[p];
						if (!mat.HasProperty(prop)) continue;
						var tex = mat.GetTexture(prop) as Texture2D;
						if (tex == null) continue;
						GUILayout.BeginHorizontal();
						GUILayout.Label($"{prop} → {(tex != null ? tex.name : "<null>")}", smallStyle, GUILayout.Width(420));
						if (GUILayout.Button("Copy Tex Name", GUILayout.Width(110)))
						{
							if (tex != null)
							{
								GUIUtility.systemCopyBuffer = tex.name;
								lastCopy = tex.name;
							}
						}
						if (GUILayout.Button("Copy Mat__Prop", GUILayout.Width(140)))
						{
							string fname = $"{mat.name}__{prop}.png";
							GUIUtility.systemCopyBuffer = fname;
							lastCopy = fname;
						}
						GUILayout.EndHorizontal();
					}
				}
			}

			if (!hasAnyMaterials)
			{
				GUILayout.Label("No materials found in hierarchy.", smallStyle);
				GUILayout.EndScrollView();
				GUILayout.EndArea();
				return;
			}

			if (!string.IsNullOrEmpty(lastCopy))
			{
				GUILayout.Space(4);
				GUILayout.Label($"Copied: {lastCopy}", smallStyle);
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		#if IL2CPP
		[HideFromIl2Cpp]
		#endif
		public void ExportCurrentSelection()
		{
			if (currentTarget == null) return;

			// Get all renderers in the hierarchy
			var allRenderers = currentTarget.GetComponentsInChildren<Renderer>(true);
			if (allRenderers == null || allRenderers.Length == 0) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"Target: {GetPath(currentTarget)}");
			sb.AppendLine($"Renderer Count: {allRenderers.Length}");
			sb.AppendLine();

			for (int r = 0; r < allRenderers.Length; r++)
			{
				var renderer = allRenderers[r];
				if (renderer == null) continue;

				var materials = renderer.sharedMaterials;
				if (materials == null || materials.Length == 0) continue;

				// Show object path for this renderer
				string objectPath = GetRelativePath(currentTarget, renderer.gameObject);
				sb.AppendLine($"Object: {objectPath}");

				for (int i = 0; i < materials.Length; i++)
				{
					var mat = materials[i];
					if (mat == null) continue;
					sb.AppendLine($"  Material[{i}]: {mat.name}");
					for (int p = 0; p < PropertyNames.TexturePropertyNames.Length; p++)
					{
						string prop = PropertyNames.TexturePropertyNames[p];
						if (!mat.HasProperty(prop)) continue;
						var tex = mat.GetTexture(prop) as Texture2D;
						if (tex == null) continue;
						sb.AppendLine($"    {prop} → {tex.name}");
						sb.AppendLine($"      Suggestion: Mods/TextureSwapper/{tex.name}.png");
					}
				}
				sb.AppendLine();
			}

			try
			{
				var fileName = $"{Sanitize(currentTarget.name)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
				var path = Path.Combine(Core.ExportsFolderPath, fileName);
				File.WriteAllText(path, sb.ToString());
				MelonLoader.MelonLogger.Msg($"Exported: {path}");
			}
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"Export failed: {ex.Message}");
			}
		}

		private static string GetPath(GameObject go)
		{
			if (go == null) return "<null>";
			string path = go.name;
			var t = go.transform;
			while (t.parent != null)
			{
				path = t.parent.name + "/" + path;
				t = t.parent;
			}
			return path;
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

		private static string GetRelativePath(GameObject root, GameObject child)
		{
			if (root == null || child == null) return "?";
			if (root == child) return ".";

			var path = new System.Text.StringBuilder();
			var current = child.transform;

			// Build path from child up to root
			while (current != null && current.gameObject != root)
			{
				if (path.Length > 0)
					path.Insert(0, "/");
				path.Insert(0, current.name);
				current = current.parent;
			}

			if (current == null)
				return "?"; // child is not actually a child of root

			return path.Length > 0 ? path.ToString() : ".";
		}
	}
}


