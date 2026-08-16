#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_Image;
uniform int u_Horizontal;

// LearnOpenGL 5-tap Gaussian weights. const (not uniform) so GLSL 330 compiles on all drivers.
const float Weight[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

void main()
{
    vec2 texel = 1.0 / vec2(textureSize(u_Image, 0));
    vec3 result = texture(u_Image, v_TexCoord).rgb * Weight[0];
    if (u_Horizontal == 1)
    {
        for (int i = 1; i < 5; ++i)
        {
            result += texture(u_Image, v_TexCoord + vec2(texel.x * float(i), 0.0)).rgb * Weight[i];
            result += texture(u_Image, v_TexCoord - vec2(texel.x * float(i), 0.0)).rgb * Weight[i];
        }
    }
    else
    {
        for (int i = 1; i < 5; ++i)
        {
            result += texture(u_Image, v_TexCoord + vec2(0.0, texel.y * float(i))).rgb * Weight[i];
            result += texture(u_Image, v_TexCoord - vec2(0.0, texel.y * float(i))).rgb * Weight[i];
        }
    }
    o_Color = vec4(result, 1.0);
}
