# AssetsManager Usage Guide

## Overview

The `AssetsManager` provides a comprehensive asset management system for the Editor with automatic asset discovery, hot reload support, and metadata tracking.

## Features

- **Automatic Asset Discovery**: Recursively scans the assets directory to find all supported files
- **Hot Reload Support**: FileSystemWatcher automatically detects file changes, additions, deletions, and renames
- **Asset Metadata**: Tracks full path, asset type, and last modification time for each asset
- **Type-based Filtering**: Query assets by type (Texture, Model, Audio, etc.)
- **Efficient Lookups**: Dictionary-based registry for fast asset retrieval

## Supported Asset Types

The AssetsManager automatically recognizes the following asset types based on file extensions:

- **Texture**: `.png`, `.jpg`, `.jpeg`, `.bmp`, `.tga`
- **Model**: `.obj`, `.fbx`, `.gltf`, `.glb`, `.model`
- **Audio**: `.wav`, `.ogg`, `.mp3`
- **Shader**: `.vert`, `.frag`, `.glsl`, `.shader`
- **Scene**: `.scene`
- **Prefab**: `.prefab`
- **Script**: `.cs`
- **Font**: `.ttf`, `.otf`, `.woff`, `.woff2`

## Basic Usage

### Setting the Assets Path

```csharp
using Editor;

// Set the assets path - this triggers automatic scanning
AssetsManager.SetAssetsPath("/path/to/project/assets");

// The AssetsPath property returns the current path
string currentPath = AssetsManager.AssetsPath;
```

### Getting All Assets

```csharp
using Editor;
using System.Linq;

// Get all assets
var allAssets = AssetsManager.GetAssets();

// Count total assets
int totalCount = allAssets.Count();

// Iterate through assets
foreach (var asset in allAssets)
{
    Console.WriteLine($"Asset: {Path.GetFileName(asset.Path)}");
    Console.WriteLine($"  Type: {asset.Type}");
    Console.WriteLine($"  Modified: {asset.LastModified}");
}
```

### Filtering by Asset Type

```csharp
using Editor;

// Get all textures
var textures = AssetsManager.GetAssets(AssetType.Texture);

// Get all models
var models = AssetsManager.GetAssets(AssetType.Model);

// Get all scripts
var scripts = AssetsManager.GetAssets(AssetType.Script);
```

### Looking Up Specific Assets

```csharp
using Editor;

// Try to get a specific asset by relative path
string relativePath = "textures/player_sprite.png";
if (AssetsManager.TryGetAsset(relativePath, out var asset))
{
    Console.WriteLine($"Found: {asset.Path}");
    Console.WriteLine($"Type: {asset.Type}");
}
```

### Manual Refresh

```csharp
using Editor;

// Manually refresh the asset registry
AssetsManager.Refresh();
```

## Hot Reload Behavior

The AssetsManager automatically monitors the assets directory for changes:

- **File Created**: Automatically added to the registry
- **File Modified**: Metadata is updated
- **File Deleted**: Removed from the registry
- **File Renamed**: Old entry removed, new entry added

## Best Practices

1. **Call SetAssetsPath() Early**: Initialize when opening/creating a project
2. **Cache Filtered Results**: Cache results rather than calling `GetAssets()` every frame
3. **Use Relative Paths**: Store asset references as relative paths for portability
4. **Handle Missing Assets**: Always check the result of `TryGetAsset()`
5. **Asset Type Checks**: Use the `AssetType` enum to validate asset types
