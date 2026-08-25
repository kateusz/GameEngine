#version 330 core

in vec4 FragPos;

uniform vec3 u_LightPos;
uniform float u_FarPlane;

void main()
{
    float lightDistance = length(FragPos.xyz - u_LightPos);
    gl_FragDepth = lightDistance / u_FarPlane;
}
