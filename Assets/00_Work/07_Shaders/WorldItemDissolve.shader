Shader "Custom/WorldItemDissolve"
{
    Properties
    {
        _BaseMap        ("Base Map",        2D)          = "white" {}
        _BaseColor      ("Base Color",      Color)       = (1,1,1,1)

        _DissolveAmount ("Dissolve Amount", Range(0,1))  = 0
        _EdgeWidth      ("Edge Width",      Range(0,0.3)) = 0.08
        _EdgeColor      ("Edge Color",      Color)       = (1, 0.4, 0.1, 1)
        _EdgeEmission   ("Edge Emission",   Float)       = 3.0

        _NoiseTex       ("Noise Texture",   2D)          = "white" {}
        _NoiseTiling    ("Noise Tiling",    Float)       = 3.0

        _Cutoff         ("Alpha Cutoff",    Range(0,1))  = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend Off
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _DissolveAmount;
                float  _EdgeWidth;
                float4 _EdgeColor;
                float  _EdgeEmission;
                float  _NoiseTiling;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 베이스 알파 클리핑 (기존 투명 픽셀 제거)
                clip(baseColor.a - _Cutoff);

                // 노이즈 기반 디졸브 클리핑
                float2 noiseUV = IN.positionWS.xz * _NoiseTiling * 0.1;
                float  noise   = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                clip(noise - _DissolveAmount);

                // 엣지 (타오르는 경계)
                float edge = saturate((noise - _DissolveAmount) / max(_EdgeWidth, 0.001));

                // 라이팅
                InputData lightData = (InputData)0;
                lightData.positionWS      = IN.positionWS;
                lightData.normalWS        = normalize(IN.normalWS);
                lightData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightData.shadowCoord     = TransformWorldToShadowCoord(IN.positionWS);
                lightData.fogCoord        = IN.fogFactor;
                lightData.bakedGI         = SampleSH(IN.normalWS);

                SurfaceData surfData = (SurfaceData)0;
                surfData.albedo     = lerp(_EdgeColor.rgb * _EdgeEmission, baseColor.rgb, edge);
                surfData.alpha      = 1.0;
                surfData.smoothness = 0;
                surfData.occlusion  = 1;

                half4 color = UniversalFragmentPBR(lightData, surfData);

                // 엣지 이미시브
                color.rgb += (1.0 - edge) * _EdgeColor.rgb * _EdgeEmission;
                color.a    = 1.0;

                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // 섀도우 패스
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _DissolveAmount;
                float  _EdgeWidth;
                float4 _EdgeColor;
                float  _EdgeEmission;
                float  _NoiseTiling;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // Unity 6 URP 섀도우 바이어스
                Light mainLight  = GetMainLight();
                float3 lightDir  = mainLight.direction;
                float  invNdotL  = 1.0 - saturate(dot(lightDir, normalWS));
                float  scale     = invNdotL * _ShadowBias.y;
                posWS            = lightDir * _ShadowBias.xxx + posWS;
                posWS            = normalWS * scale.xxx + posWS;

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.positionWS = posWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(baseColor.a - _Cutoff);

                float2 noiseUV = IN.positionWS.xz * _NoiseTiling * 0.1;
                float  noise   = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                clip(noise - _DissolveAmount);

                return 0;
            }
            ENDHLSL
        }
    }
}
