#ifndef DEMONFIGHTER_DEMON_SKIN_FORWARD_PASS_INCLUDED
#define DEMONFIGHTER_DEMON_SKIN_FORWARD_PASS_INCLUDED

// The URP Lit forward pass supplies the vertex program, the varyings and the lighting setup; only the fragment
// program is ours, so lighting keeps tracking the pipeline version for free.
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

// Per-renderer gore state, set through a MaterialPropertyBlock by BodyPartView. Deliberately no material
// properties: the material stays SRP-batcher compatible and a renderer without a block draws like URP Lit.
float _BloodAmount;
float4 _WoundCenter;
float _WoundRadius;

// Junction flesh (D-097), also per renderer: where a part meets the body, the core, the collar and the root of the
// part wear one procedural flesh in body space (rig units), colored like the average of the core's texture, so the
// joint shows no seam between two textures. _FleshFromObject takes object space to body space; _FleshAll makes the
// whole surface flesh (collars); _FleshRoot (xyz centre, w radius) and _FleshRootFade mark the root of a part;
// _FleshJunctions mark the joints on the core.
TEXTURE2D(_FleshSource);
SAMPLER(sampler_FleshSource);
float4 _FleshTint;
float4x4 _FleshFromObject;
float _FleshAll;
float4 _FleshRoot;
float _FleshRootFade;
float4 _FleshJunctions[8];
float _FleshJunctionCount;
float _FleshJunctionFade;

static const half3 BloodColor = half3(0.30, 0.012, 0.008);
static const half BloodSmoothness = 0.85;
static const float GoreNoiseScale = 9.0;
static const float StreakStretch = 0.35;

float GoreHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Value noise in [0, 1]: trilinear blend of the hashes of the eight cell corners.
float GoreNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = GoreHash(i);
    float n100 = GoreHash(i + float3(1, 0, 0));
    float n010 = GoreHash(i + float3(0, 1, 0));
    float n110 = GoreHash(i + float3(1, 1, 0));
    float n001 = GoreHash(i + float3(0, 0, 1));
    float n101 = GoreHash(i + float3(1, 0, 1));
    float n011 = GoreHash(i + float3(0, 1, 1));
    float n111 = GoreHash(i + float3(1, 1, 1));
    float x00 = lerp(n000, n100, f.x);
    float x10 = lerp(n010, n110, f.x);
    float x01 = lerp(n001, n101, f.x);
    float x11 = lerp(n011, n111, f.x);
    float y0 = lerp(x00, x10, f.y);
    float y1 = lerp(x01, x11, f.y);
    return lerp(y0, y1, f.z);
}

// How bloody this point of the surface is: strongest around the last wound, streaked downward over the body,
// everywhere a little once the part is soaked. Object space, so the pattern sticks to the part as it moves.
half GoreMask(float3 positionWS)
{
    float3 positionOS = TransformWorldToObject(positionWS);
    float wound = saturate(1.0 - distance(positionOS, _WoundCenter.xyz) / max(_WoundRadius, 0.001));
    float3 p = positionOS * GoreNoiseScale;
    float streaks = GoreNoise(float3(p.x, p.y * StreakStretch, p.z));
    float spots = GoreNoise(p * 2.3 + 7.1);
    return saturate(_BloodAmount * (0.3 + 0.7 * wound) * (0.35 + 0.9 * streaks * spots) * 2.0);
}

// How much of this point is junction flesh, 0 to 1, with a ragged edge so the fade reads as grown, not painted.
half FleshWeight(float3 positionBS)
{
    float weight = _FleshAll;
    if (_FleshRoot.w > 0.0)
    {
        weight = max(weight, 1.0 - saturate((distance(positionBS, _FleshRoot.xyz) - _FleshRoot.w) / max(_FleshRootFade, 0.0001)));
    }

    int count = (int)_FleshJunctionCount;
    for (int i = 0; i < count && i < 8; i++)
    {
        float4 junction = _FleshJunctions[i];
        weight = max(weight, 1.0 - saturate((distance(positionBS, junction.xyz) - junction.w) / max(_FleshJunctionFade, 0.0001)));
    }

    return saturate(weight * 1.6 - 0.6 * GoreNoise(positionBS * 31.0));
}

// Mottled flesh around the average color of the core's texture (its last mip), with dark veins.
half3 FleshAlbedo(float3 positionBS)
{
    half3 average = SAMPLE_TEXTURE2D_LOD(_FleshSource, sampler_FleshSource, float2(0.5, 0.5), 16.0).rgb * _FleshTint.rgb;
    float mottling = GoreNoise(positionBS * 24.0) * 0.6 + GoreNoise(positionBS * 61.0 + 4.7) * 0.4;
    float vein = 1.0 - smoothstep(0.02, 0.07, abs(GoreNoise(positionBS * 11.0 + 9.3) - 0.5));
    return average * (0.72 + 0.56 * mottling) * (1.0 - 0.45 * vein);
}

void DemonSkinFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    if (_FleshAll > 0.5 || _FleshRoot.w > 0.0 || _FleshJunctionCount > 0.5)
    {
        float3 positionBS = mul(_FleshFromObject, float4(TransformWorldToObject(input.positionWS), 1.0)).xyz;
        half flesh = FleshWeight(positionBS);
        if (flesh > 0.0)
        {
            surfaceData.albedo = lerp(surfaceData.albedo, FleshAlbedo(positionBS), flesh);
            surfaceData.normalTS = normalize(lerp(surfaceData.normalTS, half3(0.0, 0.0, 1.0), flesh));
            surfaceData.smoothness = lerp(surfaceData.smoothness, half(0.42), flesh);
            surfaceData.metallic = lerp(surfaceData.metallic, half(0.0), flesh);
            surfaceData.occlusion = lerp(surfaceData.occlusion, half(1.0), flesh);
        }
    }

    if (_BloodAmount > 0.0)
    {
        half mask = GoreMask(input.positionWS);
        surfaceData.albedo = lerp(surfaceData.albedo, BloodColor, mask);
        surfaceData.smoothness = lerp(surfaceData.smoothness, BloodSmoothness, mask);
        surfaceData.metallic = lerp(surfaceData.metallic, half(0.0), mask);
    }

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    InitializeBakedGIData(input, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
