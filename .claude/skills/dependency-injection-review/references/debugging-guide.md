# Dependency Injection Debugging Guide

This guide provides detailed troubleshooting steps and automated validation scripts for diagnosing DI-related issues.

## Common Errors and Solutions

### Error: "No service registered for type X"
**Solution**: Add registration to `Program.cs` ConfigureServices
**Check**: Search for the type in Program.cs to ensure it's registered

**Example:**
```csharp
// Missing registration causes error:
public class MyPanel
{
    public MyPanel(ICustomService service) { } // ❌ ICustomService not registered
}

// Fix: Add to Program.cs
container.Register<ICustomService, CustomService>(Reuse.Singleton);
```

---

### Error: "Circular dependency detected"
**Solution**: Refactor using decision tree from main skill (extract shared dependency, use events, or pass data directly)
**Check**: Review constructor dependencies for A→B→A cycles

**Example:**
```csharp
// Circular dependency:
public class ServiceA
{
    public ServiceA(IServiceB b) { } // A depends on B
}
public class ServiceB
{
    public ServiceB(IServiceA a) { } // B depends on A - CIRCULAR!
}

// Fix: Extract shared dependency
public class ServiceA
{
    public ServiceA(ISharedData shared) { }
}
public class ServiceB
{
    public ServiceB(ISharedData shared) { }
}
```

---

### Error: "Service is null after construction"
**Solution**: Ensure service is registered before dependent services
**Check**: Registration order matters for complex dependency graphs

**Example:**
```csharp
// Wrong order can cause issues:
container.Register<DependentService>(Reuse.Singleton);  // Registered first
container.Register<IRequiredService, RequiredService>(Reuse.Singleton);  // Needed by above

// DryIoc usually handles this, but complex graphs may need explicit ordering
```

---

### Error: "Multiple constructors found"
**Solution**: Use `[DryIoc.Attributes.Constructor]` attribute on preferred constructor
**Check**: Remove extra constructors if not needed

**Example:**
```csharp
public class MyService
{
    // Multiple constructors confuse DI container
    public MyService() { }
    public MyService(IDependency dep) { }

    // Fix: Mark preferred constructor
    [DryIoc.Attributes.Constructor]
    public MyService(IDependency dep) { }
}
```

---

### Error: "Disposed object accessed"
**Solution**: Check service lifetimes - don't inject transient into singleton
**Check**: Verify Reuse.Singleton for all long-lived services

**Example:**
```csharp
// Singleton depending on Transient causes disposal issues:
container.Register<ITransientService, TransientService>(Reuse.Transient);
container.Register<SingletonService>(Reuse.Singleton);

public class SingletonService
{
    public SingletonService(ITransientService transient) { }
    // ❌ Transient may be disposed while Singleton still uses it
}

// Fix: Make dependency Singleton or restructure
container.Register<ITransientService, TransientService>(Reuse.Singleton);
```

---

## Automated Validation Scripts

### Detect Static Singletons

**Linux/macOS:**
```bash
grep -rn "static.*Instance.*=>" --include="*.cs" Engine/ Editor/ | grep -v "Constants.cs"
```

**Windows PowerShell:**
```powershell
Get-ChildItem -Path Engine/,Editor/ -Filter *.cs -Recurse |
  Select-String "static.*Instance.*=>" |
  Where-Object { $_.Path -notlike "*Constants.cs" }
```

**Expected Output (if violations found):**
```
Engine/Services/BadService.cs:42:    public static BadService Instance => _instance ??= new();
Editor/Managers/LegacyManager.cs:15:    public static LegacyManager Instance => _instance;
```

**Expected Output (if clean):**
```
(no output - all clear!)
```

---

### Find Service Locator Usage

**Linux/macOS:**
```bash
grep -rn "ServiceLocator\|\.Resolve<" --include="*.cs" Engine/ Editor/
```

**Windows PowerShell:**
```powershell
Get-ChildItem -Path Engine/,Editor/ -Filter *.cs -Recurse |
  Select-String "ServiceLocator|\.Resolve<"
```

**Expected Output (if violations found):**
```
Engine/Systems/BadSystem.cs:78:    _factory = ServiceLocator.Resolve<ITextureFactory>();
```

**Expected Output (if clean):**
```
(no output - using proper constructor injection!)
```

---

### Check for Property Injection

**Linux/macOS:**
```bash
grep -rn "{ get; set; }.*Factory\|{ get; set; }.*Manager" --include="*.cs" Engine/ Editor/
```

**Windows PowerShell:**
```powershell
Get-ChildItem -Path Engine/,Editor/ -Filter *.cs -Recurse |
  Select-String "{ get; set; }.*Factory|{ get; set; }.*Manager"
```

**Expected Output (if violations found):**
```
Editor/Panels/OldPanel.cs:22:    public ITextureFactory TextureFactory { get; set; }
```

**Expected Output (if clean):**
```
(no output - using constructor injection!)
```

---

## Validation Workflow

Run all validation scripts in sequence:

1. **Check for static singletons** (should return no results except Constants.cs)
2. **Check for service locator** (should return no results)
3. **Check for property injection** (some legitimate cases exist, review each)
4. **Build and run** to catch runtime DI errors
5. **Watch startup logs** for circular dependency errors

If any violations are found, refer to the main SKILL.md for refactoring patterns.
