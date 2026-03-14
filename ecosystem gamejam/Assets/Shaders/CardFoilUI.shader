Shader "Custom/CardFoilUI"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _PatternTex("Foil Pattern", 2D) = "white" {}
        _MaskTex("Effect Mask", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,0.08)
        _FoilColorA("Foil Color A", Color) = (0.3,0.8,1,1)
        _FoilColorB("Foil Color B", Color) = (1,0.5,0.9,1)
        _FoilColorC("Foil Color C", Color) = (1,0.9,0.35,1)
        _Speed("Speed", Float) = 1.2
        _Strength("Strength", Float) = 0.35
        _PatternStrength("Pattern Strength", Float) = 0.7
        _RainbowStrength("Rainbow Strength", Float) = 0.65
        _MaskStrength("Mask Strength", Float) = 1
        _FresnelPower("Fresnel Power", Float) = 3.2
        _PatternTiling("Pattern Tiling", Vector) = (2.5,3.5,0,0)
        _PatternScroll("Pattern Scroll", Vector) = (0.08,0.16,0,0)
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _PatternTex_ST;
                float4 _MaskTex_ST;
                float4 _BaseColor;
                float4 _FoilColorA;
                float4 _FoilColorB;
                float4 _FoilColorC;
                float _Speed;
                float _Strength;
                float _PatternStrength;
                float _RainbowStrength;
                float _MaskStrength;
                float _FresnelPower;
                float4 _PatternTiling;
                float4 _PatternScroll;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_PatternTex);
            SAMPLER(sampler_PatternTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            float3 HueShift(float t)
            {
                float3 phase = float3(0.0, 0.3333, 0.6666);
                return saturate(abs(frac(t + phase) * 6.0 - 3.0) - 1.0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float2 patternUv = (IN.uv * _PatternTiling.xy) + (_Time.y * _Speed * _PatternScroll.xy);
                half4 patternSample = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, patternUv);
                half4 maskSample = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, TRANSFORM_TEX(IN.uv, _MaskTex));

                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float3 normalWS = normalize(TransformObjectToWorldDir(float3(0.0, 0.0, -1.0)));
                float facing = saturate(dot(viewDirWS, normalWS));
                float fresnel = pow(saturate(1.0 - facing), _FresnelPower);

                float t = _Time.y * _Speed;
                float sweep = sin((IN.uv.x * 12.0) + (IN.uv.y * 6.0) + t + (fresnel * 4.0));
                float ripple = sin((IN.uv.y * 18.0) - (t * 1.7) + (patternSample.r * 3.14159));
                float shimmer = saturate((sweep + ripple) * 0.25 + 0.5);

                float rainbowPhase = frac((IN.uv.x * 0.45) + (fresnel * 0.75) + (patternSample.g * 0.25) + (t * 0.05));
                float3 rainbow = HueShift(rainbowPhase);
                float3 foilGradient = lerp(_FoilColorA.rgb, _FoilColorB.rgb, saturate(IN.uv.x + shimmer * 0.35));
                foilGradient = lerp(foilGradient, _FoilColorC.rgb, saturate(ripple * 0.5 + 0.5));
                float3 holoColor = lerp(foilGradient, rainbow, _RainbowStrength);

                float patternValue = dot(patternSample.rgb, float3(0.299, 0.587, 0.114));
                float maskValue = lerp(1.0, maskSample.a > 0.001 ? maskSample.a : dot(maskSample.rgb, float3(0.299, 0.587, 0.114)), _MaskStrength);
                float effect = saturate((_BaseColor.a + (shimmer * _Strength) + (patternValue * _PatternStrength) + (fresnel * _RainbowStrength)) * maskValue);

                float3 finalColor = holoColor * texColor.rgb * IN.color.rgb;
                return half4(finalColor, effect * texColor.a * IN.color.a);
            }
            ENDHLSL
        }
    }
}
