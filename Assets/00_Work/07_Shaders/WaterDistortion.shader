Shader "Custom/WaterDistortion"
{
    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "WaterDistortion"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;   // 왜곡 강도
            float _Speed;       // 흐름 속도
            float _Frequency;   // 파동 촘촘함
            float _FogDensity;  // 뿌옇기
            float4 _FogColor;   // 안개 색상

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float  t  = _Time.y * _Speed;

                // 물속 — 느리고 부드러운 구형 파동
                // 수평 흐름 (물결이 옆으로 흐르는 느낌)
                float waveX = sin(uv.y * _Frequency       + t * 0.8)  * 1.0
                            + sin(uv.y * _Frequency * 1.3 + t * 0.5)  * 0.6
                            + sin(uv.y * _Frequency * 0.4 + t * 1.2)  * 0.3;

                // 수직 너울 (물 흐름 방향)
                float waveY = sin(uv.x * _Frequency * 0.6 + t * 0.7 + 1.5) * 0.8
                            + sin(uv.x * _Frequency * 1.1 + t * 0.4)        * 0.5
                            + cos(uv.y * _Frequency * 0.8 + t * 0.6)        * 0.4;

                // 화면 중앙보다 위아래 가장자리 강조 (수면 가까울수록 강하게)
                float vEdge = abs(uv.y - 0.5) * 2.0;
                float mask  = 1.0 + vEdge * 1.2;

                float2 offset = float2(waveX, waveY) * _Intensity * mask * 0.006;
                float2 distUV = saturate(uv + offset);

                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distUV);

                // 물속 안개 — FogColor와 블렌딩
                // 깊이감을 위해 UV.y 기반으로 아래쪽이 더 짙게
                float depthFog = _FogDensity * (1.0 + (1.0 - uv.y) * 0.5);
                depthFog = saturate(depthFog);

                half4 result = lerp(scene, _FogColor, depthFog);
                return result;
            }
            ENDHLSL
        }
    }
}
