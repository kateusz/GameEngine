#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;

out vec3 v_WorldPos;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_WorldPos = worldPos.xyz;
    gl_Position = worldPos * u_ViewProjection;
}
