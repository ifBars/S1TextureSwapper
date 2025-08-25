# TextureSwapper

A powerful texture replacement mod for *Schedule I*, allowing real-time texture swapping without game restarts. Built with performance optimization and user-friendly inspection tools for seamless texture customization.

## 🎨 Features

### **Core Texture System**
- **Real-time Texture Replacement**: Swap textures instantly while the game is running
- **Live File Monitoring**: Automatically detects and applies texture file changes
- **Material & Property Mapping**: Support for both material names and specific texture properties
- **Batch Processing**: Efficient renderer scanning with configurable batch sizes

### **Interactive Inspector UI**
- **Real-time Material Inspection**: Look at objects to see their materials and textures
- **Copy-to-Clipboard**: Easy copying of material names (and texture property names on Mono only)
- **File Naming Suggestions**: Automatic suggestions for texture file naming conventions
- **Export Functionality**: Save detailed material information to text files

## 🛠️ Installation

### Prerequisites
- **MelonLoader** installed for *Schedule I*
- Game version: Both Mono and IL2CPP builds supported

### Installation Steps
1. Download the latest **TextureSwapper.dll** for your game version
2. Place the DLL into the `Mods` folder within your *Schedule I* directory
3. Launch the game — the mod will automatically create necessary subdirectories

### Version Compatibility
- **Mono Build**: `TextureSwapper_Mono.dll` for alternate branch
- **IL2CPP Build**: `TextureSwapper_Il2cpp.dll` for main branch

## 🎮 Usage

### **Basic Texture Replacement**
1. **Prepare Textures**: Create PNG files with appropriate names
2. **Drop Files**: Place texture files in `Mods/TextureSwapper/`
3. **Automatic Application**: Textures are applied immediately to matching materials

### **File Naming Conventions**
- **Material-based**: `MaterialName.png` (applies to all properties of that material)
- **Property-specific**: `MaterialName__PropertyName.png` (applies to specific texture property)

### **Inspector Tool (built-in F8)**
Use the built-in inspector to quickly find the material or property name you should use for your PNG files:

1. Press `F8` (or your configured `InspectorKey`) to open the TextureSwapper inspector overlay.
2. Look at the object in the game world that you want to change — the inspector will inspect the object the camera is looking at.
3. The inspector lists all renderers and materials found on the targeted object. Each material is shown as:
   - `Material [i]: <MaterialName>`
4. Use the **Copy Material Name** button to copy the exact `MaterialName` to your clipboard. Save your PNG as `MaterialName.png`.
5. To target a specific texture property (e.g. emission, normal), use the `Copy Mat__Prop` button (only available on Mono) next to the property row. That copies a filename in the form `MaterialName__PropertyName.png`.
6. If you prefer a full report, press `F9` (or configured `ExportKey`) to export a text file with all renderers, materials, and property names for the current inspected object. Export files are saved to the `_Exports` folder in the mod directory.

Notes:
- The inspector throttles raycasts and caches renderers for performance, however FPS will still drop with the inspector open.
- Material names sometimes include suffixes like `(Instance)` or `(Clone)` — prefer the base material name (the inspector copy button will copy the name it sees; if you get unexpected suffixes, remove them when naming files).

### **Using UnityExplorer (alternative)**
If you have UnityExplorer (or a similar in-game inspector) installed, you can find material names directly in Unity's object hierarchy:

1. Open UnityExplorer while the game is running and the object is visible.
2. In the hierarchy or scene view, select the GameObject you want to inspect.
3. In the Inspector panel, expand the `Renderer` or `MeshRenderer` component.
4. Under `Materials`, inspect each material element — the material's `name` field is the value to use when naming your PNG.
   - Example path: `GameObject -> MeshRenderer -> Materials -> Element 0 -> name` → use `Element0Name.png`.
5. For property-specific textures, inspect the material asset (click the material entry to open it) and check texture slots such as `Main Texture`, `Bump Map`, `Emission Map`, etc.; use the corresponding shader property name (e.g. `_MainTex`, `_BumpMap`, `_EmissionMap`) when composing `MaterialName__PropertyName.png`.

Notes on UnityExplorer:
- UnityExplorer can be especially useful for when the built-in inspector is not able to inspect the object you are looking at.
- Some material names visible in UnityExplorer may include `Instance` suffixes, it is recommended to use the base material name.

### **Supported Texture Properties**
The mod automatically detects and supports these texture properties:
- `_MainTex` (Albedo/Diffuse)
- `_BumpMap` (Normal map)
- `_EmissionMap` (Emission texture)
- `_MetallicGlossMap` (Metallic/Smoothness)
- `_OcclusionMap` (Ambient Occlusion)
- And many more Unity standard properties

## ⚙️ Configuration

All settings are available via the **MelonPreferences** system under the `TextureSwapper` category:

### **Core Settings**
| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Toggle the TextureSwapper mod on/off |
| `LiveReload` | `true` | Auto-reload textures when files change |
| `DebugLogging` | `false` | Enable verbose debug logging for troubleshooting |

### **Scanning & Performance**
| Setting | Default | Description |
|---------|---------|-------|
| `ScanBatchSize` | `96` | Renderer scan batch size per frame (higher = faster but more frame drops) |
| `OnlyScanActive` | `true` | Scan only active renderers (exclude inactive for performance) |
| `OnlyScanVisible` | `true` | Scan only renderers currently visible to a camera |
| `MaxScanDistance` | `500.0` | Max distance from camera for scanning (0 = unlimited) |
| `RescanIntervalSeconds` | `0.0` | Periodic rescan interval in seconds (0 = scan only on scene changes and file updates) |

### **Hotkeys**
| Setting | Default | Description |
|---------|---------|-------------|
| `InspectorKey` | `F8` | Toggle inspector overlay key |
| `ExportKey` | `F9` | Export current target info key |

## 🐛 Troubleshooting

### **Common Issues**
- **Mod not loading**: Ensure MelonLoader is properly installed and the mod is placed in the `Mods` folder.
- **Textures not applying**: Check file naming conventions and placement
- **Performance issues**: Reduce `ScanBatchSize` or enable `OnlyScanVisible`

### **Debug Information**
Enable debug logging via `DebugLogging` preference to see detailed scanning and replacement information. (It is recommended to enable this when reporting issues.)

### **File Structure**
Ensure your texture files are placed in the correct directory:
```
Mods/
└── TextureSwapper/
    ├── MaterialName.png
    ├── MaterialName__PropertyName.png
    └── _Exports/ (auto-created for exports)
```

## 📄 License

This mod is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.