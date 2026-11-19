// The jungle backdrop's far world, painted on a dome round the camera: sky &
// clouds, 4 layers of hazy hills, and below them the forest canopy seen from
// above with the Amazon river winding through it. Nothing here is geometry:
// each pixel works out what its view ray meets (a hill ridge, the canopy's
// flat top, the forest floor under the near trees), so the whole far world
// is 1 draw at any height, and the hills sink & the forest opens out as the
// camera climbs, as a real landscape would.
//
// Every height-driven colour is a global JungleBackdrop sets, see
// BackdropCommon.hlsl. Drawn as an opaque dome that writes depth after the
// opaque scene, so Unity's own skybox never shows and the scene's ambient
// light stays as it was.
Shader "Monkobra/BackdropSky" {
    Properties {
        [Header(Canopy)]
        [NoScaleOffset] _DetailMap("Crowns from above, tiled", 2D) = "grey" {}
        _DetailScale("Crown tile size (u)", Float) = 180
        _DetailMeanLum("Crown tile mean luminance (linear)", Float) = 0.162
        [NoScaleOffset] _MacroMap("Forest tint (RGB, 0.5 neutral) + river (A)", 2D) = "grey" {}
        _MacroRadius("Macro map half width (u)", Float) = 2700
        _RiverColor("River", Color) = (0.26,0.70,0.80,1)
        _FloorColor("Forest floor, under the near trees", Color) = (0.24,0.35,0.25,1)

        [Header(Hills)]
        [NoScaleOffset] _RidgeMap("Ridge profiles, near (R) to far (A)", 2D) = "black" {}
        _RidgeDistance("Ridge distance (u)", Vector) = (2600,3600,5200,7500)
        _RidgeBase("Ridge foot height over the canopy (u)", Vector) = (40,150,350,600)
        _RidgeAmp("Ridge rise (u)", Vector) = (160,300,500,800)
        _HillNear("Nearest hills", Color) = (0.36,0.52,0.38,1)
        _HillFar("Farthest hills", Color) = (0.56,0.68,0.74,1)

        [Header(Clouds)]
        _CloudScale("Cloud scale", Float) = 0.7
        _CloudColor("Cloud", Color) = (1,1,1,1)
        _CloudShade("Cloud underside", Color) = (0.80,0.86,0.92,1)
        _CloudDrift("Cloud drift speed", Float) = 0.004
    }

    SubShader {
        // after the opaque scene, so it only fills what nothing else covers
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+450"}

        Pass {
            Name "Sky"
            Tags {"LightMode"="UniversalForward"}
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BackdropCommon.hlsl"

            TEXTURE2D(_RidgeMap); SAMPLER(sampler_RidgeMap);

            CBUFFER_START(UnityPerMaterial)
                float _DetailScale, _DetailMeanLum, _MacroRadius;
                half4 _RiverColor, _FloorColor;
                float4 _RidgeDistance, _RidgeBase, _RidgeAmp;
                half4 _HillNear, _HillFar;
                float _CloudScale;
                half4 _CloudColor, _CloudShade;
                float _CloudDrift;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float Hash12(float2 p) {
                float3 q = frac(float3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float Noise2(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash12(i), Hash12(i + float2(1, 0)), u.x),
                    lerp(Hash12(i + float2(0, 1)), Hash12(i + float2(1, 1)), u.x),
                    u.y);
            }

            // Sky  ------------------------------------------------------------
            half3 Sky(float3 direction) {
                float up = direction.y;
                half3 sky = lerp(_BackdropSkyHorizon.rgb, _BackdropSkyZenith.rgb, smoothstep(0.0, 0.6, up));

                // a cloud layer overhead, seen in perspective toward the horizon
                float2 plane = direction.xz / (max(up, 0.0) + 0.1) * _CloudScale
                    + _Time.y * _CloudDrift * float2(1.0, 0.4);
                float cloud = Noise2(plane) * 0.55 + Noise2(plane * 2.1 + 7.3) * 0.3
                    + Noise2(plane * 4.3 + 1.9) * 0.15;
                float threshold = 1.0 - _BackdropCloudAmount * 0.55;
                float cover = smoothstep(threshold, threshold + 0.08, cloud);
                cover *= smoothstep(0.0, 0.06, up);
                half3 puff = lerp(_CloudShade.rgb, _CloudColor.rgb, saturate((cloud - threshold) * 6.0));
                sky = lerp(sky, puff, cover * 0.9);

                // past the farthest hills, land lost in the haze
                half3 farLand = lerp(_HillFar.rgb, _BackdropHazeColor.rgb, 0.75);
                return lerp(sky, farLand, smoothstep(0.0, -0.05, up));
            }

            // Forest canopy & floor  ------------------------------------------
            // the forest the view ray meets going down, null alpha if none
            // nearer than the first ridge; over the ring of 3D trees' outer
            // part, the canopy's depth into depth, so it hides the trees
            // sunk into it
            half4 Forest(float3 eye, float3 direction, float slope, float pixelAngle, inout float depth) {
                float canopyY = _BackdropLayout.x;
                float drop = eye.y - canopyY;
                if (slope >= 0.0 || drop <= 0.0) {
                    return 0;
                }
                float across = drop / -slope;  // level distance to the canopy
                if (across > _RidgeDistance.x) {
                    return 0;
                }
                float3 hit = eye + direction * (drop / -direction.y);
                float2 fromCentre = hit.xz - _BackdropCentre.xz;
                float distanceU = distance(hit, eye);

                // the width a pixel spans on the canopy, for the texture LOD;
                // between its widths along & across the view
                float footprint = pixelAngle * distanceU * rsqrt(max(-direction.y, 0.02));
                half4 painted = BackdropCanopy(hit.xz, _DetailScale, _MacroRadius, footprint);
                half3 water = lerp(_RiverColor.rgb, _BackdropSkyHorizon.rgb, 0.1);
                half river = painted.a * _BackdropRiverReveal;
                half3 canopy = lerp(painted.rgb, water, river);
                // water cuts through the haze more than the forest, so the
                // river reads from far off
                canopy = BackdropHaze(canopy, distanceU * (1.0 - 0.45 * river));

                // nearer than the painted canopy, the floor between the trees
                float paintedFromU = _BackdropLayout.w;
                float radius = length(fromCentre);
                float floorDistance = (eye.y - _BackdropLayout.z) / -direction.y;
                half3 floorColor = BackdropHaze(_FloorColor.rgb, floorDistance);
                half color01 = smoothstep(paintedFromU - 40.0, paintedFromU, radius);
                half3 color = lerp(floorColor, canopy, color01);
                if (radius > paintedFromU && radius < _BackdropLayout.y + 60.0) {
                    float4 hitCS = TransformWorldToHClip(hit);
                    depth = hitCS.z / hitCS.w;
                }

                // melt into the first ridge's foot
                half alpha = smoothstep(_RidgeDistance.x, _RidgeDistance.x * 0.85, across);
                return half4(color, alpha);
            }

            half4 Frag(Varyings input, out float depth : SV_Depth) : SV_Target {
                depth = input.positionCS.z;
                float3 eye = _WorldSpaceCameraPos;
                float3 direction = normalize(input.positionWS - eye);
                // the angle 1 pixel spans; direction is smooth over the
                // whole screen, so its derivatives never spike
                float pixelAngle = max(length(ddx(direction)), length(ddy(direction)));
                float across = max(length(direction.xz), 1e-4);
                float slope = direction.y / across;  // tan of the elevation
                float slopeAA = max(fwidth(slope), 1e-5);
                float azimuth = atan2(direction.z, direction.x) * (0.5 * INV_PI);

                half3 color = Sky(direction);

                // Hills, far to near  -----------------------------------------
                float canopyY = _BackdropLayout.x;
                half4 ridge = SAMPLE_TEXTURE2D_LOD(_RidgeMap, sampler_RidgeMap, float2(azimuth, 0.5), 0);
                [unroll]
                for (int i = 3; i >= 0; i--) {
                    float top = canopyY + _RidgeBase[i] + _RidgeAmp[i] * ridge[i];
                    float topSlope = (top - eye.y) / _RidgeDistance[i];
                    half cover = saturate((topSlope - slope) / slopeAA + 0.5);
                    // 0 at the hill's foot, 1 at its top
                    float y = eye.y + slope * _RidgeDistance[i];
                    half up01 = saturate((y - canopyY) / max(top - canopyY, 1.0));
                    half3 hill = lerp(_HillNear.rgb, _HillFar.rgb, i / 3.0);
                    hill *= lerp(0.8, 1.06, up01);
                    if (i < 2) {
                        // near ridges show their forest
                        float2 onRidge = float2(azimuth * TWO_PI * _RidgeDistance[i], y) / _DetailScale;
                        float lod = log2(max(pixelAngle * _RidgeDistance[i] * 512.0 / _DetailScale, 1.0));
                        half3 crowns = SAMPLE_TEXTURE2D_LOD(_DetailMap, sampler_DetailMap, onRidge, lod).rgb;
                        half lum = dot(crowns, half3(0.3, 0.6, 0.1)) / _DetailMeanLum;
                        hill *= lerp(1.0, lum, i == 0 ? 0.5 : 0.3);
                    }
                    hill = BackdropHaze(hill, _RidgeDistance[i]);
                    color = lerp(color, hill, cover);
                }

                // Forest, in front of every ridge  ----------------------------
                half4 forest = Forest(eye, direction, slope, pixelAngle, depth);
                color = lerp(color, forest.rgb, forest.a);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
