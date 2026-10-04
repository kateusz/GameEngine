#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_Color;

void main()
{
    vec4 sampleColor = texture(u_Color, v_TexCoord);
    vec3 rgb = sampleColor.rgb / (sampleColor.rgb + vec3(1.0));
    rgb = pow(rgb, vec3(1.0 / 2.2));
    o_Color = vec4(rgb, sampleColor.a);
}
