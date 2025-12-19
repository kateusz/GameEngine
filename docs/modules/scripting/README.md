# Scripting Engine Documentation

Runtime C# scripting system powered by Roslyn with hot-reload support, ECS integration, and comprehensive debugging capabilities.

## Documentation Sections

1. [Architecture](./01-architecture.md) - System components and data flow
2. [Getting Started](./02-getting-started.md) - Create your first script
3. [API Reference](./03-api-reference.md) - Complete ScriptableEntity API
4. [Hot Reload](./04-hot-reload.md) - File watching and recompilation
5. [Compilation](./05-compilation.md) - Roslyn integration and error handling
6. [Advanced Topics](./06-advanced.md) - Performance, serialization, scene context
7. [Examples](./07-examples.md) - Common patterns and code samples
8. [Debugging](./08-debugging.md) - Debug symbols, logging, diagnostics
9. [Best Practices](./09-best-practices.md) - Recommended patterns and optimization
10. [Integration & Extension](./10-integration.md) - Custom base classes and editor integration
11. [Limitations & Caveats](./11-limitations.md) - Known constraints and workarounds
12. [Appendix](./12-appendix.md) - File locations, troubleshooting, quick reference

## Quick Links

**Key Files:**
- `Engine/Scripting/ScriptEngine.cs` - Core implementation
- `Engine/Scene/ScriptableEntity.cs` - Script base class
- `Engine/Scene/Systems/ScriptUpdateSystem.cs` - ECS system (priority 110)
- `Editor/ComponentEditors/ScriptComponentEditor.cs` - Editor UI

**Script Location:** `assets/scripts/` (relative to project root)
