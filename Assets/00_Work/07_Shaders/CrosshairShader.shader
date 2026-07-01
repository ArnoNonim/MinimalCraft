Shader "Custom/Crosshair"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZTest   Always
            ZWrite  Off
            Blend   OneMinusDstColor Zero  // 배경 색상 반전

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(tex.a - 0.5); // 투명 부분 제거
                return float4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}