#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec2 v_Clip;

uniform samplerCube u_Skybox;
uniform float u_Intensity;
uniform mat4 u_InverseViewProjection;

void main()
{
    vec4 world = vec4(v_Clip, 1.0, 1.0) * u_InverseViewProjection;
    vec3 dir = normalize(world.xyz / max(abs(world.w), 1e-8));
    o_Color = vec4(texture(u_Skybox, dir).rgb * u_Intensity, 1.0);
    o_EntityID = -1;
}
