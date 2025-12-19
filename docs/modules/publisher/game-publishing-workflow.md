# Game Publishing Workflow

## Overview

This document describes the workflow for publishing games created with the GameEngine. The publishing system transforms Editor projects into standalone, distributable game executables for Windows, Linux, and macOS platforms.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Publishing Pipeline](#publishing-pipeline)
- [Runtime Project](#runtime-project)
- [Publish Settings](#publish-settings)
- [Asset Management](#asset-management)
- [Platform-Specific Considerations](#platform-specific-considerations)
- [Distribution Formats](#distribution-formats)
- [Publishing from Editor](#publishing-from-editor)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### Components

The publishing system is located in `Editor/Publisher/` and consists of:

| Component | Purpose |
|-----------|---------|
| IGamePublisher | Interface defining async publishing operations with cancellation |
| GamePublisher | Main implementation handling build orchestration and asset copying |
| PublishSettings | Build configuration (platform, output path, options) |
| PublishResult | Result of publish operation (success/failure, output path, errors) |
| PublishProgress | Progress tracking for UI feedback during async operations |
| PublishSettingsUI | ImGui modal for configuring and launching builds |
| GameConfiguration | Runtime configuration (startup scene, window settings) |
| PlatformDetection | Platform detection and runtime identifier utilities |

### High-Level Architecture

The publishing flow involves three main stages:

1. **Editor Application** - Configures settings, orchestrates the build, and packages assets
2. **.NET Publish Pipeline** - Compiles the Runtime project with self-contained deployment
3. **Published Game Package** - Final output with executable, assets, and configuration

The published package contains:
- Runtime executable (with Engine.dll and dependencies)
- Assets directory (textures, shaders, audio, scenes)
- Scripts directory (for runtime compilation)
- Game configuration file (`game.config.json`)

### Publishing Flow

1. **Project Validation** - Ensure project is loaded with valid scenes and scripts directories
2. **Settings Validation** - Validate runtime identifier and configuration
3. **Temporary Build** - Build to temporary directory for atomic operations
4. **Runtime Compilation** - Execute `dotnet publish` for Runtime project
5. **Asset Copying** - Copy assets directory to build output
6. **Script Copying** - Copy scripts to build output for runtime compilation
7. **Config Generation** - Create `game.config.json` with startup scene and window settings
8. **Build Validation** - Verify executable and required files exist
9. **Finalization** - Move from temp directory to final output location

---

## Publishing Pipeline

### IGamePublisher Interface

The `IGamePublisher` interface provides:
- Legacy synchronous `Publish()` method
- Async `PublishAsync()` with progress reporting and cancellation support
- Overload accepting `GameConfiguration` for custom startup settings

### Publishing Steps

The `GamePublisher` implementation executes these steps:

1. Validate that a project is loaded with valid directories
2. Check runtime identifier is supported
3. Create temporary directory for atomic operations
4. Execute `dotnet publish` on Runtime.csproj
5. Copy project assets to build output
6. Copy scripts for runtime compilation
7. Generate `game.config.json`
8. Verify executable and config exist
9. Move from temp to final output directory

### Supported Platforms

| Platform | Runtime Identifiers |
|----------|---------------------|
| Windows | `win-x64`, `win-x86`, `win-arm64` |
| macOS | `osx-x64`, `osx-arm64` |
| Linux | `linux-x64`, `linux-arm64` |

### Progress Tracking

The `PublishProgress` class implements `IProgress<string>` and provides:
- Current step description
- Progress percentage (0-100%)
- Completion and error status flags
- Scrollable build output log

### Publish Result

The `PublishResult` class contains:
- Success/failure status
- Output path (on success)
- Error message (on failure)
- Complete build output log

---

## Runtime Project

### Purpose

The Runtime project (`Runtime/`) is a minimal executable that loads and runs published games. It provides the standalone game player without Editor dependencies.

### Structure

| File | Purpose |
|------|---------|
| Runtime.csproj | Project file with Engine reference and asset copying rules |
| Program.cs | Entry point with DI container setup and logging configuration |
| RuntimeApplication.cs | Application host derived from Engine's Application base |
| GameLayer.cs | Main game layer handling scene loading, updates, and input |
| GameConfiguration.cs | Configuration model for startup scene, window size, title, etc. |

### Runtime Behavior

On startup, the Runtime:
1. Configures Serilog logging (console and file)
2. Loads `game.config.json` from the executable directory
3. Sets up DI container using `EngineIoCContainer.Register()`
4. Registers runtime-specific services (GameLayer, RuntimeApplication)
5. Loads the startup scene and begins the game loop

### GameConfiguration Properties

| Property | Description | Default |
|----------|-------------|---------|
| StartupScenePath | Path to initial scene file | `assets/scenes/Scene.scene` |
| WindowWidth | Initial window width | 1920 |
| WindowHeight | Initial window height | 1080 |
| Fullscreen | Start in fullscreen mode | false |
| GameTitle | Window title | "My Game" |
| TargetFrameRate | Target FPS | 60 |

---

## Publish Settings

### PublishSettings Properties

| Property | Description | Default |
|----------|-------------|---------|
| OutputPath | Directory for published build | (required) |
| RuntimeIdentifier | Target platform (e.g., `win-x64`) | `win-x64` |
| SelfContained | Include .NET runtime in output | true |
| SingleFile | Package as single executable | true |
| CreatePackage | Create distributable archive | false |
| Configuration | Build configuration | "Release" |

### Debug vs Release Builds

**Debug Build:**
- Debug symbols included
- No optimization
- Larger file size
- Easier debugging

**Release Build:**
- IL optimization enabled
- Single executable (with SingleFile option)
- Self-contained .NET runtime
- Recommended for distribution

---

## Asset Management

### Asset Copying

The publisher copies the project's `assets/` directory to the build output. Scripts are copied separately to ensure they're available for runtime compilation by the ScriptEngine.

### Asset Categories

| Category | File Types | Notes |
|----------|-----------|-------|
| Textures | .png, .jpg, .bmp | Loaded by TextureFactory |
| Shaders | .vert, .frag, .glsl | OpenGL shaders |
| Audio | .wav, .ogg, .mp3 | Loaded by audio system |
| Scenes | .scene (JSON) | Scene serialization format |
| Scripts | .cs | Compiled at runtime by ScriptEngine |

### Asset Loading

Assets in the Runtime are loaded relative to `AppContext.BaseDirectory`, ensuring paths work correctly regardless of where the executable is run from.

---

## Platform-Specific Considerations

### Windows

**Considerations:**
- Include Visual C++ redistributables if needed
- Consider code signing for distribution
- Test with Windows Defender (may flag unsigned executables)

**Distribution Options:**
- Single executable file
- Installer (WiX, Inno Setup)
- Store distribution (Steam, Epic, itch.io)

### Linux

**Considerations:**
- Ensure OpenGL/Vulkan drivers are available on target systems
- Set executable permissions after copying
- Test on multiple distributions (Ubuntu, Fedora, Arch)

**Distribution Options:**
- AppImage for maximum compatibility
- Flatpak for sandboxed distribution
- Native packages (.deb, .rpm) for specific distros
- Include `.desktop` file for application menu integration

### macOS

**Considerations:**
- Create .app bundle structure for proper integration
- Code signing required for distribution outside Mac App Store
- Notarization required for Gatekeeper
- Minimum macOS version compatibility

**App Bundle Structure:**
The macOS app bundle requires:
- `Contents/MacOS/` containing the executable
- `Contents/Resources/` containing assets and icon
- `Contents/Info.plist` with app metadata

---

## Distribution Formats

### Single-File Executable

**Pros:**
- Simplest distribution
- Single download
- No installation required

**Cons:**
- Slower first startup (extraction)
- Larger memory footprint
- Cannot easily swap assets

### Self-Contained Deployment

**Pros:**
- No .NET runtime required on user machine
- Guaranteed consistent behavior
- Works offline

**Cons:**
- Larger file size (~70-100 MB for .NET)
- Separate builds required for each platform

### Framework-Dependent Deployment

**Pros:**
- Much smaller file size (~5-10 MB)
- Benefits from runtime updates

**Cons:**
- Requires .NET runtime installed on user machine
- Potential compatibility issues
- Not recommended for games

---

## Publishing from Editor

### PublishSettingsUI

The Editor provides a modal dialog for publishing with:
- Platform selection dropdown (all supported runtime identifiers)
- Output path configuration
- Build configuration (Release/Debug)
- Self-contained option toggle
- Single-file option toggle

### Progress Modal

During publishing, a progress modal displays:
- Current step description
- Progress bar (0-100%)
- Scrollable build output log
- Cancel button (or Close when complete)

Publishing runs asynchronously, allowing the Editor to remain responsive.

---

## Troubleshooting

### Game crashes on startup in published build

- Check that all assets were copied correctly to the build output
- Verify `game.config.json` exists in the build directory
- Verify startup scene path in config matches actual file location
- Check logs in `logs/runtime-.log` for detailed errors
- Test with self-contained deployment enabled

### "Runtime.csproj not found" error

- Ensure the solution file (.sln) is in the expected location
- Check that Runtime project exists in the solution
- Verify the GamePublisher can navigate to the solution directory

### Startup scene not found

- Ensure `StartupScenePath` in `game.config.json` is relative to build output
- Use forward slashes in paths (cross-platform compatible)
- Verify scene file was copied to `assets/scenes/`

### Scripts not working in published build

- Verify scripts were copied to the scripts directory
- Check that ScriptEngine has correct scripts directory path
- Review runtime logs for compilation errors

### Build fails with non-zero exit code

- Check build output in the progress modal for specific errors
- Verify .NET SDK is installed and accessible
- Ensure all project dependencies are resolvable

---

## Summary

The publishing system provides a complete workflow for creating standalone game executables:

- **IGamePublisher** - Async publishing with cancellation and progress support
- **GamePublisher** - Build orchestration, asset packaging, and validation
- **PublishSettings** - Platform, output, and build option configuration
- **Runtime project** - Standalone game player with scene loading and game loop
- **Cross-platform support** - Windows, macOS, and Linux with platform-specific considerations

The system uses `dotnet publish` to compile the Runtime project, copies project assets and scripts, generates runtime configuration, and validates the output before finalizing the build.
