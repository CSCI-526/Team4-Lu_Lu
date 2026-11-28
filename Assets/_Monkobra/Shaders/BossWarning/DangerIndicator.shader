Shader "Monkobra/DangerIndicator" {
    Properties {
        [MainTexture] _BaseMap("Warning texture (RGB colour, A shape)", 2D) = "white" {}
        [MainColor] _BaseColor("Tint (A is the opacity)", Color) = (1,1,1,0.6)
        _TintStrength("How far the tint pulls the texture colour", Range(0,1)) = 0.5
        [Enum(Strip top, 0, Arrow bottom, 1)] _Band("Band of the texture", Float) = 0
        [Enum(Disc XZ, 0, Line XY, 1, Wall round Y, 2)] _PlaneMode("Plane mode", Float) = 0
        _TileSize("Disc and wall: world units per band height. Line: width multiple", Float) = 1
        _BandInset("Gap kept from the band edge (V, 0 to 0.1)", Range(0, 0.1)) = 0.02
        _Scroll("Scroll along U, tiles per second", Float) = 0.5
        _Direction("Direction along U, 1 or -1 (flips arrows and scroll)", Float) = 1
        _PulseRate("Pulse rate at rest, rad per second", Float) = 5
        _PulseRateMax("Pulse rate at full urgency", Float) = 20
        _PulseDepth("Pulse depth, 0 steady to 1 blinking", Range(0,1)) = 0.35
        _Glow("Extra brightness at full urgency", Range(0,2)) = 0.4
        _Urgency("Urgency 0 to 1, driven by the attack", Range(0,1)) = 0
    }
    SubShader {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        Pass {
            Name "DangerIndicator"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _TintStrength, _PulseDepth, _Glow, _Urgency;
                float _Band, _PlaneMode, _TileSize, _BandInset, _Scroll, _Direction;
                float _PulseRate, _PulseRateMax;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {
                float4 positionCS:SV_POSITION;
                float3 positionOS:TEXCOORD0;
                float3 scaleWS:TEXCOORD1;
            };
            Varyings Vert(Attributes input) {
                Varyings output;
                output.positionOS=input.positionOS.xyz;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                // world scale of the object, so the art keeps its size when a fill grows or a line stretches
                float4x4 m=GetObjectToWorldMatrix();
                output.scaleWS=float3(length(m._m00_m10_m20),length(m._m01_m11_m21),length(m._m02_m12_m22));
                return output;
            }
            // triangle wave, 0 to 1 and back, so V mirrors at the band edge and never wraps
            float MirrorRepeat(float x) {
                return 1.0-abs(1.0-frac(x*0.5)*2.0);
            }
            half4 Frag(Varyings input):SV_Target {
                float3 p=input.positionOS;
                float3 s=input.scaleWS;
                float u,t;
                if(_PlaneMode<0.5) {
                    // disc: the flat cap, in the XZ plane
                    u=(p.z*s.z)/(2.0*_TileSize);
                    t=(p.x*s.x)/_TileSize+0.5;
                } else if(_PlaneMode<1.5) {
                    // line or strip: the whole band fits the width, U runs along Y
                    u=(p.y*s.y)/(2.0*max(s.x,0.0001)*_TileSize);
                    t=p.x+0.5;
                } else {
                    // wall: round the cylinder, U along the arc, V up the height
                    float arc=atan2(p.z,p.x)*0.5*s.x;
                    u=arc/(2.0*_TileSize);
                    t=(p.y*s.y)/_TileSize+0.5;
                }
                u=u*_Direction+frac(_Scroll*_Time.y);
                float bandLow=0.5*(1.0-_Band);
                float v=bandLow+_BandInset+(0.5-2.0*_BandInset)*MirrorRepeat(t);
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(u,v));
                // texture colour first, the tint only nudges it
                half3 rgb=tex.rgb*lerp(half3(1,1,1),_BaseColor.rgb,_TintStrength);
                rgb*=1.0+_Urgency*_Glow;
                float rate=lerp(_PulseRate,_PulseRateMax,_Urgency);
                float beat=0.5+0.5*sin(_Time.y*rate);
                half pulse=1.0-_PulseDepth*(1.0-beat);
                half alpha=saturate(tex.a*_BaseColor.a*pulse);
                return half4(rgb,alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
