#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_Color;

void main()
{
    vec3 color = texture(u_Color, v_TexCoord).rgb;
    float brightness = dot(color, vec3(0.2126, 0.7152, 0.0722));
    o_Color = vec4(brightness > 1.0 ? color : vec3(0.0), 1.0);
}
