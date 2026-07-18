#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Direction;

uniform sampler2D u_EquirectMap;
uniform float u_Exposure;
uniform int u_EntityID;

const vec2 invAtan = vec2(0.15915494309, 0.31830988618); // 1/(2π), 1/π

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(clamp(v.y, -1.0, 1.0)));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

void main()
{
    vec3 dir = normalize(v_Direction);
    vec2 uv = SampleSphericalMap(dir);
    vec3 hdr = texture(u_EquirectMap, uv).rgb;

    // ponytail: skybox-only Reinhard so HDR is visible before a real tone-map pass
    hdr *= u_Exposure;
    vec3 mapped = hdr / (hdr + vec3(1.0));

    o_Color = vec4(mapped, 1.0);
    o_EntityID = u_EntityID;
}
