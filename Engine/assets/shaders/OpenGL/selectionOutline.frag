#version 330 core

in vec2 v_TexCoord;
layout(location = 0) out vec4 o_Color;

uniform sampler2D u_Color;
uniform isampler2D u_EntityIds;
uniform int u_IdCount;
uniform int u_Ids[256];

bool Selected(ivec2 p)
{
    ivec2 size = textureSize(u_EntityIds, 0);
    if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y)
        return false;
    int id = texelFetch(u_EntityIds, p, 0).r;
    if (id <= 0)
        return false;
    for (int i = 0; i < u_IdCount; ++i)
    {
        if (id == u_Ids[i])
            return true;
    }
    return false;
}

void main()
{
    vec4 color = texture(u_Color, v_TexCoord);
    ivec2 p = ivec2(gl_FragCoord.xy);
    if (Selected(p))
    {
        o_Color = color;
        return;
    }
    if (Selected(p + ivec2(1, 0))
        || Selected(p + ivec2(-1, 0))
        || Selected(p + ivec2(0, 1))
        || Selected(p + ivec2(0, -1)))
    {
        o_Color = vec4(1.0, 0.55, 0.0, 1.0);
        return;
    }
    o_Color = color;
}
