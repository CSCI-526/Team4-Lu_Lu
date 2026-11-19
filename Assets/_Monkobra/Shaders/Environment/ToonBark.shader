// Stylized bark for the trunk and branches: soft 2 tone lighting, bark with
// broken grain strokes and a painterly mottle, moss patches with a soft dark
// rim and a few faint tuft marks, and a thin dark silhouette.
//
// Patterns live in "cells": round the surface the mesh UV x (0 to 1) is cut
// into _AroundCount cells, so they wrap without a seam; along it, cells are
// _CellSize long, measured in world height (_AlongFromWorldY 1, the trunk, so
// stacked segments join seamlessly) or in mesh UV y (0, a branch).
//
// The moss mask is 3D noise of the world position, the same function as
// StylizedBarkNoise.MossMask in C#, so decorations find the moss patches.
//
// With _VertexColors on (branch meshes, trunk decorations) vertex colours
// steer each part: R forces moss (hanging strands), G marks a leaf (UV x
// across it -1 to 1, UV y along it 0 to 1), B on a leaf shifts its tint and
// off a leaf turns bark into green vine (fruit stalks), A scales the
// silhouette (0 none).
Shader "Monkobra/ToonBark" {
    Properties {
        [Header(Bark)]
        _BarkColor("Bark", Color) = (0.42,0.36,0.33,1)
        _BarkPatchColor("Bark patches", Color) = (0.35,0.30,0.28,1)
        _GrainColor("Grain strokes", Color) = (0.20,0.17,0.17,1)
        _GrainPerCell("Grain strokes per cell", Float) = 3
        _GrainWidth("Grain stroke width", Range(0,0.5)) = 0.06
        _GrainStrength("Grain stroke strength", Range(0,1)) = 0.55

        [Header(Moss)]
        _MossColor("Moss", Color) = (0.38,0.60,0.50,1)
        _MossTuftColor("Moss clumps", Color) = (0.50,0.72,0.56,1)
        _MossInkColor("Moss rim & tuft marks", Color) = (0.17,0.30,0.26,1)
        _MossCoverage("Moss coverage", Range(0,1)) = 0.5
        _MossScale("Moss patch scale (1/u)", Float) = 0.28
        _MossStretch("Moss vertical stretch (lower hangs longer)", Range(0.1,1)) = 0.55
        _MossUp("Moss on top faces", Range(0,1)) = 0
        _ScribbleDensity("Tuft marks per cell", Range(0,1)) = 0.2
        _ScribbleStrength("Tuft mark strength", Range(0,1)) = 0.45

        [Header(Leaves And Vines)]
        _LeafColor("Leaf", Color) = (0.62,0.78,0.42,1)
        _LeafAltColor("Leaf tint variant", Color) = (0.42,0.66,0.55,1)
        _VineColor("Vine (fruit stalks)", Color) = (0.36,0.46,0.30,1)

        [Header(Pattern Cells)]
        _AroundCount("Cells round the surface (whole number)", Float) = 24
        _CellSize("Cell length along the surface (u)", Float) = 1.3
        [ToggleUI] _AlongFromWorldY("Along = world height", Float) = 1
        [ToggleUI] _VertexColors("Vertex colours steer parts", Float) = 0

        [Header(Light)]
        _ShadeTint("Shade tint", Color) = (0.62,0.68,0.82,1)
        _ShadeThreshold("Shade threshold", Range(0,1)) = 0.45
        _ShadeSoftness("Shade edge softness", Range(0.01,0.5)) = 0.14
        _DetailStrength("Painterly mottle", Range(0,0.4)) = 0.12

        [Header(Ink)]
        _InkColor("Patch rim & leaf edge", Color) = (0.16,0.26,0.22,1)
        _InkStrength("Patch rim strength", Range(0,1)) = 0.55
        _InkWidthPx("Patch rim width (px)", Range(0,6)) = 2.2

        [Header(Silhouette)]
        _OutlineColor("Outline", Color) = (0.14,0.13,0.12,1)
        _OutlineWidth("Outline width (u at 15 u away)", Float) = 0.025
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        half4 _BarkColor, _BarkPatchColor, _GrainColor;
        half _GrainPerCell, _GrainWidth, _GrainStrength;
        half4 _MossColor, _MossTuftColor, _MossInkColor;
        half _MossCoverage, _MossScale, _MossStretch, _MossUp;
        half _ScribbleDensity, _ScribbleStrength;
        half4 _LeafColor, _LeafAltColor, _VineColor;
        float _AroundCount, _CellSize, _AlongFromWorldY, _VertexColors;
        half4 _ShadeTint;
        half _ShadeThreshold, _ShadeSoftness, _DetailStrength;
        half4 _InkColor;
        half _InkStrength, _InkWidthPx;
        half4 _OutlineColor;
        float _OutlineWidth;
    CBUFFER_END

    // hash without sine (Dave Hoskins): stays stable at large world heights
    float Hash13(float3 p) {
        p = frac(p * 0.1031);
        p += dot(p, p.zyx + 31.32);
        return frac((p.x + p.y) * p.z);
    }

    float2 Hash22(float2 p) {
        float3 q = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
        q += dot(q, q.yzx + 33.33);
        return frac((q.xx + q.yz) * q.zy);
    }

    float ValueNoise3(float3 p) {
        float3 i = floor(p);
        float3 f = frac(p);
        float3 u = f * f * (3.0 - 2.0 * f);
        float n000 = Hash13(i);
        float n100 = Hash13(i + float3(1, 0, 0));
        float n010 = Hash13(i + float3(0, 1, 0));
        float n110 = Hash13(i + float3(1, 1, 0));
        float n001 = Hash13(i + float3(0, 0, 1));
        float n101 = Hash13(i + float3(1, 0, 1));
        float n011 = Hash13(i + float3(0, 1, 1));
        float n111 = Hash13(i + float3(1, 1, 1));
        return lerp(
            lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
            lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y),
            u.z);
    }

    // turns noise off the world axes, so patches never show straight edges
    static const float3x3 NOISE_TURN = float3x3(
        0.00, 0.80, 0.60,
        -0.80, 0.36, -0.48,
        -0.60, -0.48, 0.64);

    // 0 to 1, about 0.5 on average; mirrored by StylizedBarkNoise.MossMask
    float MossMask(float3 positionWS) {
        float3 p = mul(NOISE_TURN, positionWS * _MossScale * float3(1.0, _MossStretch, 1.0));
        float3 p2 = mul(NOISE_TURN, p) * 2.03 + 17.1;
        float3 p3 = mul(NOISE_TURN, p2) * 2.01 + 41.7;
        return ValueNoise3(p) * 0.6 + ValueNoise3(p2) * 0.28 + ValueNoise3(p3) * 0.12;
    }

    float MossThreshold() {
        return lerp(0.78, 0.22, _MossCoverage);
    }
    ENDHLSL

    SubShader {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}

        Pass {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = _VertexColors > 0.5 ? input.color : half4(0, 0, 0, 1);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            // a soft line where |distance| is under widthPx pixels; pixelSize
            // is how much distance one pixel spans, taken from a smooth
            // quantity, never from distance itself, which may jump
            half InkLine(float distance, float pixelSize, float widthPx) {
                float pixels = abs(distance) / max(pixelSize, 1e-5);
                return 1.0 - smoothstep(widthPx - 0.75, widthPx + 0.75, pixels);
            }

            // a faint "w" tuft mark in some cells, 1 on its line
            half Scribble(float2 cellPos, float cellPixel) {
                float2 cell = floor(cellPos);
                float2 local = frac(cellPos);
                float2 r = Hash22(cell + 7.3);
                float2 r2 = Hash22(cell + 91.7);
                float2 centre = 0.35 + 0.3 * r2;
                float len = 0.4 + 0.2 * r.y;
                float amp = 0.07 + 0.05 * r2.x;
                float rawT = (local.x - centre.x) / len + 0.5;
                float t = saturate(rawT);
                // |cos| bounces twice across the stroke: a "w"
                float curve = centre.y + amp * abs(cos(6.2831853 * t));
                float slope = amp * 6.2831853 * sin(6.2831853 * t) / len;
                float distance = (local.y - curve) / sqrt(1 + slope * slope);
                // taper the ends
                float width = 1.1 * smoothstep(0.0, 0.2, t) * smoothstep(1.0, 0.8, t);
                half inside = step(r.x, _ScribbleDensity) * step(0.0, rawT) * step(rawT, 1.0)
                    * step(0.05, width);
                return InkLine(distance, cellPixel, width) * inside;
            }

            half4 Frag(Varyings input) : SV_Target {
                half3 normal = normalize(input.normalWS);
                float along = _AlongFromWorldY > 0.5 ? input.positionWS.y : input.uv.y;
                float2 cellPos = float2(input.uv.x * _AroundCount, along / _CellSize);
                // cell units per pixel, from the smooth cell position
                float cellPixel = max(fwidth(cellPos.x), fwidth(cellPos.y));
                float3 turned = mul(NOISE_TURN, input.positionWS);

                half3 albedo;
                half leaf = input.color.g;

                if (leaf > 0.5) {
                    // Leaf  ---------------------------------------------------
                    half3 leafColor = lerp(_LeafColor.rgb, _LeafAltColor.rgb, input.color.b);
                    // brighter towards the tip, like painted leaves
                    leafColor *= lerp(0.82, 1.06, input.uv.y);
                    float across = fwidth(input.uv.x);
                    half edge = InkLine(1.0 - abs(input.uv.x), across, 1.0);
                    half rib = InkLine(input.uv.x, across, 0.6) * step(input.uv.y, 0.85);
                    albedo = lerp(leafColor, leafColor * 0.45, saturate(edge * 0.8 + rib * 0.35));
                } else {
                    // Bark  ---------------------------------------------------
                    float patches = ValueNoise3(turned * 0.9);
                    half3 bark = lerp(_BarkColor.rgb, _BarkPatchColor.rgb, smoothstep(0.4, 0.65, patches));
                    float grainX = cellPos.x * _GrainPerCell
                        + 0.7 * (ValueNoise3(float3(cellPos.x * 0.7, cellPos.y * 0.5, 3.7)) - 0.5);
                    // distance to the nearest stroke, in stroke spacings
                    float grainDistance = 0.5 - abs(frac(grainX) - 0.5);
                    float grainPixel = max(fwidth(grainX), 1e-4);
                    half grain = 1.0 - smoothstep(_GrainWidth - grainPixel, _GrainWidth + grainPixel, grainDistance);
                    // strokes break into dashes and fade in and out
                    grain *= smoothstep(0.38, 0.55, ValueNoise3(float3(cellPos.x * _GrainPerCell, cellPos.y * 1.6, 9.1)));
                    bark = lerp(bark, _GrainColor.rgb, grain * _GrainStrength);
                    // a fruit stalk is green vine, never mossy
                    half vine = input.color.b;
                    bark = lerp(bark, _VineColor.rgb * lerp(0.9, 1.1, patches), vine);

                    // Moss  ---------------------------------------------------
                    float mask = MossMask(input.positionWS) + _MossUp * (normal.y - 0.2) * 0.6;
                    float threshold = MossThreshold();
                    // coverage 0 means none at all, eg a vine material
                    float hasMoss = step(0.001, _MossCoverage) * (1 - vine);
                    float maskPixel = max(fwidth(mask), 1e-5);
                    float mossy = max(smoothstep(-maskPixel, maskPixel, mask - threshold) * hasMoss, input.color.r);

                    half3 moss = _MossColor.rgb;
                    float clumps = ValueNoise3(turned * 1.7 + 5.0);
                    moss = lerp(moss, _MossTuftColor.rgb, smoothstep(0.5, 0.8, clumps) * 0.65);
                    // the moss darkens towards its rim, like a thick mat
                    float depth = saturate((mask - threshold) / 0.08);
                    moss *= lerp(0.85, 1.0, depth);
                    half marks = Scribble(cellPos, cellPixel) * (1 - input.color.r) * _ScribbleStrength;
                    moss = lerp(moss, _MossInkColor.rgb, marks);

                    albedo = lerp(bark, moss, mossy);
                    // a soft dark rim where a patch meets the bark
                    half rim = InkLine(mask - threshold, maskPixel, _InkWidthPx)
                        * (1 - input.color.r) * hasMoss * _InkStrength;
                    albedo = lerp(albedo, _InkColor.rgb, rim);
                }

                // painterly mottle, so no area reads as one flat fill
                half mottle = ValueNoise3(turned * 2.6 + 11.0) * 0.65 + ValueNoise3(turned * 7.1) * 0.35;
                albedo *= 1.0 + (mottle - 0.5) * 2.0 * _DetailStrength;

                // Light  ------------------------------------------------------
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                half lambert = dot(normal, light.direction) * 0.5 + 0.5;
                half lit = lambert * lerp(0.35, 1.0, light.shadowAttenuation);
                half band = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, lit);
                half3 color = lerp(albedo * _ShadeTint.rgb, albedo, band);
                // a little of the real gradient inside each tone
                color *= lerp(0.9, 1.05, lambert);
                color *= lerp(half3(1, 1, 1), light.color, 0.25);
                color += SampleSH(normal) * albedo * 0.15;

                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }

        // dark silhouette: back faces pushed out along their normals
        Pass {
            Name "Outline"
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                half fog : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                // a far line stays thick enough to read, a near one never fat
                float distanceScale = clamp(distance(positionWS, _WorldSpaceCameraPos) / 15.0, 0.6, 3.0);
                float partScale = _VertexColors > 0.5 ? input.color.a : 1.0;
                positionWS += normalWS * _OutlineWidth * distanceScale * partScale;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                return half4(MixFog(_OutlineColor.rgb, input.fog), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
