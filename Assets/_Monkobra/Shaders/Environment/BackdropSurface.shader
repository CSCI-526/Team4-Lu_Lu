// The jungle backdrop's real 3D trees: the ring of round-crowned trees round
// the trunk & the few giants standing out of the forest. Unlit and cheap:
// vertex colours (sRGB), a little light from the sun & the sky, darker down
// toward the forest floor, then the same haze as BackdropSky, so the near
// trees melt into the painted forest behind them.
Shader "Monkobra/BackdropSurface" {
    Properties {
        _BaseColor("Tint", Color) = (1,1,1,1)
        _ShadeByLight("Shade by sun", Range(0,1)) = 1
        _UnderDark("Darker toward the forest floor", Range(0,1)) = 0.6

        [Header(Painted canopy, same as the BackdropSky material)]
        [NoScaleOffset] _DetailMap("Crowns from above, tiled", 2D) = "grey" {}
        _DetailScale("Crown tile size (u)", Float) = 180
        [NoScaleOffset] _MacroMap("Forest tint (RGB, 0.5 neutral) + river (A)", 2D) = "grey" {}
        _MacroRadius("Macro map half width (u)", Float) = 2700
    }

    SubShader {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}

        Pass {
            Name "Backdrop"
            Tags {"LightMode"="UniversalForward"}
            // both sides, so a crown or trunk shows whichever way its
            // triangles wind; the outward normal shades either side
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BackdropCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _ShadeByLight, _UnderDark;
                float _DetailScale, _MacroRadius;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 color : TEXCOORD2;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = BackdropToWorkingSpace(input.color.rgb);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                half3 color = input.color * _BaseColor.rgb;

                // the sun brought lower, so even a high sun models the
                // crowns from the side, a little sky from above, and rims
                // darkening away from the eye, so crowns read round
                half3 normal = normalize(input.normalWS);
                half3 key = normalize(half3(_BackdropSunDir.x, _BackdropSunDir.y * 0.5, _BackdropSunDir.z));
                half sun = saturate(dot(normal, key));
                half3 toEye = normalize(_WorldSpaceCameraPos - input.positionWS);
                half rim = 1.0 - saturate(dot(normal, toEye));
                half light = (0.45 + 0.55 * sun + 0.1 * normal.y) * (1.0 - 0.3 * rim * rim);
                color *= lerp(1.0, light, _ShadeByLight);

                // down in the forest, darker
                float canopyY = _BackdropLayout.x;
                half depth = saturate((canopyY - input.positionWS.y) / max(canopyY - _BackdropLayout.z, 1.0));
                color *= 1.0 - depth * _UnderDark;

                // toward the ring's edge the trees take on the painted
                // canopy's colour, projected from above, as they sink into it
                float distanceU = distance(input.positionWS, _WorldSpaceCameraPos);
                float radius = length(input.positionWS.xz - _BackdropCentre.xz);
                half paint = smoothstep(_BackdropLayout.w, _BackdropLayout.y - 40.0, radius);
                if (paint > 0.0) {
                    half3 painted = BackdropCanopy(input.positionWS.xz, _DetailScale, _MacroRadius,
                        BackdropPixelAngle() * distanceU).rgb;
                    color = lerp(color, painted * lerp(0.8, 1.05, saturate(normal.y)), paint);
                }

                color = BackdropHaze(color, distanceU);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
