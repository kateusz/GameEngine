using System.Runtime.InteropServices;

namespace Engine.ImGuiNet;

public static class ImGuizmoWrapper
{
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_SetImGuiContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetImGuiContext(IntPtr ctx);
    
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_BeginFrame", CallingConvention = CallingConvention.Cdecl)]
    public static extern void BeginFrame();

    // Define the DLL import for ImGuizmo.Manipulate
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_Manipulate", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Manipulate(
        float[] viewMatrix,
        float[] projectionMatrix,
        int operation,
        int mode,
        float[] matrix,
        float[] deltaMatrix,
        float[] snapValues,
        float[] localBounds,
        float[] boundsSnap
    );

    // Define the DLL import for ImGuizmo.IsUsing
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_IsUsing", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)] // To handle the boolean return type
    public static extern bool IsUsing();

    // Import the SetOrthographic function from ImGuizmo.dll
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_SetOrthographic", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetOrthographic(bool isOrthographic);

    // Import the SetDrawlist function from ImGuizmo.dll
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_SetDrawlist", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDrawlist();

    // Import the SetRect function from ImGuizmo.dll
    [DllImport("cimguizmo.dylib", EntryPoint = "ImGuizmo_SetRect", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetRect(float x, float y, float width, float height);
}