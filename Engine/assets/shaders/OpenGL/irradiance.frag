#version 330 core

layout(location = 0) out vec4 o_Color;

in vec3 v_Direction;

uniform samplerCube environmentMap;

const float PI = 3.14159265359;

void main()
{
    vec3 normal = normalize(v_Direction);
    vec3 up = abs(normal.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
    vec3 right = normalize(cross(up, normal));
    up = normalize(cross(normal, right));

    vec3 irradiance = vec3(0.0);
    float nrSamples = 0.0;
    const float sampleDelta = 0.025;
    // texels must be wider than sampleDelta or a sun smaller than one step is hit or missed per texel (blotches);
    // lod 4 of the 512 environment is 32 per face, about 2.8 degrees
    const float c_SourceLod = 4.0;
    for (float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
    {
        for (float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
        {
            vec3 tangent = vec3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
            vec3 sampleVec = tangent.x * right + tangent.y * up + tangent.z * normal;
            irradiance += textureLod(environmentMap, sampleVec, c_SourceLod).rgb * cos(theta) * sin(theta);
            nrSamples++;
        }
    }

    irradiance = PI * irradiance * (1.0 / nrSamples);
    o_Color = vec4(irradiance, 1.0);
}
