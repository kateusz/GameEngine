#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Shininess;
uniform int u_HasDiffuseMap;
uniform int u_HasSpecularMap;
uniform int u_HasNormalMap;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_SpecularMap;
uniform sampler2D u_NormalMap;

const int c_MaxPointLights = 8;
const float c_PointEpsilon = 0.0001;

uniform int u_PointLightCount;
uniform vec3 u_PointLightPositions[c_MaxPointLights];
uniform vec3 u_PointLightColors[c_MaxPointLights];
uniform float u_PointLightIntensities[c_MaxPointLights];
uniform float u_PointLightRanges[c_MaxPointLights];

uniform mat4 u_LightViewProjection;
uniform sampler2D u_ShadowMap;
uniform int u_ShadowsEnabled;

const float c_ShadowBias = 0.002;

float DirectionalShadow(vec3 fragPos)
{
    if (u_ShadowsEnabled == 0)
        return 1.0;

    vec4 clipPos = vec4(fragPos, 1.0) * u_LightViewProjection;
    vec3 ndc = clipPos.xyz / clipPos.w;
    vec2 uv = ndc.xy * 0.5 + 0.5;
    float current = ndc.z * 0.5 + 0.5;
    float shadow = 0.0;
    vec2 texel = vec2(1.0 / 1024.0);
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float closest = texture(u_ShadowMap, uv + vec2(x, y) * texel).r;
            shadow += current - c_ShadowBias > closest ? 0.0 : 1.0;
        }
    }
    return shadow / 9.0;
}

vec3 PointLights(vec3 N, vec3 fragPos, vec3 V, vec3 albedo, vec3 specColor, float shininess)
{
    vec3 sum = vec3(0.0);
    for (int i = 0; i < c_MaxPointLights; i++)
    {
        if (i >= u_PointLightCount)
        break;

        vec3 toLight = u_PointLightPositions[i] - fragPos;
        float dist = length(toLight);
        if (dist >= u_PointLightRanges[i])
        continue;

        vec3 radiance = u_PointLightColors[i] * u_PointLightIntensities[i];
        if (dist < c_PointEpsilon)
        {
            sum += radiance * albedo;
            continue;
        }

        float remaining = 1.0 - dist / u_PointLightRanges[i];
        float attenuation = remaining * remaining;
        vec3 L = toLight / dist;
        float ndotl = max(dot(N, L), 0.0);
        vec3 diffuse = ndotl * radiance * albedo;

        vec3 H = normalize(L + V);
        float spec = pow(max(dot(N, H), 0.0), shininess);
        vec3 specular = spec * radiance * specColor;
        sum += (diffuse + specular) * attenuation;
    }
    return sum;
}

void main()
{
    vec3 norm;
    if (u_HasNormalMap != 0)
    {
        vec3 sampledNormal = texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0;
        norm = normalize(v_TBN * sampledNormal);
    }
    else
    {
        norm = normalize(v_Normal);
    }

    vec3 diffuseColor = u_HasDiffuseMap != 0
    ? texture(u_DiffuseMap, v_TexCoord).rgb
    : vec3(1.0);
    diffuseColor *= u_Color.rgb;

    vec3 specularColor = u_HasSpecularMap != 0
    ? texture(u_SpecularMap, v_TexCoord).rgb
    : vec3(0.5);

    vec3 ambient = u_AmbientStrength * u_AmbientColor * diffuseColor;

    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(norm, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor * diffuseColor;

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 H = normalize(L + V);
    float spec = pow(max(dot(norm, H), 0.0), u_Shininess);
    vec3 specular = spec * u_LightColor * specularColor;
    vec3 points = PointLights(norm, v_FragPos, V, diffuseColor, specularColor, u_Shininess);
    
    float shadow = DirectionalShadow(v_FragPos);
    o_Color = vec4(ambient + (diffuse + specular) * shadow + points, u_Color.a);
    o_EntityID = u_EntityID;
}
