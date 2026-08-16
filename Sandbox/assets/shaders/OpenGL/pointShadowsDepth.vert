#version 330 core
layout(location = 0) in vec3 a_Position;

uniform mat4 u_Model;
uniform mat4 u_ShadowMatrix;

out vec4 FragPos;

void main()
{
    FragPos = vec4(a_Position, 1.0) * u_Model;
    gl_Position = FragPos * u_ShadowMatrix;
}
