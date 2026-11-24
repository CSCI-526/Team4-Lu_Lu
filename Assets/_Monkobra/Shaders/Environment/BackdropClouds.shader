// The jungle backdrop's small puffy clouds, high above the forest: one quad
// each, turned to the camera round the world's up in the vertex shader, its
// puff drawn in the fragment shader from a few soft overlapping circles, so
// no texture. Fades in with _BackdropPuffs, out up close so a cloud never
// fills the view, and into the haze far off.
//
// Mesh: position the cloud's centre (all 4 corners alike), uv0 the corner
// (-1 to 1), uv1 x its width (u), y a seed.
Shader "Monkobra/BackdropClouds" {
    Properties {
        _CloudColor("Lit top", Color) = (1,1,1,1)
        _CloudShade("Shaded underside", Color) = (0.76,0.82,0.9,1)
        _NearFadeU("Fade out nearer than (u)", Float) = 140
        _Drift("Drift (u/s)", Float) = 1.5
    }

    SubShader {
        Tags {"RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10"}

        Pass {
            Name "Clouds"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BackdropCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CloudColor, _CloudShade;
                float _NearFadeU, _Drift;
            CBUFFER_END

            // set by JungleBackdrop: 0 to 1, how much the clouds show
            half _BackdropPuffs;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 corner : TEXCOORD0;
                float2 sizeSeed : TEXCOORD1;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                float seed : TEXCOORD1;
                float distanceU : TEXCOORD2;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                float3 centre = TransformObjectToWorld(input.positionOS.xyz);
                // drift slowly round the trunk, keeping their distance
                float2 fromAxis = centre.xz - _BackdropCentre.xz;
                float turn = _Time.y * _Drift / max(length(fromAxis), 1.0);
                float s = sin(turn);
                float c = cos(turn);
                centre.xz = _BackdropCentre.xz
                    + float2(fromAxis.x * c - fromAxis.y * s, fromAxis.x * s + fromAxis.y * c);
                float3 toEye = _WorldSpaceCameraPos - centre;
                toEye.y = 0;
                float3 forward = normalize(toEye + float3(1e-4, 0, 0));
                float3 right = normalize(cross(float3(0, 1, 0), forward));
                float width = input.sizeSeed.x;
                float3 world = centre
                    + right * input.corner.x * width * 0.5
                    + float3(0, 1, 0) * input.corner.y * width * 0.3;
                output.positionCS = TransformWorldToHClip(world);
                output.corner = input.corner;
                output.seed = input.sizeSeed.y;
                output.distanceU = distance(world, _WorldSpaceCameraPos);
                return output;
            }

            float Rand(float seed, float i) {
                return frac(sin(seed * 12.9898 + i * 78.233) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target {
                // quad space: x -1 to 1 across, y -1 to 1 up, 0.6 as tall
                float2 p = float2(input.corner.x, input.corner.y * 0.6);
                float puff = 0;
                [unroll]
                for (int i = 0; i < 5; i++) {
                    float x = lerp(-0.55, 0.55, i / 4.0) + (Rand(input.seed, i) - 0.5) * 0.2;
                    // middle puffs bigger & higher, a cumulus heap
                    float middle = 1.0 - abs(i - 2.0) / 2.0;
                    float radius = lerp(0.24, 0.42, middle) * lerp(0.85, 1.15, Rand(input.seed, i + 7.0));
                    float y = -0.12 + radius * 0.55;
                    float d = length(p - float2(x, y)) / radius;
                    puff = max(puff, 1.0 - d);
                }
                // flat underside
                puff *= smoothstep(-0.2, -0.1, p.y);
                half alpha = smoothstep(0.0, 0.12, puff);

                half3 color = lerp(_CloudShade.rgb, _CloudColor.rgb, saturate(p.y * 2.2 + 0.45));
                color = BackdropHaze(color, input.distanceU * 0.6);

                alpha *= _BackdropPuffs * smoothstep(_NearFadeU * 0.6, _NearFadeU, input.distanceU);
                return half4(color, alpha * 0.95);
            }
            ENDHLSL
        }
    }
}
