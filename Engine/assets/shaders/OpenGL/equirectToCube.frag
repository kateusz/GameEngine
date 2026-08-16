#version 330 core

layout(location = 0) out vec4 o_Color;

in vec3 v_LocalPos;

uniform sampler2D u_EquirectMap;

const vec2 invAtan = vec2(0.1591, 0.3183);

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

void main()
{
    vec3 dir = normalize(v_LocalPos);
    o_Color = vec4(texture(u_EquirectMap, SampleSphericalMap(dir)).rgb, 1.0);
}
