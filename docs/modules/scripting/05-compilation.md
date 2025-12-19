# Compilation System

## Roslyn Integration

The scripting engine uses **Microsoft.CodeAnalysis** (Roslyn) for runtime C# compilation.

**Key Components:**
- **CSharpSyntaxTree** - Parses `.cs` files into syntax trees
- **CSharpCompilation** - Compiles syntax trees with metadata references
- **Assembly.Load()** - Loads compiled assembly into memory

## Compilation Pipeline

### 1. File Parsing
```csharp
// ScriptEngine.cs
var syntaxTree = CSharpSyntaxTree.ParseText(
    scriptContent,
    CSharpParseOptions.Default,
    encoding: Encoding.UTF8
);
```

**Input:** All `.cs` files in `assets/scripts/`

**Output:** Collection of `SyntaxTree` objects

### 2. Reference Resolution
```csharp
// ScriptEngine.cs:434
private IEnumerable<MetadataReference> GetReferencesFromRuntimeDirectory()
```

**Loaded Assemblies:**
- .NET Runtime (`System.Runtime`, `System.Numerics`, etc.)
- Engine assemblies (`Engine.dll`, `ECS.dll`, `Editor.dll`)
- Physics (`Box2D.NET.dll`)

**Resolution Strategy:**
1. Find .NET runtime directory from `typeof(object).Assembly.Location`
2. Load core assemblies (`System.Runtime`, `System.Collections`, etc.)
3. Search `AppDomain.CurrentDomain.GetAssemblies()` for Engine/ECS
4. Validate all required assemblies present

### 3. Compilation Options
```csharp
var compilation = CSharpCompilation.Create(
    "DynamicScripts",
    syntaxTrees,
    references,
    new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: _debugMode ? OptimizationLevel.Debug : OptimizationLevel.Release,
        allowUnsafe: true,
        warningLevel: 4
    )
);
```

**Configuration:**
- **OutputKind:** DLL (not executable)
- **Optimization:** Debug for development, Release for production
- **Unsafe Code:** Allowed
- **Warning Level:** 4 (highest)

### 4. Assembly Emission
```csharp
using var assemblyStream = new MemoryStream();
using var symbolsStream = _debugMode ? new MemoryStream() : null;

var emitResult = compilation.Emit(
    peStream: assemblyStream,
    pdbStream: symbolsStream,
    options: emitOptions
);
```

**Output:**
- **PE (Portable Executable)** - In-memory DLL
- **PDB (Program Database)** - Debug symbols (if debug mode)

### 5. Assembly Loading
```csharp
assemblyStream.Seek(0, SeekOrigin.Begin);
symbolsStream?.Seek(0, SeekOrigin.Begin);

var assemblyBytes = assemblyStream.ToArray();
var symbolBytes = symbolsStream?.ToArray();

_dynamicAssembly = Assembly.Load(assemblyBytes, symbolBytes);
```

**Loaded Into:** Current AppDomain

## Supported C# Features

**Language Version:** C# 12 (latest features supported by Roslyn)

**Available Features:**
- Records, pattern matching
- Nullable reference types
- Primary constructors
- Collection expressions
- Using declarations
- Switch expressions
- Init-only properties
- File-scoped namespaces

**Unsafe Code:** Allowed (use at your own risk)

## Available Assemblies

### .NET Core
- `System.Runtime`
- `System.Collections`
- `System.Numerics`
- `System.Linq`
- `netstandard`

### Engine
- `Engine.dll` - Core engine (Renderer, Audio, Scene)
- `ECS.dll` - Entity Component System
- `Editor.dll` - Editor types (if needed)

### Third-Party
- `Box2D.NET.dll` - Physics engine
- `Silk.NET.*` - OpenGL, Windowing, Input (via Engine)
- `ImGui.NET` - UI (via Editor)

**Note:** Scripts can use any type from these assemblies without additional setup.

## Debug Symbol Generation

### Enabling Debug Mode
```csharp
scriptEngine.EnableHybridDebugging(enable: true);
```

**Effect:**
- PDB symbols embedded in compilation
- Debugger can step through script code
- Line numbers in stack traces

### Saving Debug Symbols
```csharp
scriptEngine.SaveDebugSymbols(
    outputPath: "path/to/debug",
    assemblyName: "DynamicScripts"
);
```

**Output Files:**
- `DynamicScripts.dll` - Assembly PE
- `DynamicScripts.pdb` - Portable PDB

**Use Case:** Attach external debugger (Visual Studio, Rider) to editor process

## Error Handling

### Compile-Time Errors

**Detection:**
```csharp
var diagnostics = compilation.GetDiagnostics();
var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
```

**Reporting:**
```
❌ COMPILATION ERRORS DETECTED:
ERROR: CS1002: ; expected
  Location: PlayerController.cs(15,10): +Vector3.UnitY
ERROR: CS0103: The name 'Vctor3' does not exist in the current context
  Location: EnemyAI.cs(42,20): Vctor3 direction
```

**Error Fields:**
- **Diagnostic ID** (e.g., CS1002)
- **Message** - Human-readable error
- **Location** - File, line, column
- **Severity** - Error, Warning, Info

### Runtime Errors

**Caught By:**
```csharp
try
{
    scriptComponent.ScriptableEntity.OnCreate();
}
catch (Exception ex)
{
    Logger.Error(ex, "Error initializing script on entity {EntityName}", entity.Name);
}
```

**Logged With:**
- Exception type
- Stack trace
- Entity name
- Script type

## Compilation Diagnostics

### Pre-Emit Diagnostics
- Syntax errors
- Type resolution failures
- Missing references

### Emit Diagnostics
- Code generation failures
- Assembly verification errors

### Example Output
```csharp
scriptEngine.PrintDebugInfo();
```

**Prints:**
- Loaded assembly count
- Script type count
- Registered script names
- Compilation warnings/errors
