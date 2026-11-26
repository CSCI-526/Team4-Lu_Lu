// inverted-hull outline, width in world units (object scale never changes it)
Shader "Monkobra/Outline/OutlineHull"
{
    Properties
    {
        // names kept for the boss golden fruit code
        _Outline_Color ("Outline Color", Color) = (1, 1, 1, 1)
        _Outline_Width ("Outline Width (world units)", Float) = 0.025
        // stencil bit of its family (8 fruit, 16 monkey): the hull hides
        // where OutlineMask marked that family's body, leaving only the
        // whole silhouette
        [HideInInspector] _Outline_StencilBit ("Stencil Bit", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OutlineHull"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            Stencil
            {
                Ref [_Outline_StencilBit]
                ReadMask [_Outline_StencilBit]
                WriteMask [_Outline_StencilBit]
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Outline_Color;
                float _Outline_Width;
                float _Outline_StencilBit;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // push along the world normal: scale of the object drops out
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                positionWS += normalWS * _Outline_Width;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(_Outline_Color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
