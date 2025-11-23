---
name: cross-platform-validation
description: Validate code for cross-platform compatibility across Windows, macOS, and Linux including path handling (Path.Combine usage), platform-specific API abstraction via Platform/SilkNet, OpenGL version compatibility (3.3+), file system case sensitivity, line ending handling, and native library dependencies. Use when adding platform-specific features or debugging platform-specific issues.
---

# Cross-Platform Validation

## Overview
This skill ensures code works correctly across Windows, macOS, and Linux by validating path handling, platform abstractions, OpenGL compatibility, file system assumptions, and native library usage.

## When to Use
Invoke this skill when:
- Adding new file I/O operations
- Working with native libraries or P/Invoke
- Implementing platform-specific features
- Debugging issues that occur on only one platform
- Adding new OpenGL rendering code
- Working with file paths or directory structures
- Handling user input or windowing

## Target Platforms

- **Windows**: Windows 10/11 (x64)
- **macOS**: macOS 11+ (x64, ARM64)
- **Linux**: Ubuntu 20.04+ and similar distributions

## Validation Areas

### 1. Path Handling

**❌ WRONG** - Hardcoded path separators:
```csharp
// Breaks on Unix (uses '/' not '\')
string path = "assets\\textures\\sprite.png";
string combined = basePath + "\\" + filename;
```

**✅ CORRECT** - Use Path.Combine and Path class:
```csharp
// Works on all platforms
string path = Path.Combine("assets", "textures", "sprite.png");
// Windows: assets\textures\sprite.png
// macOS/Linux: assets/textures/sprite.png

// Combine paths correctly
string combined = Path.Combine(basePath, filename);

// Get directory separator
char sep = Path.DirectorySeparatorChar;

// Get platform-appropriate path
string fullPath = Path.GetFullPath(relativePath);
```

**Common Path Operations**:
```csharp
// ✅ CORRECT cross-platform path handling
string projectPath = Path.Combine(Environment.GetFolderPath(
    Environment.SpecialFolder.MyDocuments), "GameEngine", "Projects");

string assetPath = Path.Combine(projectPath, "Assets", "Textures", "sprite.png");

string directory = Path.GetDirectoryName(assetPath);
string filename = Path.GetFileName(assetPath);
string extension = Path.GetExtension(assetPath);

// Check if path is absolute
bool isAbsolute = Path.IsPathRooted(assetPath);
```

### 2. File System Case Sensitivity

**Issue**: Windows is case-insensitive, macOS/Linux are case-sensitive

**❌ POTENTIAL PROBLEM**:
```csharp
// Code references "Sprite.PNG"
var texture = LoadTexture("assets/Sprite.PNG");

// File on disk is "sprite.png"
// Works on Windows (case-insensitive)
// Fails on macOS/Linux (case-sensitive)
```

**✅ SOLUTION**:
```csharp
// 1. Always use exact case matching file on disk
var texture = LoadTexture("assets/sprite.png");

// 2. For user input, normalize paths
string NormalizePath(string path)
{
    // Convert to lowercase for lookups
    return path.Replace('\\', '/').ToLowerInvariant();
}

// 3. Build asset databases with case-insensitive lookups
var assetDb = new Dictionary<string, string>(
    StringComparer.OrdinalIgnoreCase);
```

**Best Practice**: Use consistent casing convention:
- **Recommended**: All lowercase with underscores: `sprite_sheet.png`
- **Alternative**: PascalCase: `SpriteSheet.png`
- **Important**: Be consistent and match filesystem exactly

### 3. Line Endings

**Issue**: Windows (CRLF `\r\n`), Unix (LF `\n`)

**✅ SOLUTION** - Use .gitattributes:
```gitattributes
# .gitattributes
* text=auto
*.cs text eol=lf
*.md text eol=lf
*.shader text eol=lf
*.glsl text eol=lf
```

**Code Handling**:
```csharp
// ✅ CORRECT - Handle both line endings
string[] lines = content.Split(new[] { "\r\n", "\n" },
    StringSplitOptions.None);

// Or use built-in cross-platform handling
string[] lines = File.ReadAllLines(path); // Handles both
```

### 4. Platform Abstraction via SilkNet

**All platform-specific code** goes through `Engine/Platform/SilkNet/`

**✅ CORRECT**:
```csharp
// Engine/Renderer/Graphics2D.cs
public class Graphics2D
{
    private readonly IRendererAPI _rendererApi;

    public Graphics2D(IRendererAPI rendererApi)
    {
        _rendererApi = rendererApi; // Injected platform abstraction
    }

    public void DrawQuad(/* params */)
    {
        // Use abstraction, not direct OpenGL
        _rendererApi.DrawIndexed(_vertexArray, indexCount);
    }
}

// Engine/Platform/SilkNet/SilkNetRendererApi.cs
public class SilkNetRendererApi : IRendererAPI
{
    public void DrawIndexed(IVertexArray vertexArray, int indexCount)
    {
        // Platform-specific OpenGL calls here
        GL.DrawElements(PrimitiveType.Triangles, indexCount, ...);
    }
}
```

**❌ WRONG**:
```csharp
// Direct OpenGL calls outside Platform layer
public class Graphics2D
{
    public void DrawQuad(/* params */)
    {
        GL.DrawElements(...); // WRONG - breaks abstraction!
    }
}
```

### 5. OpenGL Version Compatibility

**Target**: OpenGL 3.3 Core Profile minimum

**✅ CORRECT** - Use core profile features:
```glsl
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexCoord;

out vec4 vColor;
out vec2 vTexCoord;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
    vColor = aColor;
    vTexCoord = aTexCoord;
}
```

**❌ WRONG** - Using deprecated features:
```glsl
#version 120 // Too old!

// Deprecated immediate mode
gl_Vertex = ...;
gl_Color = ...;
```

**Compatibility Checks**:
```csharp
// Check OpenGL version at startup
public void InitializeGraphics()
{
    string version = GL.GetString(StringName.Version);
    Logger.Info($"OpenGL Version: {version}");

    // Parse version and validate >= 3.3
    if (!IsVersionSupported(version, 3, 3))
    {
        throw new NotSupportedException(
            "OpenGL 3.3 or higher is required");
    }
}
```

**Platform-Specific OpenGL Notes**:
- **Windows**: Usually good OpenGL support
- **macOS**: OpenGL deprecated (supports up to 4.1), Metal preferred
  - Engine uses OpenGL 3.3 (works but not optimal)
  - Future: Consider Metal backend via Silk.NET.Metal
- **Linux**: Excellent OpenGL support via Mesa drivers

### 6. Native Library Dependencies

**Current Native Dependencies**:
1. **OpenGL**: Via graphics drivers (system-provided)
2. **OpenAL**: Audio library (bundled with Silk.NET)
3. **Box2D**: Physics (Box2D.NetStandard - managed wrapper)

**Platform-Specific Library Loading**:
```csharp
// Silk.NET handles platform-specific loading automatically
// No manual P/Invoke needed for OpenGL/OpenAL

// If manual P/Invoke needed (avoid if possible):
[DllImport("nativelib", EntryPoint = "NativeFunction")]
private static extern void NativeFunction();

// Platform-specific library names
private static string GetLibraryName()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return "nativelib.dll";
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return "libnativelib.dylib";
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        return "libnativelib.so";
    else
        throw new PlatformNotSupportedException();
}
```

### 7. File Permissions and Execution

**❌ POTENTIAL PROBLEM**:
```csharp
// Assumes file is executable (Unix requires +x permission)
Process.Start("/path/to/script.sh");
```

**✅ SOLUTION**:
```csharp
// Check platform and handle appropriately
public void ExecuteScript(string scriptPath)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // Windows: .bat or .cmd
        Process.Start("cmd.exe", $"/c {scriptPath}");
    }
    else
    {
        // Unix: Ensure script has execute permission
        Process.Start("chmod", $"+x {scriptPath}").WaitForExit();
        Process.Start("bash", scriptPath);
    }
}
```

### 8. Environment Variables

**✅ CORRECT** - Cross-platform environment access:
```csharp
// Get environment variable
string? value = Environment.GetEnvironmentVariable("MY_VAR");

// Get special folders (cross-platform)
string documents = Environment.GetFolderPath(
    Environment.SpecialFolder.MyDocuments);

string appData = Environment.GetFolderPath(
    Environment.SpecialFolder.ApplicationData);

// Platform check
bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

// Architecture check
Architecture arch = RuntimeInformation.ProcessArchitecture;
// X64, Arm64, X86, Arm, etc.
```

### 9. Character Encoding

**✅ CORRECT** - Use UTF-8 consistently:
```csharp
// Always specify encoding for text files
File.WriteAllText(path, content, Encoding.UTF8);
string content = File.ReadAllText(path, Encoding.UTF8);

// Stream operations
using var writer = new StreamWriter(path, false, Encoding.UTF8);
using var reader = new StreamReader(path, Encoding.UTF8);
```

### 10. Time and Timestamps

**✅ CORRECT** - Platform-agnostic time handling:
```csharp
// Use DateTime with UTC
DateTime now = DateTime.UtcNow;

// Use TimeSpan for durations
TimeSpan deltaTime = TimeSpan.FromSeconds(0.016);

// High-resolution timing (cross-platform via .NET)
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... timed operation ...
stopwatch.Stop();
TimeSpan elapsed = stopwatch.Elapsed;
```

## Testing Checklist

### Manual Testing (if possible)
- [ ] Test on Windows
- [ ] Test on macOS
- [ ] Test on Linux

### Code Review Checklist
- [ ] All paths use `Path.Combine()` or `Path` class methods
- [ ] No hardcoded backslashes (`\`) in paths
- [ ] File references match exact case (including extension)
- [ ] Line endings handled via .gitattributes or split on both `\r\n` and `\n`
- [ ] OpenGL code uses Core Profile (no deprecated features)
- [ ] GLSL shaders use `#version 330 core` or higher
- [ ] Native library dependencies documented and cross-platform
- [ ] Platform-specific code isolated to `Platform/` folder
- [ ] File operations use UTF-8 encoding explicitly
- [ ] Time/date handling uses UTC and TimeSpan

## Common Cross-Platform Pitfalls

### 1. Home Directory Access
```csharp
// ❌ WRONG
string home = "C:\\Users\\Username";

// ✅ CORRECT
string home = Environment.GetFolderPath(
    Environment.SpecialFolder.UserProfile);
// Windows: C:\Users\Username
// macOS: /Users/Username
// Linux: /home/username
```

### 2. Temporary Files
```csharp
// ❌ WRONG
string temp = "C:\\Temp";

// ✅ CORRECT
string temp = Path.GetTempPath();
// Platform-appropriate temp directory
```

### 3. Application Data
```csharp
// ❌ WRONG
string appData = "C:\\ProgramData\\GameEngine";

// ✅ CORRECT
string appData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "GameEngine");
// Windows: C:\Users\User\AppData\Roaming\GameEngine
// macOS: /Users/User/.config/GameEngine or ~/Library/Application Support/GameEngine
// Linux: /home/user/.config/GameEngine
```

### 4. Shader Path Loading
```csharp
// ✅ CORRECT - Use forward slashes or Path.Combine
string shaderPath = Path.Combine("Assets", "Shaders", "sprite.vert");
// Or normalize paths in asset system
string shaderPath = "Assets/Shaders/sprite.vert"; // Forward slash works everywhere
```

## Output Format

**Issue**: [Cross-platform compatibility problem]
**Location**: [File path and line number]
**Platform Impact**: [Which platforms are affected]
**Problem**: [Specific incompatibility]
**Recommendation**: [Fix with code example]
**Priority**: [Critical/High/Medium/Low]

### Example Output
```
**Issue**: Hardcoded Windows path separators
**Location**: Editor/Managers/ProjectManager.cs:45
**Platform Impact**: Breaks on macOS and Linux
**Problem**: Path uses backslashes directly instead of Path.Combine
**Recommendation**:
// BEFORE
string assetPath = basePath + "\\" + "Assets" + "\\" + filename;

// AFTER
string assetPath = Path.Combine(basePath, "Assets", filename);

**Priority**: High
```

## Reference Documentation
- **CLAUDE.md**: Cross-platform compatibility section
- **Platform Abstraction**: `Engine/Platform/SilkNet/`
- **.NET Documentation**: [RuntimeInformation](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.runtimeinformation)

## Integration with Agents
This skill works with the **game-engine-expert** agent for platform-specific low-level concerns and rendering compatibility.

## Tool Restrictions
None - this skill may read code, analyze platform dependencies, and suggest fixes.
