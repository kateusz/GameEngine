uniform mat4 u_LightViewProjection;
uniform mat4 u_FarLightViewProjection;
uniform sampler2DShadow u_ShadowMap;
uniform int u_ShadowsEnabled;
uniform int u_CascadeCount;
uniform float u_CascadeSplit;
uniform int u_ShadowPcf;
uniform float u_ShadowMapSize;
uniform int u_PointPcf;
uniform float u_PointMapSize;

const float c_ShadowBias = 0.002;
const float c_PointShadowBias = 0.05;

float SampleDirectional(vec2 uv, float current, int cascade)
{
    float tiles = float(max(u_CascadeCount, 1));
    vec2 atlas = uv * vec2(1.0 / tiles, 1.0) + vec2(float(cascade) / tiles, 0.0);
    return texture(u_ShadowMap, vec3(atlas, current - c_ShadowBias));
}

float PcfDirectional(vec2 uv, float current, int cascade)
{
    int radius = u_ShadowPcf >= 5 ? 2 : 1;
    vec2 texel = 1.0 / vec2(max(u_ShadowMapSize, 1.0));
    float sum = 0.0;
    float count = 0.0;
    for (int x = -2; x <= 2; ++x)
    {
        if (abs(x) > radius)
            continue;
        for (int y = -2; y <= 2; ++y)
        {
            if (abs(y) > radius)
                continue;
            sum += SampleDirectional(uv + vec2(x, y) * texel, current, cascade);
            count += 1.0;
        }
    }
    return sum / count;
}

float DirectionalShadow(vec3 fragPos)
{
    if (u_ShadowsEnabled == 0)
        return 1.0;

    int cascade = 0;
    if (u_CascadeCount > 1 && length(fragPos - u_ViewPosition) > u_CascadeSplit)
        cascade = 1;
    mat4 lightVp = cascade == 0 ? u_LightViewProjection : u_FarLightViewProjection;
    vec4 clipPos = vec4(fragPos, 1.0) * lightVp;
    vec3 ndc = clipPos.xyz / clipPos.w;
    vec2 uv = ndc.xy * 0.5 + 0.5;
    float current = ndc.z * 0.5 + 0.5;
    if (u_ShadowPcf <= 1)
        return SampleDirectional(uv, current, cascade);
    return PcfDirectional(uv, current, cascade);
}

float PointShadow(int i, vec3 fragPos)
{
    if (u_PointShadowsEnabled[i] == 0)
        return 1.0;

    vec3 toFrag = fragPos - u_PointLightPositions[i];
    float range = u_PointLightRanges[i];
    float current = length(toFrag) / range;
    float bias = c_PointShadowBias / range;
    if (u_PointPcf <= 1)
        return texture(u_PointShadowMaps[i], vec4(toFrag, current - bias));

    int radius = u_PointPcf >= 5 ? 2 : 1;
    float dist = max(length(toFrag), 0.0001);
    vec3 dir = toFrag / dist;
    vec3 helper = abs(dir.y) > 0.99 ? vec3(1.0, 0.0, 0.0) : vec3(0.0, 1.0, 0.0);
    vec3 tangent = normalize(cross(helper, dir));
    vec3 bitangent = cross(dir, tangent);
    float step = dist * 3.14159265 / max(u_PointMapSize, 1.0);
    float sum = 0.0;
    float count = 0.0;
    for (int x = -2; x <= 2; ++x)
    {
        if (abs(x) > radius)
            continue;
        for (int y = -2; y <= 2; ++y)
        {
            if (abs(y) > radius)
                continue;
            vec3 sampleDir = toFrag + (tangent * float(x) + bitangent * float(y)) * step;
            sum += texture(u_PointShadowMaps[i], vec4(sampleDir, current - bias));
            count += 1.0;
        }
    }
    return sum / count;
}
