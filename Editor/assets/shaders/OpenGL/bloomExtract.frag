#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_SceneTexture;
uniform float u_Threshold;
uniform float u_SoftKnee;

void main()
{
    vec3 sceneColor = texture(u_SceneTexture, v_TexCoord).rgb;
    float brightness = max(sceneColor.r, max(sceneColor.g, sceneColor.b));

    float knee = max(u_Threshold * u_SoftKnee, 0.0001);
    float soft = clamp((brightness - u_Threshold + knee) / (2.0 * knee), 0.0, 1.0);
    float contribution = max(brightness - u_Threshold, 0.0) + soft * soft * knee * 2.0;
    contribution /= max(brightness, 0.0001);

    o_Color = vec4(sceneColor * contribution, 1.0);
}
