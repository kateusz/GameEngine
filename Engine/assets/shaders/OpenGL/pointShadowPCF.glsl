// Included by lit fragment shaders. Requires: uniform samplerCube u_PointShadowMap;

const vec3 kPoisson16[16] = vec3[]
(
    vec3(-0.942, -0.399, 0.0), vec3( 0.945, -0.768, 0.0),
    vec3(-0.094, -0.929, 0.0), vec3( 0.345,  0.294, 0.0),
    vec3(-0.915,  0.457, 0.0), vec3(-0.815, -0.879, 0.0),
    vec3(-0.382,  0.276, 0.0), vec3( 0.975,  0.217, 0.0),
    vec3(-0.444,  0.871, 0.0), vec3( 0.251,  0.743, 0.0),
    vec3( 0.440, -0.556, 0.0), vec3( 0.695,  0.637, 0.0),
    vec3(-0.665,  0.752, 0.0), vec3( 0.108, -0.327, 0.0),
    vec3( 0.647, -0.051, 0.0), vec3(-0.178,  0.534, 0.0)
);

float PointShadowPCF(vec3 fragPos, vec3 normal, vec3 lightPos, float range)
{
    const float pcfRadius = 0.04;

    vec3 toFrag = fragPos - lightPos;
    float currentDepth = length(toFrag);
    if (currentDepth < 1e-4 || currentDepth > range)
        return 0.0;

    int mapSize = textureSize(u_PointShadowMap, 0).x;
    float texel = currentDepth / max(float(mapSize), 1.0);
    vec3 L = toFrag / currentDepth;
    float ndotl = max(dot(normal, -L), 0.0);
    float bias = max(0.6 * texel * (1.0 - ndotl), 0.2 * texel);

    vec3 samplePos = fragPos + normal * (2.0 * texel);
    vec3 sampleToFrag = samplePos - lightPos;
    float sampleDepth = length(sampleToFrag);
    vec3 dir = sampleToFrag / max(sampleDepth, 1e-4);

    vec3 right = normalize(cross(dir, abs(dir.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0)));
    vec3 up = cross(right, dir);
    float angularR = pcfRadius / max(sampleDepth, 1e-4);

    float shadow = 0.0;
    for (int i = 0; i < 16; ++i)
    {
        vec2 o = kPoisson16[i].xy;
        vec3 sdir = dir + (right * o.x + up * o.y) * angularR;
        float closest = texture(u_PointShadowMap, sdir).r * range;
        shadow += sampleDepth - bias > closest ? 1.0 : 0.0;
    }
    return shadow / 16.0;
}
