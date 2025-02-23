#version 330 core

// Input attributes from the vertex buffer
layout(location = 0) in vec3 a_WorldPosition;
layout(location = 1) in vec3 a_LocalPosition;
layout(location = 2) in vec4 a_Color;
layout(location = 3) in float a_Thickness;
layout(location = 4) in float a_Fade;
layout(location = 5) in int a_EntityID;

// Camera uniform buffer (std140 is ignored in 330 core)
uniform mat4 u_ViewProjection;

// Manually expanded struct using varyings
out vec3 v_LocalPosition;
out vec4 v_Color;
out float v_Thickness;
out float v_Fade;
flat out int v_EntityID;  // Flat qualifier applied here (for fragment shader use)

void main()
{
    // Assigning values (replacing struct)
    v_LocalPosition = a_LocalPosition;
    v_Color = a_Color;
    v_Thickness = a_Thickness;
    v_Fade = a_Fade;

    v_EntityID = a_EntityID;

    // Compute final vertex position
    gl_Position = u_ViewProjection * vec4(a_WorldPosition, 1.0);
}
