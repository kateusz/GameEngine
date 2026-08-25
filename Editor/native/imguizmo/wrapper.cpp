#include "imgui.h"
#include "ImGuizmo.h"

extern "C" {

__declspec(dllexport) void ImGuizmo_BeginFrame()
{
    ImGuizmo::BeginFrame();
}

__declspec(dllexport) void ImGuizmo_SetOrthographic(bool isOrthographic)
{
    ImGuizmo::SetOrthographic(isOrthographic);
}

__declspec(dllexport) void ImGuizmo_SetRect(float x, float y, float width, float height)
{
    ImGuizmo::SetRect(x, y, width, height);
}

__declspec(dllexport) void ImGuizmo_SetDrawlist(ImDrawList* drawlist)
{
    ImGuizmo::SetDrawlist(drawlist);
}

__declspec(dllexport) bool ImGuizmo_Manipulate(
    const float* view,
    const float* projection,
    int operation,
    int mode,
    float* matrix,
    float* deltaMatrix,
    const float* snap)
{
    return ImGuizmo::Manipulate(
        view,
        projection,
        static_cast<ImGuizmo::OPERATION>(operation),
        static_cast<ImGuizmo::MODE>(mode),
        matrix,
        deltaMatrix,
        snap);
}

__declspec(dllexport) bool ImGuizmo_IsUsing()
{
    return ImGuizmo::IsUsing();
}

__declspec(dllexport) bool ImGuizmo_IsOver()
{
    return ImGuizmo::IsOver();
}

__declspec(dllexport) void ImGuizmo_Enable(bool enable)
{
    ImGuizmo::Enable(enable);
}

}
