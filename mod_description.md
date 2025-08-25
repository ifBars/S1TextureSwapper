# TextureSwapper – Real‑Time Texture Swapping

[![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.x-brightgreen.svg?style=flat-square)](https://melonwiki.xyz/#/)
[![Schedule I](https://img.shields.io/badge/Schedule%20I-Mono%20%7C%20IL2CPP-blue.svg?style=flat-square)]()
[![Version](https://img.shields.io/badge/Version-0.1.0-blueviolet.svg?style=flat-square)]()

**Swap game textures on the fly without restarting. Inspect materials in-game, drop in PNGs, and see changes instantly. Built for performance and ease-of-use.**

---

## 🎨 What This Mod Does

TextureSwapper lets you replace materials’ textures in *Schedule I* at runtime. Use the built-in inspector overlay to discover the exact material (and property) names you need, then drop PNG files into the mod folder. File changes are detected live and applied immediately.

---

## ✨ Key Features

- **Real-time Texture Replacement**: No restarts, changes apply as soon as files update
- **Built-in Inspector (F8)**: Look at an object to see its renderers, materials, and texture properties
- **Copy-to-Clipboard**: Copy material names (and `Mat__Prop` suggestions on Mono)
- **Exports (F9)**: Save a full report of the current target’s materials and properties to `_Exports/`
- **Smart Scanning**: Efficient, single-pass scene scans with active/visible/distance filters
- **Live File Monitoring**: Auto-reload changed files from `Mods/TextureSwapper/`

---

## 🚀 Quick Start

1. Launch the game with the mod installed.
2. Press **F8** to open the TextureSwapper inspector overlay.
3. Look at an object to inspect its materials. You’ll see entries like `Material [i]: <MaterialName>`.
4. Click **Copy Material Name**, then save your PNG as `MaterialName.png` in `Mods/TextureSwapper/`.
5. To target a specific texture property (e.g., emission/normal): on **Mono**, use **Copy Mat__Prop** to get a filename like `MaterialName__PropertyName.png` (e.g., `MyMat__EmissionMap.png`).
6. Alternatively, use **UnityExplorer** to select the object → check its `Renderer -> Materials -> Element -> name` for the base material name. For properties, open the material and use shader property names like `_MainTex`, `_BumpMap`, `_EmissionMap`.
7. Changes are applied live. Press **F9** to export a full text report to `_Exports/` if needed.

> Tip: The inspector is throttled for performance, but expect some FPS drop while it’s open. If you see `(Instance)` or `(Clone)` suffixes, prefer the base material name for file naming.

---

## 🛠️ Installation

### Requirements
- **[MelonLoader](https://melonwiki.xyz/#/)** for Schedule I

### Steps
1. Install MelonLoader for Schedule I
2. Download the appropriate build:
   - **IL2CPP**: `TextureSwapper_Il2cpp.dll`
   - **Mono**: `TextureSwapper_Mono.dll`
3. Place the DLL in your `Mods` folder
4. Launch the game — the mod initializes automatically

---

## ⚙️ Configuration

All settings are available via **MelonPreferences** under `TextureSwapper`:

- `Enabled` (true) — master toggle
- `LiveReload` (true) — apply texture file changes automatically
- `DebugLogging` (false) — verbose logging for troubleshooting
- `ScanBatchSize` (96) — renderers processed per frame
- `OnlyScanActive` (true) — skip inactive renderers
- `OnlyScanVisible` (true) — scan only what cameras can see
- `MaxScanDistance` (500) — skip renderers beyond this distance (0 = unlimited)
- `RescanIntervalSeconds` (0) — periodic rescans (0 = only on scene changes/file updates)

Settings live in `UserData/MelonPreferences.cfg` under `[TextureSwapper]`.

---

## 🎯 Perfect For

- Modders who want rapid iteration on textures
- Players customizing visuals without restarts
- Debugging and reverse-engineering material setups

---

## 👥 Credits

- **Bars** — Design, implementation, UI, scanning optimizations

---

**Customize textures instantly. Inspect, drop, enjoy.** 🎨✨
