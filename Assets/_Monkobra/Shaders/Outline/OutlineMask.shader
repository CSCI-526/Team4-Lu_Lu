// marks where a fruit's own body is, before any body or hull is drawn, so
// the hull shows only on the whole fruit's silhouette, never between parts
Shader "Monkobra/Outline/OutlineMask"
{
    Properties
    {
        // 8 = fruit, 16 = monkey; the matching hull skips what it marks
        [HideInInspector] _Outline_StencilBit ("Stencil Bit", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            // just ahead of every body and hull (Geometry = 2000)
            "Queue" = "Geometry-1"
        }

        Pass
        {
            Name "OutlineMask"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest Always
            Cull Back
            ColorMask 0

            // bit 8 fruit, bit 16 monkey; the ghost halo uses bit 4
            Stencil
            {
                Ref [_Outline_StencilBit]
                ReadMask [_Outline_StencilBit]
                WriteMask [_Outline_StencilBit]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Outline_StencilBit;
            CBUFFER_END

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
    }
}
