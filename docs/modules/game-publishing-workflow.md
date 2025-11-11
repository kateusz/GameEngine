# Game Publishing Workflow

## Overview

This document describes the comprehensive workflow for publishing games created with the GameEngine. The publishing system transforms Editor projects into standalone, distributable game executables for Windows, Linux, and macOS platforms.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Publishing Pipeline](#publishing-pipeline)
- [Runtime Project](#runtime-project)
- [Build Configurations](#build-configurations)
- [Asset Management](#asset-management)
- [Script Compilation Strategies](#script-compilation-strategies)
- [Platform-Specific Considerations](#platform-specific-considerations)
- [Distribution Formats](#distribution-formats)
- [Publishing from Editor](#publishing-from-editor)
- [Advanced Topics](#advanced-topics)

---

## Architecture Overview

### Components

The publishing system consists of several key components:

```
┌─────────────────────────────────────────────────────────┐
│                    Editor Application                    │
│  ┌────────────────────────────────────────────────────┐ │
│  │           IGamePublisher Interface                 │ │
│  │  • Project configuration                           │ │
│  │  • Build orchestration                             │ │
│  │  • Asset packaging                                 │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                  .NET Publish Pipeline                   │
│  • Compilation                                           │
│  • IL Linking                                            │
│  • Native AOT (optional)                                 │
│  • Platform-specific builds                              │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                 Published Game Package                   │
│  ┌───────────────┐  ┌────────────────┐                  │
│  │ Runtime.exe   │  │    Assets/     │                  │
│  │ (or binary)   │  │  • Textures    │                  │
│  │               │  │  • Shaders     │                  │
│  │ + Engine.dll  │  │  • Audio       │                  │
│  │ + ECS.dll     │  │  • Models      │                  │
│  │ + Dependencies│  │  • Scripts     │                  │
│  │               │  │  • Scenes      │                  │
│  └───────────────┘  └────────────────┘                  │
└─────────────────────────────────────────────────────────┘
```

### Publishing Flow

1. **Project Preparation**: Validate project structure, scenes, and assets
2. **Build Configuration**: Select target platform, architecture, and optimization level
3. **Script Processing**: Pre-compile or package scripts for runtime compilation
4. **Runtime Compilation**: Build the Runtime project with .NET publish
5. **Asset Packaging**: Copy and organize all required assets
6. **Post-Processing**: Platform-specific packaging (e.g., .app bundles for macOS)
7. **Distribution**: Create final distributable package

---

## Publishing Pipeline

### Phase 1: Pre-Publishing Validation

Before building, the publisher validates:

- **Scene Integrity**: All referenced scenes exist and are valid
- **Asset References**: No broken texture, model, or audio references
- **Script Compilation**: All scripts compile without errors
- **Dependencies**: All required DLLs and native libraries are present
- **Project Settings**: Valid game title, version, icon, etc.

```csharp
public class PublishValidator
{
    public ValidationResult ValidateProject(ProjectConfig config)
    {
        var results = new List<ValidationIssue>();

        // Validate scenes
        foreach (var sceneRef in config.Scenes)
        {
            if (!File.Exists(sceneRef.Path))
                results.Add(ValidationIssue.Error($"Scene not found: {sceneRef.Path}"));
        }

        // Validate assets
        var assetValidator = new AssetValidator();
        results.AddRange(assetValidator.ValidateAssetReferences(config));

        // Validate scripts
        var scriptEngine = ScriptEngine.Instance;
        var (success, errors) = scriptEngine.CompileAllScripts();
        if (!success)
            results.AddRange(errors.Select(e => ValidationIssue.Error(e)));

        return new ValidationResult(results);
    }
}
```

### Phase 2: Build Configuration

Define build parameters:

```csharp
public record BuildConfiguration
{
    public required string ProjectName { get; init; }
    public required string Version { get; init; }

    // Platform settings
    public required TargetPlatform Platform { get; init; }
    public required TargetArchitecture Architecture { get; init; }

    // Optimization settings
    public required OptimizationLevel Optimization { get; init; }
    public bool EnableAOT { get; init; } = false;
    public bool TrimUnusedCode { get; init; } = true;

    // Script settings
    public ScriptCompilationMode ScriptMode { get; init; } = ScriptCompilationMode.PreCompile;

    // Distribution settings
    public bool SingleFile { get; init; } = true;
    public bool SelfContained { get; init; } = true;

    // Output settings
    public required string OutputDirectory { get; init; }
}

public enum TargetPlatform
{
    Windows,
    Linux,
    macOS
}

public enum TargetArchitecture
{
    x64,
    ARM64,
    x86 // For Windows only
}

public enum OptimizationLevel
{
    Debug,
    Release,
    ReleaseOptimized
}

public enum ScriptCompilationMode
{
    PreCompile,      // Compile all scripts into game assembly
    RuntimeCompile,  // Package .cs files for runtime compilation
    Hybrid           // Core scripts pre-compiled, mod scripts runtime
}
```

### Phase 3: Runtime Build Process

Execute the .NET publish pipeline:

```bash
# Basic publish command structure
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r <runtime-identifier> \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:EnableCompressionInSingleFile=true \
    -o <output-directory>

# Runtime identifiers for different platforms:
# Windows x64:  win-x64
# Windows ARM:  win-arm64
# Linux x64:    linux-x64
# Linux ARM:    linux-arm64
# macOS x64:    osx-x64
# macOS ARM:    osx-arm64
```

Advanced optimization options:

```bash
# With trimming and optimization
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    /p:TrimMode=link \
    /p:DebugType=none \
    /p:DebugSymbols=false \
    -o Builds/MyGame-Windows-x64

# With Native AOT (experimental, requires C++ toolchain)
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r win-x64 \
    /p:PublishAot=true \
    -o Builds/MyGame-Windows-x64-AOT
```

### Phase 4: Asset Packaging

Copy and organize all required assets:

```csharp
public class AssetPackager
{
    public void PackageAssets(BuildConfiguration config)
    {
        var outputAssetsDir = Path.Combine(config.OutputDirectory, "assets");
        Directory.CreateDirectory(outputAssetsDir);

        // Copy asset directories
        var assetCategories = new[]
        {
            "textures",
            "shaders",
            "audio",
            "models",
            "fonts",
            "scenes",
            "scripts" // If using runtime compilation
        };

        foreach (var category in assetCategories)
        {
            var sourceDir = Path.Combine(config.ProjectPath, "assets", category);
            var destDir = Path.Combine(outputAssetsDir, category);

            if (Directory.Exists(sourceDir))
            {
                CopyDirectory(sourceDir, destDir, recursive: true);
            }
        }

        // Process shaders (ensure correct platform versions)
        ProcessShaders(config);

        // Optimize textures if configured
        if (config.OptimizeTextures)
        {
            OptimizeTextures(outputAssetsDir);
        }
    }

    private void ProcessShaders(BuildConfiguration config)
    {
        var shadersDir = Path.Combine(config.OutputDirectory, "assets", "shaders");

        // Currently using OpenGL shaders - ensure they're included
        var openGLShadersSource = Path.Combine(config.ProjectPath, "assets", "shaders", "OpenGL");
        var openGLShadersDest = Path.Combine(shadersDir, "OpenGL");

        if (Directory.Exists(openGLShadersSource))
        {
            CopyDirectory(openGLShadersSource, openGLShadersDest, recursive: true);
        }

        // Future: Add DirectX/Metal shader compilation for different platforms
    }
}
```

### Phase 5: Scene Packaging

Package all scenes used by the game:

```csharp
public class ScenePackager
{
    public void PackageScenes(BuildConfiguration config)
    {
        var scenesDir = Path.Combine(config.OutputDirectory, "assets", "scenes");
        Directory.CreateDirectory(scenesDir);

        // Get all scenes from project
        var sceneFiles = Directory.GetFiles(
            Path.Combine(config.ProjectPath, "assets", "scenes"),
            "*.scene",
            SearchOption.AllDirectories
        );

        foreach (var sceneFile in sceneFiles)
        {
            var relativePath = Path.GetRelativePath(
                Path.Combine(config.ProjectPath, "assets", "scenes"),
                sceneFile
            );

            var destPath = Path.Combine(scenesDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(sceneFile, destPath, overwrite: true);
        }

        // Create startup scene config
        CreateStartupConfig(config, scenesDir);
    }

    private void CreateStartupConfig(BuildConfiguration config, string scenesDir)
    {
        var startupConfig = new
        {
            StartupScene = config.StartupScene ?? "MainScene.scene",
            WindowTitle = config.ProjectName,
            WindowWidth = config.DefaultWindowWidth ?? 1280,
            WindowHeight = config.DefaultWindowHeight ?? 720,
            Fullscreen = config.StartFullscreen ?? false,
            VSync = config.EnableVSync ?? true
        };

        var configPath = Path.Combine(config.OutputDirectory, "game.config.json");
        File.WriteAllText(configPath, JsonSerializer.Serialize(startupConfig, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
```

---

## Runtime Project

### Structure

The Runtime project is a minimal executable that loads and runs published games:

```
Runtime/
├── Runtime.csproj          # Project file
├── Program.cs              # Entry point
├── RuntimeApplication.cs   # Game application host
└── Config/
    └── RuntimeConfig.cs    # Runtime configuration loader
```

### Runtime.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net9.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>

        <!-- Publishing optimization -->
        <PublishTrimmed>true</PublishTrimmed>
        <TrimMode>link</TrimMode>
        <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
        <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>

        <!-- Optional: Enable ReadyToRun for faster startup -->
        <PublishReadyToRun>true</PublishReadyToRun>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="../Engine/Engine.csproj" />
        <ProjectReference Include="../ECS/ECS.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="DryIoc" Version="5.4.3" />
    </ItemGroup>

</Project>
```

### Program.cs

```csharp
using DryIoc;
using Engine.Core;
using Engine.Core.Window;
using Engine.Events;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Scripting;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Text.Json;

namespace Runtime;

public class Program
{
    public static void Main(string[] args)
    {
        // Load runtime configuration
        var config = LoadRuntimeConfig();

        var container = new Container();
        ConfigureContainer(container, config);

        try
        {
            var app = container.Resolve<RuntimeApplication>();

            // Load startup scene
            var sceneManager = container.Resolve<SceneManager>();
            sceneManager.LoadScene(config.StartupScene);

            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Runtime Error: {e.Message}");
            Console.WriteLine(e.StackTrace);

            // Log to file for release builds
            File.WriteAllText("crash.log", $"{e.Message}\n\n{e.StackTrace}");
        }
    }

    private static RuntimeConfig LoadRuntimeConfig()
    {
        var configPath = Path.Combine(Environment.CurrentDirectory, "game.config.json");

        if (!File.Exists(configPath))
        {
            // Use defaults if config missing
            return new RuntimeConfig
            {
                StartupScene = "MainScene.scene",
                WindowTitle = "Game",
                WindowWidth = 1280,
                WindowHeight = 720,
                Fullscreen = false,
                VSync = true
            };
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<RuntimeConfig>(json)
            ?? throw new Exception("Failed to load runtime config");
    }

    private static void ConfigureContainer(Container container, RuntimeConfig config)
    {
        // Window setup
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(config.WindowWidth, config.WindowHeight);
        options.Title = config.WindowTitle;
        options.VSync = config.VSync;

        if (config.Fullscreen)
        {
            options.WindowState = WindowState.Fullscreen;
        }

        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Window.Create(options))
        );

        container.Register<IGameWindow>(Reuse.Singleton,
            made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
        );

        // Core services
        container.Register<EventBus, EventBus>(Reuse.Singleton);
        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<Engine.Audio.IAudioEngine,
            Engine.Platform.SilkNet.Audio.SilkNetAudioEngine>(Reuse.Singleton);

        // Scene systems
        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<SceneSystemRegistry>(Reuse.Singleton);
        container.Register<SceneManager>(Reuse.Singleton);
        container.Register<SpriteRenderingSystem>(Reuse.Singleton);
        container.Register<ModelRenderingSystem>(Reuse.Singleton);
        container.Register<ScriptUpdateSystem>(Reuse.Singleton);
        container.Register<SubTextureRenderingSystem>(Reuse.Singleton);
        container.Register<PhysicsDebugRenderSystem>(Reuse.Singleton);
        container.Register<AudioSystem>(Reuse.Singleton);
        container.Register<AnimationSystem>(Reuse.Singleton);
        container.Register<AnimationAssetManager>(Reuse.Singleton);

        // Application
        container.Register<RuntimeApplication>(Reuse.Singleton);

        container.ValidateAndThrow();
    }
}
```

### RuntimeApplication.cs

```csharp
using Engine.Core;
using Engine.Events;
using Engine.Scene;
using Engine.Scripting;

namespace Runtime;

public class RuntimeApplication : Application
{
    private readonly SceneSystemRegistry _systemRegistry;
    private readonly IScriptEngine _scriptEngine;

    public RuntimeApplication(
        IGameWindow window,
        EventBus eventBus,
        SceneSystemRegistry systemRegistry,
        IScriptEngine scriptEngine)
        : base(window, eventBus)
    {
        _systemRegistry = systemRegistry;
        _scriptEngine = scriptEngine;

        // Set up script directory
        var scriptsPath = Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
        _scriptEngine.SetScriptsDirectory(scriptsPath);
    }

    protected override void OnUpdate(TimeSpan deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Update all registered systems
        _systemRegistry.UpdateSystems(CurrentScene.Instance, deltaTime);

        // Update scripts
        _scriptEngine.OnUpdate(deltaTime);
    }

    protected override void OnEvent(Event e)
    {
        base.OnEvent(e);

        // Forward events to scripts
        _scriptEngine.ProcessEvent(e);
    }
}
```

---

## Build Configurations

### Debug Build

For development and testing:

```bash
dotnet publish Runtime/Runtime.csproj \
    -c Debug \
    -r win-x64 \
    --self-contained true \
    /p:DebugType=full \
    /p:DebugSymbols=true \
    -o Builds/Debug
```

**Characteristics:**
- Debug symbols included
- No optimization
- Larger file size
- Easier debugging
- Script hot-reload enabled

### Release Build

For production distribution:

```bash
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    /p:TrimMode=link \
    /p:EnableCompressionInSingleFile=true \
    /p:DebugType=none \
    /p:DebugSymbols=false \
    -o Builds/MyGame-Windows-x64
```

**Characteristics:**
- Full IL optimization
- Dead code elimination
- Compressed single file
- Smallest file size
- No debug symbols

### High-Performance Build

Maximum runtime performance:

```bash
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    /p:TrimMode=link \
    /p:PublishReadyToRun=true \
    /p:TieredCompilation=true \
    /p:TieredCompilationQuickJit=false \
    -o Builds/MyGame-HighPerf
```

**Characteristics:**
- ReadyToRun ahead-of-time compilation
- Faster startup time
- Optimized for throughput
- Larger file size than standard release

---

## Asset Management

### Asset Categories

| Category  | File Types | Copied to Output | Notes |
|-----------|-----------|------------------|-------|
| Textures  | .png, .jpg, .bmp | Yes | Can be optimized/compressed |
| Shaders   | .vert, .frag, .glsl | Yes | Platform-specific |
| Audio     | .wav, .ogg, .mp3 | Yes | Can be compressed |
| Models    | .obj, .fbx, .gltf | Yes | Can be pre-processed |
| Fonts     | .ttf, .otf | Yes | Subset if possible |
| Scenes    | .scene (JSON) | Yes | Required |
| Scripts   | .cs | Conditional | Depends on compilation mode |

### Asset Optimization

```csharp
public class AssetOptimizer
{
    public void OptimizeAssets(string assetsDirectory, AssetOptimizationConfig config)
    {
        if (config.CompressTextures)
            CompressTextures(Path.Combine(assetsDirectory, "textures"));

        if (config.CompressAudio)
            CompressAudio(Path.Combine(assetsDirectory, "audio"));

        if (config.StripUnusedAssets)
            StripUnusedAssets(assetsDirectory);
    }

    private void CompressTextures(string texturesDir)
    {
        // Convert large PNGs to compressed formats
        // Generate mipmaps
        // Resize textures if needed
    }

    private void CompressAudio(string audioDir)
    {
        // Convert WAV to OGG for smaller size
        // Adjust bitrate for background music vs SFX
    }

    private void StripUnusedAssets(string assetsDir)
    {
        // Scan all scenes for asset references
        // Remove unreferenced assets
    }
}
```

### Asset Loading in Runtime

Assets are loaded relative to the executable:

```csharp
// In published game
var texturePath = Path.Combine(
    Environment.CurrentDirectory,
    "assets",
    "textures",
    "player.png"
);

var texture = Texture.LoadFromFile(texturePath);
```

---

## Script Compilation Strategies

### 1. Pre-Compilation (Recommended for Production)

**Approach**: Compile all scripts into the game assembly during publishing.

**Advantages:**
- Faster game startup (no runtime compilation)
- Smaller distribution (no Roslyn dependencies)
- Scripts can be optimized with IL trimming
- More secure (no source code in distribution)

**Disadvantages:**
- No hot-reload in published builds
- Harder to mod
- Requires recompilation for script changes

**Implementation:**

Create a Scripts project that gets compiled into the Runtime:

```xml
<!-- Scripts/Scripts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="../Engine/Engine.csproj" />
        <ProjectReference Include="../ECS/ECS.csproj" />
    </ItemGroup>
</Project>
```

During publishing, copy all .cs files from assets/scripts to Scripts/ and compile them with Runtime.

### 2. Runtime Compilation

**Approach**: Package .cs script files and compile them at startup using Roslyn.

**Advantages:**
- Easy modding support
- Hot-reload in published builds
- Flexible for user-generated content

**Disadvantages:**
- Slower startup time
- Larger distribution (includes Roslyn)
- Potential security concerns (arbitrary code execution)
- Cannot use IL trimming on scripts

**Implementation:**

Package Roslyn dependencies:

```xml
<!-- Runtime/Runtime.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.x.x" />
</ItemGroup>
```

Copy scripts during publishing:

```csharp
CopyDirectory(
    Path.Combine(projectPath, "assets", "scripts"),
    Path.Combine(outputPath, "assets", "scripts"),
    recursive: true
);
```

### 3. Hybrid Approach (Best of Both Worlds)

**Approach**: Pre-compile core game scripts, allow runtime compilation for mods/extensions.

**Advantages:**
- Fast startup for core gameplay
- Modding support for extensions
- Balanced file size

**Disadvantages:**
- More complex build pipeline
- Need to separate core vs mod scripts

**Implementation:**

```csharp
public class HybridScriptSystem
{
    private Assembly _precompiledScripts;
    private ScriptEngine _runtimeEngine;

    public void Initialize()
    {
        // Load pre-compiled scripts
        _precompiledScripts = Assembly.LoadFrom("GameScripts.dll");
        RegisterPrecompiledScripts();

        // Initialize runtime compiler for mods
        _runtimeEngine = ScriptEngine.Instance;
        _runtimeEngine.SetScriptsDirectory(
            Path.Combine(Environment.CurrentDirectory, "mods", "scripts")
        );
    }
}
```

---

## Platform-Specific Considerations

### Windows

**Runtime Identifier**: `win-x64`, `win-x86`, `win-arm64`

**Considerations:**
- Include Visual C++ redistributables if needed
- Consider code signing for distribution
- Test with Windows Defender (may flag unsigned executables)
- Can use DirectX for rendering (future enhancement)

**Distribution:**
- Single .exe file with `/p:PublishSingleFile=true`
- Optional: Create installer with WiX or Inno Setup
- Optional: Distribute via Steam, Epic, itch.io

### Linux

**Runtime Identifier**: `linux-x64`, `linux-arm64`

**Considerations:**
- Ensure OpenGL/Vulkan drivers are available
- Include .desktop file for application menu
- Set executable permissions: `chmod +x GameName`
- Test on multiple distros (Ubuntu, Fedora, Arch)

**Distribution:**
- AppImage for maximum compatibility
- Flatpak for sandboxed distribution
- Native packages (.deb, .rpm) for specific distros

**Example .desktop file:**

```ini
[Desktop Entry]
Type=Application
Name=MyGame
Exec=/path/to/mygame
Icon=/path/to/icon.png
Categories=Game;
```

### macOS

**Runtime Identifier**: `osx-x64`, `osx-arm64`

**Considerations:**
- Create .app bundle structure
- Code signing required for distribution outside Mac App Store
- Notarization required for Gatekeeper
- Metal rendering for better performance (future enhancement)

**App Bundle Structure:**

```
MyGame.app/
└── Contents/
    ├── Info.plist
    ├── MacOS/
    │   └── MyGame (executable)
    └── Resources/
        ├── Icon.icns
        └── assets/
            ├── textures/
            ├── shaders/
            └── ...
```

**Info.plist template:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>MyGame</string>
    <key>CFBundleDisplayName</key>
    <string>My Amazing Game</string>
    <key>CFBundleIdentifier</key>
    <string>com.mystudio.mygame</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>MyGame</string>
    <key>CFBundleIconFile</key>
    <string>Icon.icns</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
</dict>
</plist>
```

---

## Distribution Formats

### Single-File Executable

**Configuration:**
```xml
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

**Pros:**
- Simplest distribution
- Single download
- No installation required

**Cons:**
- Slower first startup (extraction)
- Larger memory footprint
- Cannot easily swap assets

### Self-Contained Deployment

**Configuration:**
```bash
--self-contained true
```

**Pros:**
- No .NET runtime required on user machine
- Guaranteed consistent behavior
- Works offline

**Cons:**
- Much larger file size (~70-100 MB for .NET 9)
- Separate builds for each platform

### Framework-Dependent Deployment

**Configuration:**
```bash
--self-contained false
```

**Pros:**
- Smaller file size (~5-10 MB)
- Benefits from runtime updates

**Cons:**
- Requires .NET 9 runtime installed
- Potential compatibility issues
- Not recommended for games

---

## Publishing from Editor

### UI Integration

The Editor should provide a "Publish" panel with the following options:

```csharp
public class PublishPanel
{
    private BuildConfiguration _config = new()
    {
        Platform = TargetPlatform.Windows,
        Architecture = TargetArchitecture.x64,
        Optimization = OptimizationLevel.Release,
        ScriptMode = ScriptCompilationMode.PreCompile,
        SingleFile = true,
        SelfContained = true
    };

    public void OnImGuiRender()
    {
        ImGui.Begin("Publish Game");

        // Platform selection
        if (ImGui.BeginCombo("Platform", _config.Platform.ToString()))
        {
            foreach (var platform in Enum.GetValues<TargetPlatform>())
            {
                if (ImGui.Selectable(platform.ToString()))
                    _config.Platform = platform;
            }
            ImGui.EndCombo();
        }

        // Architecture selection
        if (ImGui.BeginCombo("Architecture", _config.Architecture.ToString()))
        {
            foreach (var arch in Enum.GetValues<TargetArchitecture>())
            {
                if (ImGui.Selectable(arch.ToString()))
                    _config.Architecture = arch;
            }
            ImGui.EndCombo();
        }

        // Optimization level
        if (ImGui.BeginCombo("Optimization", _config.Optimization.ToString()))
        {
            foreach (var opt in Enum.GetValues<OptimizationLevel>())
            {
                if (ImGui.Selectable(opt.ToString()))
                    _config.Optimization = opt;
            }
            ImGui.EndCombo();
        }

        // Script compilation mode
        if (ImGui.BeginCombo("Script Mode", _config.ScriptMode.ToString()))
        {
            foreach (var mode in Enum.GetValues<ScriptCompilationMode>())
            {
                if (ImGui.Selectable(mode.ToString()))
                    _config.ScriptMode = mode;
            }
            ImGui.EndCombo();
        }

        // Distribution options
        ImGui.Checkbox("Single File", ref _config.SingleFile);
        ImGui.Checkbox("Self Contained", ref _config.SelfContained);

        // Output directory
        ImGui.InputText("Output Directory", ref _config.OutputDirectory, 256);
        ImGui.SameLine();
        if (ImGui.Button("Browse..."))
        {
            // Open folder browser
        }

        ImGui.Separator();

        // Build button
        if (ImGui.Button("Build Game", new Vector2(200, 40)))
        {
            StartBuild();
        }

        ImGui.End();
    }

    private void StartBuild()
    {
        var publisher = new GamePublisher();
        Task.Run(() =>
        {
            var result = publisher.Publish(_config);
            // Show result notification
        });
    }
}
```

### Build Progress Tracking

Show real-time build progress:

```csharp
public class BuildProgressTracker
{
    public event Action<string> OnStatusUpdate;
    public event Action<float> OnProgressUpdate;

    public void UpdateStatus(string status)
    {
        OnStatusUpdate?.Invoke(status);
        Logger.Information("Build: {Status}", status);
    }

    public void UpdateProgress(float progress)
    {
        OnProgressUpdate?.Invoke(progress);
    }
}
```

### Post-Build Actions

After successful build:

```csharp
public void OnBuildComplete(BuildResult result)
{
    if (result.Success)
    {
        // Show success notification
        ShowNotification("Build completed successfully!", NotificationType.Success);

        // Offer to open build folder
        if (ShowDialog("Build complete. Open build folder?"))
        {
            OpenFolder(result.OutputPath);
        }

        // Offer to test run
        if (ShowDialog("Run the built game?"))
        {
            RunBuiltGame(result.ExecutablePath);
        }
    }
    else
    {
        // Show error details
        ShowErrorDialog("Build failed", result.Errors);
    }
}
```

---

## Advanced Topics

### Code Signing

For professional distribution, sign executables:

**Windows:**
```bash
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com MyGame.exe
```

**macOS:**
```bash
codesign --deep --force --verify --verbose --sign "Developer ID Application: Company Name" MyGame.app
```

### Asset Encryption

Protect assets from casual extraction:

```csharp
public class AssetEncryptor
{
    public void EncryptAssets(string assetsPath, string password)
    {
        // Create encrypted asset bundle
        var bundlePath = Path.ChangeExtension(assetsPath, ".bundle");
        using var bundle = new AssetBundle(bundlePath, password);

        foreach (var file in Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories))
        {
            bundle.AddFile(file);
        }
    }
}
```

### DLC and Modding Support

Structure for downloadable content:

```
Game/
├── MyGame.exe
├── assets/
│   └── base/           # Base game assets
└── dlc/
    ├── dlc1/
    │   └── assets/     # DLC 1 assets
    └── dlc2/
        └── assets/     # DLC 2 assets
```

### Analytics Integration

Add telemetry to published builds:

```csharp
public class GameAnalytics
{
    public void TrackEvent(string category, string action, string label = null)
    {
        // Send to analytics service
        // Only in Release builds
        #if RELEASE
        // Implementation
        #endif
    }
}
```

### Auto-Update System

Implement game updates:

```csharp
public class UpdateChecker
{
    public async Task<UpdateInfo> CheckForUpdates()
    {
        // Check remote server for new version
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        var latestVersion = await FetchLatestVersion();

        if (latestVersion > currentVersion)
        {
            return new UpdateInfo
            {
                Available = true,
                Version = latestVersion,
                DownloadUrl = GetDownloadUrl(latestVersion)
            };
        }

        return UpdateInfo.None;
    }
}
```

---

## Complete Publishing Workflow Example

### Step-by-Step Process

1. **Open Editor** → Load your game project
2. **Validate Project** → File → Validate Project
   - Fix any errors or warnings
3. **Open Publish Panel** → File → Publish Game
4. **Configure Build:**
   - Platform: Windows
   - Architecture: x64
   - Optimization: Release
   - Script Mode: Pre-Compile
   - Single File: Yes
   - Self Contained: Yes
   - Output: `C:\Builds\MyGame-v1.0`
5. **Click Build** → Wait for completion
6. **Test Build** → Run from output directory
7. **Package for Distribution:**
   - Windows: Create ZIP or installer
   - Linux: Create AppImage or package
   - macOS: Create DMG
8. **Upload to Distribution Platform:**
   - Steam
   - itch.io
   - Epic Games Store
   - Your own website

### Automated Build Script

For CI/CD or batch building:

```bash
#!/bin/bash
# build-all.sh - Build for all platforms

GAME_NAME="MyGame"
VERSION="1.0.0"
OUTPUT_DIR="./Builds"

# Windows x64
echo "Building for Windows x64..."
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    -o "$OUTPUT_DIR/$GAME_NAME-Windows-x64-v$VERSION"

# Linux x64
echo "Building for Linux x64..."
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    -o "$OUTPUT_DIR/$GAME_NAME-Linux-x64-v$VERSION"

# macOS ARM64 (Apple Silicon)
echo "Building for macOS ARM64..."
dotnet publish Runtime/Runtime.csproj \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    -o "$OUTPUT_DIR/$GAME_NAME-macOS-ARM64-v$VERSION"

# Copy assets to all builds
for build_dir in "$OUTPUT_DIR"/*; do
    echo "Copying assets to $build_dir..."
    cp -r assets "$build_dir/"
done

echo "All builds complete!"
```

---

## Troubleshooting

### Common Issues

**Issue**: Game crashes on startup in published build but works in Editor

**Solutions:**
- Check that all assets are copied correctly
- Verify asset paths are relative, not absolute
- Ensure all DLL dependencies are included
- Test with `--self-contained true`

---

**Issue**: Large file size (>200 MB)

**Solutions:**
- Enable trimming: `/p:PublishTrimmed=true`
- Use single file: `/p:PublishSingleFile=true`
- Remove unused dependencies
- Optimize/compress assets

---

**Issue**: Slow startup time

**Solutions:**
- Use ReadyToRun: `/p:PublishReadyToRun=true`
- Pre-compile scripts instead of runtime compilation
- Lazy-load assets
- Reduce single-file extraction overhead

---

**Issue**: Scripts not working in published build

**Solutions:**
- Check script compilation mode
- If using runtime compilation, ensure Roslyn is included
- Verify scripts are copied to output
- Check script paths

---

## Summary

This comprehensive workflow covers:

✅ **Architecture** - Understanding the publishing pipeline
✅ **Runtime Project** - Creating the standalone game executable
✅ **Build Configurations** - Debug, Release, and optimized builds
✅ **Asset Management** - Packaging and optimizing game assets
✅ **Script Compilation** - Pre-compile, runtime, and hybrid strategies
✅ **Platform Support** - Windows, Linux, and macOS considerations
✅ **Distribution** - Creating distributable packages
✅ **Editor Integration** - Publishing directly from the Editor
✅ **Advanced Topics** - Signing, encryption, DLC, updates

The publishing system transforms Editor projects into professional, distributable games ready for players on multiple platforms.
