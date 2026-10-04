#version 330 core

in vec3 v_WorldPos;

uniform vec3 u_LightPosition;
uniform float u_LightRange;

void main()
{
    gl_FragDepth = length(v_WorldPos - u_LightPosition) / u_LightRange;
}
