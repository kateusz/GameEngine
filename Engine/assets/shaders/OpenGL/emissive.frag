#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int o_EntityID;

in vec2 v_TexCoord;
flat in int v_EntityID;

uniform sampler2D u_EmissiveMap;
uniform vec3 u_Emissive;

void main()
{
    o_Color = vec4(texture(u_EmissiveMap, v_TexCoord).rgb * u_Emissive, 0.0);
    o_EntityID = v_EntityID;
}
