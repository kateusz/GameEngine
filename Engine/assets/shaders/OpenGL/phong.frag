#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int o_EntityID;

in vec3 v_Position;
in vec3 v_Normal;
in vec2 v_TexCoord;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform vec3 u_LightPosition;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Shininess;
uniform sampler2D u_Texture;
uniform int u_UseTexture;

void main()
{
    // Ambient lighting
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * u_LightColor;

    // Diffuse lighting
    vec3 norm = normalize(v_Normal);
    vec3 lightDir = normalize(u_LightPosition - v_Position);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * u_LightColor;

    // Specular lighting
    float specularStrength = 0.5;
    vec3 viewDir = normalize(u_ViewPosition - v_Position);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), u_Shininess);
    vec3 specular = specularStrength * spec * u_LightColor;

    // Base color
    vec4 baseColor = u_Color;
    if (u_UseTexture == 1)
    {
        baseColor = texture(u_Texture, v_TexCoord) * u_Color;
    }

    // Combined lighting
    vec3 result = (ambient + diffuse + specular) * baseColor.rgb;
    o_Color = vec4(result, baseColor.a);

    // Output entity ID
    o_EntityID = v_EntityID;
}