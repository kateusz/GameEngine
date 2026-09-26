#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform sampler2D u_Texture;
uniform int u_UseTexture;

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

    // ponytail: bilinear PCF; integer-offset PCF is constant per texel → stairs
    vec2 mapSize = vec2(textureSize(u_ShadowMap, 0));
    vec2 texelSize = 1.0 / mapSize;
    vec2 texelUv = uv * mapSize - 0.5;
    vec2 f = fract(texelUv);
    vec2 origin = (floor(texelUv) + 0.5) * texelSize;

    float shadow = 0.0;
    for (int x = 0; x <= 1; ++x)
    {
        for (int y = 0; y <= 1; ++y)
        {
            float closest = texture(u_ShadowMap, origin + vec2(x, y) * texelSize).r;
            float lit = current - c_ShadowBias > closest ? 0.0 : 1.0;
            float wx = x == 0 ? 1.0 - f.x : f.x;
            float wy = y == 0 ? 1.0 - f.y : f.y;
            shadow += lit * wx * wy;
        }
    }
    return shadow;
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
    vec4 baseColor = u_UseTexture == 1
        ? texture(u_Texture, v_TexCoord) * u_Color
        : u_Color;
    vec3 albedo = baseColor.rgb;
    vec3 ambient = u_AmbientStrength * u_AmbientColor;

    vec3 N = normalize(v_Normal);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor;

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 points = PointLights(N, v_FragPos, V, albedo, vec3(0.5), 32.0);
    float shadow = DirectionalShadow(v_FragPos);
    o_Color = vec4((ambient + diffuse * shadow) * albedo + points, baseColor.a);
    o_EntityID = u_EntityID;
}
