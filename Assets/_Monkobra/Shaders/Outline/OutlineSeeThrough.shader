// ghost shell: drawn only where something nearer covers the fruit
Shader "Monkobra/Outline/OutlineSeeThrough"
{
    Properties
    {
        _Ghost_Color ("Ghost Color", Color) = (1, 0.86, 0.3, 1)
        _Ghost_FillAlpha ("Fill Alpha", Range(0, 1)) = 0.3
        _Ghost_RimAlpha ("Rim Alpha", Range(0, 1)) = 0.9
        _Ghost_RimPower ("Rim Power", Float) = 3
        _Ghost_Push ("Shell Push (world units)", Float) = 0.008
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "SeeThroughGhost"
            Tags { "LightMode" = "UniversalForward" }

            // hidden fruit only: passes where the depth buffer is nearer
            ZTest Greater
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Ghost_Color;
                half _Ghost_FillAlpha;
                half _Ghost_RimAlpha;
                float _Ghost_RimPower;
                float _Ghost_Push;
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
                // shell out by a world-unit amount so the rim hugs the edge
                positionWS += normalWS * _Ghost_Push;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                // steady Fresnel edge, no time term
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _Ghost_RimPower);
                half alpha = lerp(_Ghost_FillAlpha, _Ghost_RimAlpha, fresnel);
                return half4(_Ghost_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
