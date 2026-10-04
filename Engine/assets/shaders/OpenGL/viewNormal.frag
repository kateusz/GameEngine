#version 330 core

layout(location = 0) out vec4 o_Normal;

in vec3 v_ViewNormal;
in vec2 v_TexCoord;

uniform sampler2D u_DiffuseMap;
uniform int u_AlphaTest;
uniform float u_AlphaCutoff;

void main()
{
    if (u_AlphaTest != 0 && texture(u_DiffuseMap, v_TexCoord).a < u_AlphaCutoff)
        discard;

    o_Normal = vec4(normalize(v_ViewNormal), 0.0);
}
