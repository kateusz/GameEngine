#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_HdrBuffer;
uniform sampler2D u_BloomBlur;
uniform float u_Exposure;
uniform float u_BloomIntensity;

vec3 AcesFitted(vec3 x)
{
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

vec3 LinearToSrgb(vec3 linear)
{
    return pow(linear, vec3(1.0 / 2.2));
}

void main()
{
    vec3 hdr = texture(u_HdrBuffer, v_TexCoord).rgb;
    hdr += texture(u_BloomBlur, v_TexCoord).rgb * u_BloomIntensity;
    vec3 exposed = hdr * u_Exposure;
    vec3 mapped = AcesFitted(exposed);
    o_Color = vec4(LinearToSrgb(mapped), 1.0);
}
