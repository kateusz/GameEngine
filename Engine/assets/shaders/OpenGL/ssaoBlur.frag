#version 330 core

out float FragColor;

in vec2 v_TexCoord;

uniform sampler2D u_Occlusion;

void main()
{
    vec2 texel = 1.0 / vec2(textureSize(u_Occlusion, 0));
    float result = 0.0;
    for (int x = -2; x < 2; ++x)
    {
        for (int y = -2; y < 2; ++y)
            result += texture(u_Occlusion, v_TexCoord + vec2(x, y) * texel).r;
    }

    FragColor = result / 16.0;
}
