#version 330 core

out vec4 FragColor;
in vec2 v_TexCoord;

uniform sampler2D u_Source;
uniform bool u_Horizontal;

void main()
{
    vec2 texelSize = 1.0 / vec2(textureSize(u_Source, 0));

    float weights[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

    vec3 result = texture(u_Source, v_TexCoord).rgb * weights[0];
    for (int i = 1; i < 5; i++)
    {
        vec2 offset = u_Horizontal
            ? vec2(texelSize.x * float(i), 0.0)
            : vec2(0.0, texelSize.y * float(i));

        result += texture(u_Source, v_TexCoord + offset).rgb * weights[i];
        result += texture(u_Source, v_TexCoord - offset).rgb * weights[i];
    }

    FragColor = vec4(result, 1.0);
}
