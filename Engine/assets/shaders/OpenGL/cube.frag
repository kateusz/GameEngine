#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Normal;
in vec2 v_TexCoord;
in vec4 v_FragPosLightSpace;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform sampler2D u_Texture;
uniform sampler2D u_ShadowMap;
uniform int u_UseTexture;
uniform int u_ShadowsEnabled;

float ShadowCalculation(vec3 normal, vec3 lightDir)
{
    vec3 projCoords = v_FragPosLightSpace.xyz / v_FragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    if (projCoords.z > 1.0)
        return 0.0;

    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(u_ShadowMap, 0);
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(u_ShadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += projCoords.z - bias > pcfDepth ? 1.0 : 0.0;
        }
    }
    return shadow / 9.0;
}

void main()
{
    vec4 baseColor = u_UseTexture == 1
        ? texture(u_Texture, v_TexCoord) * u_Color
        : u_Color;
    vec3 albedo = baseColor.rgb;
    vec3 ambient = u_AmbientStrength * u_AmbientColor;

    vec3 N = normalize(v_Normal);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    float shadow = u_ShadowsEnabled != 0 ? ShadowCalculation(N, L) : 0.0;
    vec3 diffuse = (1.0 - shadow) * ndotl * u_LightColor;

    o_Color = vec4((ambient + diffuse) * albedo, baseColor.a);
    o_EntityID = u_EntityID;
}
