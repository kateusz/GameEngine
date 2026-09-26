#version 330 core

in vec2 v_TexCoord;
layout(location = 0) out vec4 o_Color;

uniform sampler2D u_Texture;
uniform float u_InverseWidth;
uniform float u_InverseHeight;

// NVIDIA FXAA 3.11 quality (Timothy Lottes, public domain).
#define FXAA_REDUCE_MIN (1.0 / 128.0)
#define FXAA_REDUCE_MUL (1.0 / 8.0)
#define FXAA_SPAN_MAX 8.0

void main()
{
    vec2 texel = vec2(u_InverseWidth, u_InverseHeight);

    vec3 rgbNW = texture(u_Texture, v_TexCoord + vec2(-1.0, -1.0) * texel).rgb;
    vec3 rgbNE = texture(u_Texture, v_TexCoord + vec2(1.0, -1.0) * texel).rgb;
    vec3 rgbSW = texture(u_Texture, v_TexCoord + vec2(-1.0, 1.0) * texel).rgb;
    vec3 rgbSE = texture(u_Texture, v_TexCoord + vec2(1.0, 1.0) * texel).rgb;
    vec3 rgbM  = texture(u_Texture, v_TexCoord).rgb;

    const vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM, luma);

    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = clamp(dir * rcpDirMin, vec2(-FXAA_SPAN_MAX), vec2(FXAA_SPAN_MAX)) * texel;

    vec3 rgbA = 0.5 * (
        texture(u_Texture, v_TexCoord + dir * (1.0 / 3.0 - 0.5)).rgb +
        texture(u_Texture, v_TexCoord + dir * (2.0 / 3.0 - 0.5)).rgb);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (
        texture(u_Texture, v_TexCoord + dir * -0.5).rgb +
        texture(u_Texture, v_TexCoord + dir * 0.5).rgb);

    float lumaB = dot(rgbB, luma);
    o_Color = vec4((lumaB < lumaMin || lumaB > lumaMax) ? rgbA : rgbB, 1.0);
}
