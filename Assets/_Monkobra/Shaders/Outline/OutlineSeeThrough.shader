// misty halo round a fruit hidden by something nearer; the fruit's own
// shape is cut out by stencil, so only the glow shows through the trunk
Shader "Monkobra/Outline/OutlineSeeThrough"
{
    Properties
    {
        _Ghost_Color ("Halo Color", Color) = (1, 0.86, 0.3, 1)
        _Ghost_HaloAlpha ("Halo Peak Alpha", Range(0, 1)) = 0.55
        _Ghost_HaloSigma ("Halo Blur Sigma", Range(0.1, 1)) = 0.9
        _Ghost_HaloReach ("Halo Reach (world units)", Float) = 0.14
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        // pass 1: marks where the hidden fruit itself is, draws no color
        Pass
        {
            Name "GhostMask"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZTest Greater
            ZWrite Off
            Cull Back
            ColorMask 0

            Stencil
            {
                Ref 4
                ReadMask 4
                WriteMask 4
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // pass 2: the halo shell, skipped where pass 1 marked the fruit
        Pass
        {
            Name "GhostHalo"
            Tags { "LightMode" = "UniversalForward" }

            // hidden fruit only: passes where the depth buffer is nearer
            ZTest Greater
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 4
                ReadMask 4
                WriteMask 4
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Ghost_Color;
                half _Ghost_HaloAlpha;
                float _Ghost_HaloSigma;
                float _Ghost_HaloReach;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                // shell out by the halo reach, in world units
                positionWS += normalWS * _Ghost_HaloReach;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                // t: 0 next to the fruit, 1 at the shell's outer edge
                half t = 1.0 - saturate(dot(normalWS, viewDirWS));
                // Gaussian falloff reads as an out-of-focus glow; the smooth
                // fade ends it at 0 so the shell edge never shows
                half gauss = exp(-t * t / (2.0 * _Ghost_HaloSigma * _Ghost_HaloSigma));
                half alpha = _Ghost_HaloAlpha * gauss * smoothstep(1.0, 0.6, t);
                return half4(_Ghost_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
