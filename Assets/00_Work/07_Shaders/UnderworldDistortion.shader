Shader "Custom/UnderworldDistortion"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "UnderworldDistortion"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            // Blitter API 사용 시 필요한 헤더
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Blitter가 _BlitTexture를 제공함
            // _MainTex 대신 _BlitTexture 사용

            float _Intensity;
            float _Speed;
            float _Frequency;

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float  t  = _Time.y * _Speed;

                // X 방향 파동 (수평 물결)
                float waveX = sin(uv.y * _Frequency       + t)          * 1.0
                            + sin(uv.y * _Frequency * 0.5 + t * 1.4)    * 0.6
                            + sin(uv.y * _Frequency * 1.7 + t * 0.7)    * 0.3;

                // Y 방향 파동 (수직 아지랑이)
                float waveY = sin(uv.x * _Frequency * 0.7 + t * 0.9)    * 1.0
                            + sin(uv.x * _Frequency * 1.3 + t * 0.5)    * 0.5
                            + sin(uv.x * _Frequency * 0.3 + t * 1.1)    * 0.4;

                // 가장자리 강조
                float2 center   = uv - 0.5;
                float  edgeMask = 1.0 + dot(center, center) * 1.5;

                float2 offset = float2(waveX, waveY)
                              * _Intensity
                              * edgeMask
                              * 0.008;

                float2 distortedUV = saturate(uv + offset);

                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUV);
            }
            ENDHLSL
        }
    }
}
