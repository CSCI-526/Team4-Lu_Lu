// What both backdrop shaders share: the globals JungleBackdrop sets from the
// climbed height, and the haze that makes far things fade into the sky.
#ifndef MONKOBRA_BACKDROP_COMMON_INCLUDED
#define MONKOBRA_BACKDROP_COMMON_INCLUDED

// set by JungleBackdrop  ------------------------------------------------------
half4 _BackdropSkyZenith;
half4 _BackdropSkyHorizon;
half4 _BackdropHazeColor;
// haze per u of distance, thinner the higher the monkey climbs
float _BackdropHazeDensity;
half _BackdropCloudAmount;
half _BackdropRiverReveal;
// toward the sun
float4 _BackdropSunDir;
// x: the forest's canopy height; y: radius of the ring of real 3D trees;
// z: the forest floor's height; w: radius the painted canopy starts at, its
// top hiding the 3D trees beyond as they sink into it; all world u
float4 _BackdropLayout;
// the trunk's axis, which the forest & hills ring round; world
float4 _BackdropCentre;

// the canopy seen from above: crowns tiled in world space, tinted by the
// macro map round the trunk, the river in the macro map's alpha
TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);
TEXTURE2D(_MacroMap); SAMPLER(sampler_MacroMap);

/// the angle 1 pixel spans at the screen's middle; radians
float BackdropPixelAngle() {
    return 2.0 / (abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y);
}

/// the painted canopy's colour at <paramref name="xz"/>, rgb, & its river
/// mask, a; <paramref name="footprintU"/> the width a pixel spans there
half4 BackdropCanopy(float2 xz, float detailScaleU, float macroRadiusU, float footprintU) {
    float detailLod = log2(max(footprintU * 512.0 / detailScaleU, 1.0));
    float macroLod = log2(max(footprintU * 1024.0 / (2.0 * macroRadiusU), 1.0));
    half3 crowns = SAMPLE_TEXTURE2D_LOD(_DetailMap, sampler_DetailMap, xz / detailScaleU, detailLod).rgb;
    half4 macro = SAMPLE_TEXTURE2D_LOD(_MacroMap, sampler_MacroMap,
        (xz - _BackdropCentre.xz) / (2.0 * macroRadiusU) + 0.5, macroLod);
    return half4(crowns * macro.rgb * 2.0, macro.a);
}

/// a colour faded into the haze by its distance from the eye
half3 BackdropHaze(half3 color, float distanceU) {
    half haze = 1.0 - exp(-distanceU * _BackdropHazeDensity);
    return lerp(color, _BackdropHazeColor.rgb, haze);
}

/// an sRGB colour, eg a vertex colour, into the working colour space
half3 BackdropToWorkingSpace(half3 color) {
#if defined(UNITY_COLORSPACE_GAMMA)
    return color;
#else
    return color * (color * (color * 0.305306011 + 0.682171111) + 0.012522878);
#endif
}

#endif
