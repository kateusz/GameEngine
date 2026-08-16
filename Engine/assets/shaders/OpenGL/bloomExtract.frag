#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_HdrBuffer;
uniform float u_Threshold;

void main()
{
    vec3 hdr = texture(u_HdrBuffer, v_TexCoord).rgb;
    float brightness = dot(hdr, vec3(0.2126, 0.7152, 0.0722));
    o_Color = brightness > u_Threshold ? vec4(hdr, 1.0) : vec4(0.0, 0.0, 0.0, 1.0);
}
