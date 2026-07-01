using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace _00_Work._01_Scripts.Environment
{
    public class UnderworldDistortionFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Shader shader;
            [Range(0f, 5f)] public float intensity  = 0f;
            public float speed     = 1.2f;
            public float frequency = 10f;
        }

        public Settings          settings = new Settings();
        public static UnderworldDistortionFeature Instance { get; private set; }

        private DistortionPass _pass;
        private Material       _mat;

        private static readonly int IntensityProp = Shader.PropertyToID("_Intensity");
        private static readonly int SpeedProp     = Shader.PropertyToID("_Speed");
        private static readonly int FreqProp      = Shader.PropertyToID("_Frequency");

        // ──────────────────────────────────────────────

        public override void Create()
        {
            Instance = this;

            if (settings.shader == null)
                settings.shader = Shader.Find("Custom/UnderworldDistortion");

            if (settings.shader != null)
                _mat = CoreUtils.CreateEngineMaterial(settings.shader);

            _pass = new DistortionPass();
            _pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (_pass == null || _mat == null) return;
            if (settings.intensity <= 0.001f) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;

            _mat.SetFloat(IntensityProp, settings.intensity);
            _mat.SetFloat(SpeedProp,     settings.speed);
            _mat.SetFloat(FreqProp,      settings.frequency);

            _pass.Setup(_mat);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_mat);
        }

        public void SetIntensity(float value)
        {
            settings.intensity = Mathf.Max(0f, value);
        }

        // ──────────────────────────────────────────────
        // RenderGraph Pass
        // ──────────────────────────────────────────────

        class DistortionPass : ScriptableRenderPass
        {
            private Material _mat;

            // RenderGraph에서 사용할 패스 데이터
            private class PassData
            {
                public Material          material;
                public TextureHandle     source;
                public TextureHandle     destination;
            }

            public DistortionPass()
            {
                profilingSampler = new ProfilingSampler("UnderworldDistortion");
            }

            public void Setup(Material mat)
            {
                _mat = mat;
            }

            // ── RenderGraph API (Unity 6 기본 모드) ───────────────────────
            public override void RecordRenderGraph(
                RenderGraph         renderGraph,
                ContextContainer    frameData)
            {
                if (_mat == null) return;

                var resourceData = frameData.Get<UniversalResourceData>();

                // 소스 텍스처
                TextureHandle source = resourceData.activeColorTexture;

                // 임시 텍스처 생성
                var desc = renderGraph.GetTextureDesc(source);
                desc.name        = "_UnderworldDistortionTemp";
                desc.clearBuffer = false;
                TextureHandle dest = renderGraph.CreateTexture(desc);

                // 패스 등록
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "UnderworldDistortion", out var passData, profilingSampler))
                {
                    passData.material    = _mat;
                    passData.source      = source;
                    passData.destination = dest;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(dest, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(
                            ctx.cmd,
                            data.source,
                            new Vector4(1, 1, 0, 0),
                            data.material,
                            0);
                    });
                }

                // 결과를 다시 activeColorTexture에 복사
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "UnderworldDistortion_Copy", out var copyData, profilingSampler))
                {
                    copyData.material    = null;
                    copyData.source      = dest;
                    copyData.destination = source;

                    builder.UseTexture(dest, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(
                            ctx.cmd,
                            data.source,
                            new Vector4(1, 1, 0, 0),
                            0, false);
                    });
                }
            }

            // ── Compatibility Mode 폴백 (RenderGraph 꺼져있을 때) ─────────
            [System.Obsolete]
            public override void Execute(
                ScriptableRenderContext context,
                ref RenderingData renderingData)
            {
                if (_mat == null) return;

                var cmd    = CommandBufferPool.Get("UnderworldDistortion");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples     = 1;

                RTHandle tempRT = RTHandles.Alloc(desc.width, desc.height,
                    colorFormat: desc.graphicsFormat,
                    name: "_UnderworldTemp");

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    Blitter.BlitCameraTexture(cmd, source, tempRT);
                    Blitter.BlitCameraTexture(cmd, tempRT, source, _mat, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                tempRT.Release();
            }
        }
    }
}