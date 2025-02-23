#version 330 core

out vec4 o_Color;
flat out int o_EntityID; // No explicit location in 330 core

// Inputs from the vertex shader (manually expanded from struct)
in vec3 v_LocalPosition;
in vec4 v_Color;
in float v_Thickness;
in float v_Fade;
flat in int v_EntityID;

void main()
{
    // Calculate distance and fill circle with white
    float distance = 1.0 - length(v_LocalPosition);
    float circle = smoothstep(0.0, v_Fade, distance);
    circle *= smoothstep(v_Thickness + v_Fade, v_Thickness, distance);

    if (circle == 0.0)
    discard;

    // Set output color
    o_Color = v_Color;
    o_Color.a *= circle;

    color2 = v_EntityID;
}
