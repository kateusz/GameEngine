// Included by lit fragment shaders. Requires: uniform samplerCube u_PointShadowMap;

const vec3 sampleOffsetDirections[20] = vec3[]
(
    vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1),
    vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
    vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
    vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
    vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
);

float PointShadowPCF(vec3 fragPos, vec3 normal, vec3 lightPos, float range)
{
    vec3 fragToLight = fragPos - lightPos;
    float currentDepth = length(fragToLight);
    if (currentDepth > range)
        return 0.0;

    vec3 L = normalize(lightPos - fragPos);
    float ndotl = max(dot(normal, L), 0.0);
    float rangeScale = range / 25.0;
    float bias = max(0.05 * (1.0 - ndotl), 0.005) * rangeScale;

    vec3 dir = fragToLight / currentDepth;
    vec3 right = normalize(cross(dir, abs(dir.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0)));
    vec3 up = cross(right, dir);

    float diskRadius = 0.05 * (1.0 + currentDepth / max(range, 1e-4)) * rangeScale;
    float shadow = 0.0;
    for (int i = 0; i < 20; ++i)
    {
        vec2 o = sampleOffsetDirections[i].xy;
        vec3 sampleDir = dir + (right * o.x + up * o.y) * diskRadius;
        float closestDepth = texture(u_PointShadowMap, sampleDir).r * range;
        shadow += currentDepth - bias > closestDepth ? 1.0 : 0.0;
    }
    return shadow / 20.0;
}
