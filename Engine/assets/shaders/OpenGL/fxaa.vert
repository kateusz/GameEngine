#version 330 core

out vec2 v_TexCoord;

void main()
{
    float x = float((gl_VertexID & 1) << 2) - 1.0;
    float y = float((gl_VertexID & 2) << 1) - 1.0;
    v_TexCoord = vec2(x, y) * 0.5 + 0.5;
    gl_Position = vec4(x, y, 0.0, 1.0);
}
