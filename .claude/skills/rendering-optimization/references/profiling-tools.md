# Profiling Tools for Rendering Optimization

External tools for capturing, analyzing, and profiling rendering performance.

## GPU Profiling Tools

### RenderDoc (Cross-Platform)
**Best for**: Frame capture, GPU state inspection, draw call analysis

**Key Features**:
- Frame capture (F12 in-game)
- Complete GPU state inspection
- Texture/buffer viewer
- Shader debugger
- Draw call timeline

**Usage**:
1. Launch game through RenderDoc
2. Press F12 to capture frame
3. Analyze draw calls, state changes, texture switches
4. Identify bottlenecks (excessive draw calls, state changes)

**Download**: https://renderdoc.org/

---

### NVIDIA Nsight Graphics
**Best for**: NVIDIA GPU deep-dive profiling

**Key Features**:
- GPU trace analysis
- Shader performance profiling
- Memory bandwidth analysis
- Warp occupancy metrics
- Frame debugger

**Usage**:
1. Connect to running game
2. Capture frame range
3. Analyze GPU utilization, bottlenecks
4. Profile individual shaders

**Platform**: Windows, Linux
**Download**: https://developer.nvidia.com/nsight-graphics

---

### AMD Radeon GPU Profiler
**Best for**: AMD GPU optimization

**Key Features**:
- Low-level GPU performance counters
- Pipeline state analysis
- Wavefront occupancy
- Memory bandwidth profiling

**Platform**: Windows, Linux
**Download**: https://gpuopen.com/rgp/

---

### Xcode Instruments (macOS Metal)
**Best for**: macOS GPU profiling

**Key Features**:
- GPU frame capture
- Metal API trace
- Memory allocation tracking
- Shader profiling

**Usage**:
1. Profile → Metal System Trace
2. Capture game session
3. Analyze GPU timeline, shader costs
4. Identify overdraw, state changes

**Platform**: macOS only

---

## CPU Profiling Tools

### dotnet-trace
**Best for**: .NET CPU profiling

**Usage**:
```bash
# Start tracing
dotnet-trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime

# Analyze trace (convert to speedscope format)
dotnet-trace convert trace.nettrace --format speedscope
```

**Key Metrics**:
- Method call times
- Garbage collection pauses
- CPU hotspots in C# code

---

### Visual Studio Profiler
**Best for**: Integrated .NET profiling

**Features**:
- CPU sampling
- Memory allocation tracking
- Instrumentation profiling
- Hot path analysis

**Usage**: Debug → Performance Profiler → CPU Usage

---

### JetBrains dotTrace
**Best for**: Advanced .NET profiling

**Features**:
- Timeline profiling
- Call tree analysis
- Memory allocation profiling
- Async/await profiling

---

## In-Engine Stats Tracking

### StatsPanel (Built-in)
**Location**: `Editor/Panels/StatsPanel.cs`

**Displays**:
- FPS (frames per second)
- Frame time (milliseconds)
- Draw calls per frame
- Quads/triangles rendered
- Batch count

**Usage**: View → Stats Panel (in editor)

---

## Performance Metrics Reference

### Target Metrics
- **Frame Time**: < 16.67ms (60 FPS)
- **Draw Calls**: < 10 (2D), < 50 (3D)
- **GPU Utilization**: > 80% (avoid CPU bottleneck)
- **Batch Efficiency**: > 1000 quads/batch
- **Texture Switches**: < 5 per frame

### Bottleneck Identification
| Symptom | Likely Cause | Profiling Tool |
|---------|--------------|----------------|
| Low FPS, low GPU usage | CPU bottleneck | dotnet-trace |
| Low FPS, high GPU usage | GPU bottleneck | RenderDoc |
| Frequent frame spikes | GC pauses | dotnet-trace |
| High draw call count | Poor batching | RenderDoc |
| High frame time, few triangles | Overdraw | RenderDoc (overdraw view) |