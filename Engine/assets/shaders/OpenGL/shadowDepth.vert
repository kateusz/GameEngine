#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_LightSpaceMatrix;
uniform mat4 u_Model;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    gl_Position = worldPos * u_LightSpaceMatrix;
}
