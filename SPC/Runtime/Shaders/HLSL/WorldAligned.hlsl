#ifndef WORLD_ALIGNED_INCLUDED
#define WORLD_ALIGNED_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#endif

float3 TriplanarBlendCoefficients(float3 normalWS, float blendPower)
{
    float3 blend = max(pow(abs(normalWS), blendPower), 1e-8);
    return blend / dot(blend, 1.0);
}

float3 TransformTriplanarNormalUnity(
    float3 tangentNormal,
    float3 normalWS,
    float selection,
    float blendSign)
{
    // tangentNormal.xy *= blendSign;
    // We ignore the blend sign since unity also doesn't seem to use it
    uint index = (uint)selection;

    float3 swizzledNormals;

    if (index == 0)
    {
        swizzledNormals =
            float3(
                tangentNormal.xy + normalWS.zy,
                tangentNormal.z * normalWS.x
            ).zyx;
    }
    else if (index == 1)
    {
        swizzledNormals =
            float3(
                tangentNormal.xy + normalWS.xz,
                tangentNormal.z * normalWS.y
            ).xzy;
    }
    else
    {
        swizzledNormals =
            float3(
                tangentNormal.xy + normalWS.xy,
                tangentNormal.z * normalWS.z
            );
    }

    return SafeNormalize(swizzledNormals);
}

void BiplanarUV(
    float3 p,
    float3 n,
    float k,
    out float2 uv1,
    out float2 uv2,
    out float2 w)
{
    n = abs(n);

    int3 ma = (n.x > n.y && n.x > n.z) ? int3(0, 1, 2) : (n.y > n.z) ? int3(1, 2, 0) : int3(2, 0, 1);
    int3 mi = (n.x < n.y && n.x < n.z) ? int3(0, 1, 2) : (n.y < n.z) ? int3(1, 2, 0) : int3(2, 0, 1);
    int3 me = int3(3, 3, 3) - mi - ma;

    uv1 = float2(p[ma.y], p[ma.z]);
    uv2 = float2(p[me.y], p[me.z]);

    w = float2(n[ma.x], n[me.x]);
    w = clamp((w - 0.5773) / (1.0 - 0.5773), 0.0, 1.0);
    w = pow(w, float2(k / 8.0, k / 8.0));
}

void DeriveTriplanarCoordinates_float(
    float3 Position, float3 Normal, float3 Tiling, float Sharpness,
    out float2 UV_X, out float2 UV_Y, out float2 UV_Z,
    out float4 DD_UV_X, out float4 DD_UV_Y, out float4 DD_UV_Z,
    out float3 BlendWeights, out float3 BlendSigns)
{
    float3 pos = Position;
    pos *= Tiling;

    BlendWeights = TriplanarBlendCoefficients(Normal, Sharpness);
    BlendSigns = sign(Normal);

    UV_X = pos.zy;
    UV_Y = pos.xz;
    UV_Z = pos.xy;

    float3 ddxPos = ddx(pos);
    float3 ddyPos = ddy(pos);
    DD_UV_X = float4(ddxPos.zy, ddyPos.zy);
    DD_UV_Y = float4(ddxPos.xz, ddyPos.xz);
    DD_UV_Z = float4(ddxPos.xy, ddyPos.xy);
}

void TriplanarSelectDominantAxis_float(
    float3 BlendWeights,
    float3 BlendSigns,
    float2 UV_X, float2 UV_Y, float2 UV_Z,
    float4 DD_UV_X, float4 DD_UV_Y, float4 DD_UV_Z,
    float Dither,
    out float Axis,
    out float Sign,
    out float2 UV,
    out float2 DDX,
    out float2 DDY
)
{
    float3 noise3;
    noise3.x = Dither;
    noise3.y = frac(Dither * 2.11377);
    noise3.z = frac(Dither * 3.57143);
    float3 noisyWeights = saturate(BlendWeights) + (noise3 - 0.5) * 1;

    uint i = 0;
    float maxWeight = noisyWeights.x;
    if (noisyWeights.y > maxWeight)
    {
        i = 1;
        maxWeight = noisyWeights.y;
    }
    if (noisyWeights.z > maxWeight)
    {
        i = 2;
    }

    Sign = BlendSigns[i];
    Axis = i;

    float2 uvChoices[3] = {UV_X, UV_Y, UV_Z};
    float4 derivativeChoices[3] = {DD_UV_X, DD_UV_Y, DD_UV_Z};

    float4 ddPacked = derivativeChoices[i];
    DDX = ddPacked.xy;
    DDY = ddPacked.zw;
    UV = uvChoices[i];
}

void TriplanarSampleAxis_float(
    UnityTexture2D Texture,
    UnitySamplerState Sampler,
    float2 UV,
    float2 DDX,
    float2 DDY,
    out float4 Color)
{
    Color = SAMPLE_TEXTURE2D_GRAD(
        Texture,
        Sampler,
        UV,
        DDX,
        DDY
    );
}

void TriplanarTransformAxisNormal_float(
    float3 NormalWS,
    float Axis,
    float BlendSign,
    float3 NormalTS,
    out float3 Normal)
{
    Normal = TransformTriplanarNormalUnity(NormalTS, NormalWS, Axis, BlendSign);
}

void InterleavedGradientNoise_float(float2 Pixel, float Time, out float Noise)
{
    int2 iP = (int2)trunc(Pixel);

    Pixel = float2(iP) + 5.588238 * Time;

    Noise = frac(
        52.9829189 *
        frac(0.06711056 * Pixel.x + 0.00583715 * Pixel.y)
    );
}
#endif
