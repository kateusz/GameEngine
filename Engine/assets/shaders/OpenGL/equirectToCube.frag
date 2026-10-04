#version 330 core

layout(location = 0) out vec4 o_Color;

in vec3 v_Direction;

uniform sampler2D u_Equirect;

const vec2 c_InvAtan = vec2(0.1591, 0.3183);

void main()
{
    vec3 d = normalize(v_Direction);
    float x = d.x == 0.0 ? 0.0 : d.x;
    float z = d.z == 0.0 ? 0.0 : d.z;
    vec2 uv = vec2(atan(z, x), asin(d.y)) * c_InvAtan + 0.5;
    o_Color = vec4(texture(u_Equirect, uv).rgb, 1.0);
}