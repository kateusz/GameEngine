#version 330 core

out vec4 outputColor;

in vec2 v_TexCoord;

// A sampler2d is the representation of a texture in a shader.
// Each sampler is bound to a texture unit (texture units are described in Texture.cs on the Use function).
// By default, the unit is 0, so no code-related setup is actually needed.
// Multiple samplers will be demonstrated in section 1.5.
uniform sampler2D u_Texture;
uniform vec4 u_Color;
uniform float u_TilingFactor;

void main()
{
    // To use a texture, you call the texture() function.
    // It takes two parameters: the sampler to use, and a vec2, used as texture coordinates.
    outputColor = texture(u_Texture, v_TexCoord * u_TilingFactor) * u_Color;
}