#version 330 core

in vec3 v_WorldPos;
in vec2 v_TexCoord;

uniform vec3 u_LightPosition;
uniform float u_LightRange;
uniform sampler2D u_DiffuseMap;
uniform int u_AlphaTest;
uniform float u_AlphaCutoff;

void main()
{
    if (u_AlphaTest != 0 && texture(u_DiffuseMap, v_TexCoord).a < u_AlphaCutoff)
        discard;

    gl_FragDepth = length(v_WorldPos - u_LightPosition) / u_LightRange;
}
