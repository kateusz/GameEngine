#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

const int MAX_POINT_LIGHTS = 16;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

uniform int   u_DirectionalLightEnabled;
uniform vec3  u_DirectionalLightDirection;
uniform vec3  u_DirectionalLightColor;
uniform float u_DirectionalLightStrength;

uniform int   u_AmbientLightEnabled;
uniform vec3  u_AmbientLightColor;
uniform float u_AmbientLightStrength;

uniform int   u_PointLightCount;
uniform vec3  u_PointLightPositions[MAX_POINT_LIGHTS];
uniform vec3  u_PointLightColors[MAX_POINT_LIGHTS];
uniform float u_PointLightIntensities[MAX_POINT_LIGHTS];

uniform vec3  u_ViewPosition;
uniform vec4  u_Color;
uniform float u_Shininess;
uniform int   u_HasDiffuseMap;
uniform int   u_HasSpecularMap;
uniform int   u_HasNormalMap;
uniform int   u_EntityID;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_SpecularMap;
uniform sampler2D u_NormalMap;

void main()
{
    vec3 norm;
    if (u_HasNormalMap != 0)
    {
        vec3 sampledNormal = texture(u_NormalMap, v_TexCoord).rgb;
        sampledNormal = sampledNormal * 2.0 - 1.0;
        norm = normalize(v_TBN * sampledNormal);
    }
    else
    {
        norm = normalize(v_Normal);
    }

    // sRGB -> linear for albedo (gamma encoded textures + sRGB vertex color)
    vec3 diffuseColor = u_HasDiffuseMap != 0
        ? pow(texture(u_DiffuseMap, v_TexCoord).rgb, vec3(2.2))
        : vec3(1.0);
    diffuseColor *= pow(u_Color.rgb, vec3(2.2));

    // Specular maps are typically authored as linear data (roughness/specular masks)
    vec3 specularColor = u_HasSpecularMap != 0
        ? texture(u_SpecularMap, v_TexCoord).rgb
        : vec3(0.5);

    // Light colors are authored in sRGB via color pickers; convert to linear for math.
    vec3 ambientLightLinear = pow(u_AmbientLightColor, vec3(2.2));
    vec3 dirLightLinear     = pow(u_DirectionalLightColor, vec3(2.2));

    vec3 ambient = vec3(0.0);
    if (u_AmbientLightEnabled != 0)
        ambient = ambientLightLinear * u_AmbientLightStrength * diffuseColor;

    vec3 diffuse = vec3(0.0);
    vec3 specular = vec3(0.0);
    vec3 viewDir = normalize(u_ViewPosition - v_FragPos);

    if (u_DirectionalLightEnabled != 0)
    {
        vec3 dirLightDir = vec3(0.0, 1.0, 0.0);
        if (length(u_DirectionalLightDirection) > 0.0001)
            dirLightDir = normalize(-u_DirectionalLightDirection);
        float dirDiff = max(dot(norm, dirLightDir), 0.0);
        // Blinn-Phong: half vector instead of reflect
        vec3 dirHalf = normalize(dirLightDir + viewDir);
        float dirSpec = pow(max(dot(norm, dirHalf), 0.0), u_Shininess);

        diffuse += dirDiff * dirLightLinear * diffuseColor * u_DirectionalLightStrength;
        specular += dirSpec * dirLightLinear * specularColor * u_DirectionalLightStrength;
    }

    int pointCount = min(u_PointLightCount, MAX_POINT_LIGHTS);
    for (int i = 0; i < pointCount; i++)
    {
        vec3 toLight = u_PointLightPositions[i] - v_FragPos;
        float dist = length(toLight);
        vec3 pointDir = toLight / max(dist, 0.0001);

        float pointDiff = max(dot(norm, pointDir), 0.0);
        // Blinn-Phong half vector
        vec3 pointHalf = normalize(pointDir + viewDir);
        float pointSpec = pow(max(dot(norm, pointHalf), 0.0), u_Shininess);

        // Physically-plausible inverse-square attenuation with small bias to avoid singularity.
        // Intensity acts as luminous power (light energy), distance falloff is 1/(d^2+1).
        float attenuation = 1.0 / (1.0 + dist * dist);

        vec3 pointLightLinear = pow(u_PointLightColors[i], vec3(2.2));
        vec3 pointColor = pointLightLinear * u_PointLightIntensities[i] * attenuation;
        diffuse += pointDiff * pointColor * diffuseColor;
        specular += pointSpec * pointColor * specularColor;
    }

    o_Color    = vec4(ambient + diffuse + specular, u_Color.a);
    o_EntityID = u_EntityID;
}
