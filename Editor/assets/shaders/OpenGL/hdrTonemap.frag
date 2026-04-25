#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_HdrColor;
uniform float u_Exposure;

// Narkowicz 2015 - ACES filmic curve fit. Cheap, good highlight rolloff,
// preserves saturation better than Reinhard.
vec3 acesFilmic(vec3 x)
{
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main()
{
    vec3 hdrColor = texture(u_HdrColor, v_TexCoord).rgb;
    vec3 exposed = hdrColor * u_Exposure;
    vec3 mapped = acesFilmic(exposed);
    // Linear -> sRGB encode for display
    mapped = pow(mapped, vec3(1.0 / 2.2));
    o_Color = vec4(mapped, 1.0);
}
