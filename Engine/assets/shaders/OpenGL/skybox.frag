#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec2 v_Clip;

uniform sampler2D u_EquirectMap;
uniform float u_Intensity;
uniform float u_Yaw;
uniform mat4 u_InverseViewProjection;

const vec2 invAtan = vec2(0.1591, 0.3183);

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(clamp(v.y, -1.0, 1.0)));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

void main()
{
    vec4 world = vec4(v_Clip, 1.0, 1.0) * u_InverseViewProjection;
    vec3 dir = normalize(world.xyz / max(abs(world.w), 1e-8));

    float c = cos(u_Yaw);
    float s = sin(u_Yaw);
    dir = vec3(c * dir.x + s * dir.z, dir.y, -s * dir.x + c * dir.z);

    o_Color = vec4(texture(u_EquirectMap, SampleSphericalMap(dir)).rgb * u_Intensity, 1.0);
    o_EntityID = -1;
}
