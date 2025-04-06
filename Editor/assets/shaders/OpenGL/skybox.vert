#version 330 core

layout(location = 0) in vec3 a_Position;

out vec3 v_TexCoords;

uniform mat4 u_Projection;
uniform mat4 u_View;

void main()
{
    v_TexCoords = a_Position;

    // Note: Remove translation from the view matrix by using mat3(u_View)
    //vec4 pos = u_Projection * mat4(mat3(u_View)) * vec4(a_Position, 1.0);
    
    // macos
    vec4 pos = vec4(a_Position, 1.0) * mat4(mat3(u_View)) * u_Projection;
    
    // Ensure the skybox is always at the far plane (z/w = 1.0) to avoid z-fighting
    gl_Position = pos.xyww;
}