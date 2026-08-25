using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Editor.Features.Viewport.Gizmos;

internal static class ImGuizmoNative
{
    private const string Dll = "cimgui";
    private static readonly bool Available = Detect();

    public static bool IsAvailable => Available;

    private static bool Detect()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            if (!NativeLibrary.TryLoad(Dll, out var lib))
                return false;
            return NativeLibrary.TryGetExport(lib, "ImGuizmo_BeginFrame", out _);
        }
        catch
        {
            return false;
        }
    }

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuizmo_BeginFrame();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuizmo_SetOrthographic([MarshalAs(UnmanagedType.I1)] bool isOrthographic);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuizmo_SetRect(float x, float y, float width, float height);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuizmo_SetDrawlist(IntPtr drawlist);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ImGuizmo_Manipulate(
        ref float view,
        ref float projection,
        int operation,
        int mode,
        ref float matrix,
        IntPtr deltaMatrix,
        IntPtr snap);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ImGuizmo_IsUsing();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ImGuizmo_IsOver();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuizmo_Enable([MarshalAs(UnmanagedType.I1)] bool enable);

    public static bool Manipulate(ref Matrix4x4 view, ref Matrix4x4 projection, int operation, int mode, ref Matrix4x4 matrix)
        => ImGuizmo_Manipulate(ref view.M11, ref projection.M11, operation, mode, ref matrix.M11, IntPtr.Zero, IntPtr.Zero);

    public static unsafe void SetDrawlist(ImDrawListPtr drawList)
        => ImGuizmo_SetDrawlist((IntPtr)drawList.NativePtr);
}

public enum ImGuizmoOperation
{
    Translate = 1 | 2 | 4,
    Rotate = 8 | 16 | 32 | 64,
    Scale = 128 | 256 | 512
}

internal enum ImGuizmoMode
{
    Local = 0,
    World = 1
}
