#version 330 core

out vec2 v_Clip;

void main()
{
    // Same fullscreen triangle as hdrTonemap.vert — clip z=0 so near/far never clip it.
    float x = float((gl_VertexID & 1) << 2) - 1.0;
    float y = float((gl_VertexID & 2) << 1) - 1.0;
    v_Clip = vec2(x, y);
    gl_Position = vec4(x, y, 0.0, 1.0);
}
