#version 330 core

out vec4 FragColor;

in vec2 v_TexCoord;

uniform sampler2D u_SceneTexture;
uniform sampler2D u_BloomTexture;
uniform float u_BloomIntensity;
uniform float u_Exposure;
uniform float u_Gamma;

void main()
{
    vec3 sceneColor = texture(u_SceneTexture, v_TexCoord).rgb;
    vec3 bloomColor = texture(u_BloomTexture, v_TexCoord).rgb;
    vec3 hdrColor = sceneColor + bloomColor * u_BloomIntensity;

    vec3 mapped = vec3(1.0) - exp(-hdrColor * u_Exposure);
    mapped = pow(mapped, vec3(1.0 / max(u_Gamma, 0.0001)));

    FragColor = vec4(mapped, 1.0);
}
