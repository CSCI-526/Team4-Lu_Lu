// The jungle backdrop's birds: small dark silhouettes in a few flocks
// circling the tree at about the camera's height, flapping. Every bird is 2
// triangles whose flight & wing beat the vertex shader works out from
// _Time, so they cost nothing on the CPU. _BackdropBirds scales them from
// nothing, so they only show high up.
//
// Mesh: position the bird's shape (x across the wings, z along the body, 1
// u span), uv0 its path (radius u, start angle rad, height over the eye u,
// angular speed rad/s, signed for the way round), uv1 x its flap phase, y
// its span u.
Shader "Monkobra/BackdropBirds" {
    Properties {
        _BirdColor("Silhouette", Color) = (0.17,0.19,0.21,1)
        _FlapHz("Wing beats per second", Float) = 2.6
    }

    SubShader {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10"}

        Pass {
            Name "Birds"
            Tags {"LightMode"="UniversalForward"}
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BackdropCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BirdColor;
                float _FlapHz;
            CBUFFER_END

            // set by JungleBackdrop: 0 to 1, how much the birds show
            half _BackdropBirds;

            struct Attributes {
                float4 positionOS : POSITION;
                float4 path : TEXCOORD0;
                float2 flapSpan : TEXCOORD1;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float distanceU : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                float t = _Time.y;
                float angle = input.path.y + t * input.path.w;
                float radius = input.path.x + 40.0 * sin(t * 0.07 + input.path.y * 3.0);
                float3 centre = _BackdropCentre.xyz
                    + float3(cos(angle), 0, sin(angle)) * radius;
                centre.y = _WorldSpaceCameraPos.y + input.path.z
                    + 5.0 * sin(t * 0.3 + input.path.y * 5.0);

                float3 forward = float3(-sin(angle), 0, cos(angle)) * sign(input.path.w);
                float3 up = float3(0, 1, 0);
                float3 right = cross(up, forward);

                // the wing tips beat up & down, the body barely
                float3 shape = input.positionOS.xyz;
                float flap = sin(t * _FlapHz * TWO_PI + input.flapSpan.x);
                shape.y += abs(shape.x) * flap * 0.55;

                float span = input.flapSpan.y * _BackdropBirds;
                float3 world = centre + (right * shape.x + up * shape.y + forward * shape.z) * span;
                output.positionCS = TransformWorldToHClip(world);
                output.distanceU = distance(world, _WorldSpaceCameraPos);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                return half4(BackdropHaze(_BirdColor.rgb, input.distanceU), 1);
            }
            ENDHLSL
        }
    }
}
