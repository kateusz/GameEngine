#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_InverseViewProjection;

out vec3 v_Direction;

void main()
{
    vec4 clip = vec4(a_Position.xy, 1.0, 1.0);
    vec4 world = clip * u_InverseViewProjection;
    v_Direction = world.xyz / max(world.w, 1e-6);
    // Depth unused for skybox pass (CPU disables depth test); keep far NDC for safety
    gl_Position = vec4(a_Position.xy, 1.0, 1.0);
}
