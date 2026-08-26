#version 330 core

in vec2 v_TexCoord;
layout(location = 0) out vec4 o_Color;

uniform sampler2D u_Texture;
uniform float u_Intensity;
uniform float u_Radius;

void main()
{
    vec4 color = texture(u_Texture, v_TexCoord);
    float dist = distance(v_TexCoord, vec2(0.5)) * 2.0;
    float edge = smoothstep(u_Radius, 1.0, dist);
    float darken = mix(1.0, 1.0 - u_Intensity, edge);
    o_Color = vec4(color.rgb * darken, color.a);
}
