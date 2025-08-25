using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using TextureSwapper.Config;
#if IL2CPP
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime;
#endif

namespace TextureSwapper.Runtime
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public sealed class MaterialScanner : MonoBehaviour
    {
#if IL2CPP
        public MaterialScanner(System.IntPtr ptr) : base(ptr)
        {
        }
#endif
        private readonly List<Renderer> rendererBuffer = new List<Renderer>(256);
        private readonly HashSet<int> processedMaterialIds = new HashSet<int>();
        private ReplacementIndex replacementIndex;
        private bool fullRescanRequested;
        private int lastRendererIndex;
        private float nextAllowedRescanTime;
        private readonly List<Camera> cameraCache = new List<Camera>(4);
        private bool scanningInProgress;
        private int totalRenderersFound;
        private int totalRenderersProcessed;

        private static bool ShouldSkipRenderer(Renderer renderer)
        {
            if (renderer == null) return true;
            var go = renderer.gameObject;
            if (go != null)
            {
                var name = go.name;
                if (!string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains("collider"))
                    return true;
            }

            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return true;
            // Do not skip single-material renderers based solely on material name; users may target these

            return false;
        }

        private static bool IsRendererActive(Renderer renderer)
        {
            if (renderer == null) return false;
            var go = renderer.gameObject;
            return go != null && go.activeInHierarchy && renderer.enabled;
        }

        private bool IsRendererVisible(Renderer renderer)
        {
            return renderer != null && renderer.isVisible;
        }

        private float SquaredDistanceToNearestCamera(Vector3 position)
        {
            float min = float.MaxValue;
            for (int i = 0; i < cameraCache.Count; i++)
            {
                var cam = cameraCache[i];
                if (cam == null || !cam.enabled) continue;
                float d = (cam.transform.position - position).sqrMagnitude;
                if (d < min) min = d;
            }

            return (min == float.MaxValue ? 0f : min);
        }

#if IL2CPP
        [HideFromIl2Cpp]
#endif
        public void Initialize(ReplacementIndex index)
        {
            replacementIndex = index;
            fullRescanRequested = true;
            if (Preferences.DebugEnabled)
                MelonLogger.Msg("[MaterialScanner] Initialize and request full rescan");
        }

#if IL2CPP
        [HideFromIl2Cpp]
#endif
        public void RequestFullRescan()
        {
            fullRescanRequested = true;
            if (Preferences.DebugEnabled)
                MelonLogger.Msg("[MaterialScanner] Full rescan requested");
        }

        private void LateUpdate()
        {
            if (!Preferences.Enabled.Value || replacementIndex == null) return;

            // Handle periodic rescan scheduling
            HandlePeriodicRescan();

            // Handle full rescan if requested
            if (fullRescanRequested)
            {
                StartFullRescan();
            }

            // Continue batch processing if we have renderers to process
            if (scanningInProgress && rendererBuffer.Count > 0)
            {
                ProcessRendererBatch();
            }
        }

        private void HandlePeriodicRescan()
        {
            float interval = Preferences.RescanIntervalSeconds?.Value ?? 0f;
            if (interval > 0f && Time.unscaledTime >= nextAllowedRescanTime)
            {
                fullRescanRequested = true;
                nextAllowedRescanTime = Time.unscaledTime + interval;
            }
        }

         private void StartFullRescan()
         {
             processedMaterialIds.Clear();
             rendererBuffer.Clear();
             lastRendererIndex = 0;
             totalRenderersProcessed = 0;

             // Validate and reload any destroyed textures before scanning
             if (replacementIndex != null)
             {
                 replacementIndex.ValidateAndReloadAllTextures();
             }

             // Cache active cameras once per rescan
             RefreshCameraCache();

             // Find and filter all renderers
             DiscoverAndFilterRenderers();

             // Start scanning process
             scanningInProgress = rendererBuffer.Count > 0;
             fullRescanRequested = false;

             if (Preferences.DebugEnabled)
             {
                 bool hasActiveCameras = cameraCache.Count > 0 &&
                                         cameraCache.Exists(c =>
                                             c != null && c.enabled && c.gameObject.activeInHierarchy);
                 string filterInfo = hasActiveCameras
                     ? $"cameras={cameraCache.Count}"
                     : "no active cameras, skipping vis/dist filters";
                 MelonLogger.Msg(
                     $"[MaterialScanner] Started full rescan: found {totalRenderersFound} renderers, {rendererBuffer.Count} after filters ({filterInfo})");
             }
         }

        private void RefreshCameraCache()
        {
            cameraCache.Clear();
            var cams = Camera.allCameras;
            if (cams != null)
                cameraCache.AddRange(cams);
        }

        private void DiscoverAndFilterRenderers()
        {
            var foundRenderers = FindObjectsOfType<Renderer>(true);
            totalRenderersFound = foundRenderers?.Length ?? 0;

            if (foundRenderers == null || foundRenderers.Length == 0)
                return;

            // Pre-calculate filter settings
            bool hasActiveCameras = cameraCache.Count > 0 &&
                                    cameraCache.Exists(c => c != null && c.enabled && c.gameObject.activeInHierarchy);
            bool useVisibilityFilter = Preferences.OnlyScanVisible?.Value == true && hasActiveCameras;
            bool useDistanceFilter = Preferences.MaxScanDistance?.Value > 0f && hasActiveCameras;
            bool useActiveFilter = Preferences.OnlyScanActive?.Value == true;
            float maxDistanceSq = useDistanceFilter
                ? Preferences.MaxScanDistance.Value * Preferences.MaxScanDistance.Value
                : 0f;

            // Ensure adequate capacity
            if (rendererBuffer.Capacity < foundRenderers.Length)
                rendererBuffer.Capacity = foundRenderers.Length;

            // Filter renderers in one pass
            for (int i = 0; i < foundRenderers.Length; i++)
            {
                var renderer = foundRenderers[i];

                // Apply all filters efficiently
                if (ShouldSkipRenderer(renderer)) continue;
                if (useActiveFilter && !IsRendererActive(renderer)) continue;
                if (useVisibilityFilter && !IsRendererVisible(renderer)) continue;
                if (useDistanceFilter)
                {
                    float distSq = SquaredDistanceToNearestCamera(renderer.bounds.center);
                    if (distSq > maxDistanceSq) continue;
                }

                rendererBuffer.Add(renderer);
            }
        }

        private void ProcessRendererBatch()
        {
            int batchSize = Mathf.Max(8, Preferences.ScanBatchSize.Value);
            int endIndex = Mathf.Min(rendererBuffer.Count, lastRendererIndex + batchSize);
            int processedInThisBatch = 0;

            for (int i = lastRendererIndex; i < endIndex; i++)
            {
                var renderer = rendererBuffer[i];
                if (ProcessSingleRenderer(renderer))
                {
                    processedInThisBatch++;
                    totalRenderersProcessed++;
                }
            }

            // Update scan progress
            lastRendererIndex = endIndex;
            bool scanCompleted = lastRendererIndex >= rendererBuffer.Count;

            if (scanCompleted)
            {
                CompleteScanPass();
            }

            if (Preferences.DebugEnabled && processedInThisBatch > 0)
            {
                float progress = scanCompleted ? 100f : (float)lastRendererIndex / rendererBuffer.Count * 100f;
                MelonLogger.Msg(
                    $"[MaterialScanner] Processed batch: {processedInThisBatch} renderers, progress: {progress:F1}% ({lastRendererIndex}/{rendererBuffer.Count})");
            }
        }

        private bool ProcessSingleRenderer(Renderer renderer)
        {
            if (renderer == null) return false;

            // Re-check dynamic filters for real-time changes
            if (Preferences.OnlyScanActive?.Value == true && !IsRendererActive(renderer)) return false;
            if (Preferences.OnlyScanVisible?.Value == true && !IsRendererVisible(renderer)) return false;

            float maxDistance = Preferences.MaxScanDistance?.Value ?? 0f;
            if (maxDistance > 0f)
            {
                float distSq = SquaredDistanceToNearestCamera(renderer.bounds.center);
                if (distSq > maxDistance * maxDistance) return false;
            }

            // Process materials
            var materials = renderer.sharedMaterials;
            if (materials == null) return false;

            bool anyMaterialProcessed = false;
            for (int m = 0; m < materials.Length; m++)
            {
                var mat = materials[m];
                if (mat == null) continue;

                int materialId = mat.GetInstanceID();
                if (processedMaterialIds.Contains(materialId)) continue;

                bool applied = false;
                // First try material-name/property mapping (robust for IL2CPP where textures may be null)
                applied |= ReplacementApplicator.ApplyByMaterialMapping(renderer, mat, replacementIndex);
                // Then fallback to texture-name mapping if the material has textures
                if (!applied)
                {
                    applied |= ReplacementApplicator.ApplyAllTextureProperties(mat, replacementIndex);
                }

                if (applied && Preferences.DebugEnabled)
                {
                    MelonLogger.Msg(
                        $"[MaterialScanner] Applied replacements to material '{mat.name}' on '{renderer.gameObject.name}'");
                }

                // Mark this material as processed to avoid redundant work
                processedMaterialIds.Add(materialId);
                anyMaterialProcessed = true;
            }

            return anyMaterialProcessed;
        }

        private void CompleteScanPass()
        {
            scanningInProgress = false;
            lastRendererIndex = 0;

            if (Preferences.DebugEnabled)
            {
                MelonLogger.Msg(
                    $"[MaterialScanner] Completed scan pass: processed {totalRenderersProcessed}/{rendererBuffer.Count} renderers");
            }

            // Always clear buffer after scan to save memory - rescans are triggered by scene changes or file updates
            rendererBuffer.Clear();
            processedMaterialIds.Clear();
        }
    }
}


