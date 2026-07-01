using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace _00_Work._01_Scripts.Environment
{
    public class WaterDistortionFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Shader  shader;
            [Range(0f, 3f)] public float intensity   = 0f;
            public float speed      = 0.8f;
            public float frequency  = 6f;
            [Range(0f, 0.6f)] public float fogDensity = 0f;
            public Color fogColor   = new Color(0.1f, 0.3f, 0.5f, 1f);
        }

        public Settings settings = new Settings();
        public static WaterDistortionFeature Instance { get; private set; }

        private WaterPass _pass;
        private Material  _mat;

        private static readonly int IntensityProp  = Shader.PropertyToID("_Intensity");
        private static readonly int SpeedProp      = Shader.PropertyToID("_Speed");
        private static readonly int FreqProp       = Shader.PropertyToID("_Frequency");
        private static readonly int FogDensityProp = Shader.PropertyToID("_FogDensity");
        private static readonly int FogColorProp   = Shader.PropertyToID("_FogColor");

        public override void Create()
        {
            Instance = this;

            if (settings.shader == null)
                settings.shader = Shader.Find("Custom/WaterDistortion");

            if (settings.shader != null)
                _mat = CoreUtils.CreateEngineMaterial(settings.shader);

            _pass = new WaterPass();
            _pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (_pass == null || _mat == null) return;
            if (settings.intensity <= 0.001f && settings.fogDensity <= 0.001f) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;

            _mat.SetFloat(IntensityProp,  settings.intensity);
            _mat.SetFloat(SpeedProp,      settings.speed);
            _mat.SetFloat(FreqProp,       settings.frequency);
            _mat.SetFloat(FogDensityProp, settings.fogDensity);
            _mat.SetColor(FogColorProp,   settings.fogColor);

            _pass.Setup(_mat);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_mat);
        }

        public void SetWaterEffect(float intensity, float fogDensity)
        {
            settings.intensity  = Mathf.Max(0f, intensity);
            settings.fogDensity = Mathf.Max(0f, fogDensity);
        }

        // ──────────────────────────────────────────────
        // RenderGraph Pass
        // ──────────────────────────────────────────────

        class WaterPass : ScriptableRenderPass
        {
            private Material _mat;

            private class PassData
            {
                public Material     material;
                public TextureHandle source;
                public TextureHandle destination;
            }

            public WaterPass()
            {
                profilingSampler = new ProfilingSampler("WaterDistortion");
            }

            public void Setup(Material mat) => _mat = mat;

            public override void RecordRenderGraph(
                RenderGraph      renderGraph,
                ContextContainer frameData)
            {
                if (_mat == null) return;

                var resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle source = resourceData.activeColorTexture;

                var desc = renderGraph.GetTextureDesc(source);
                desc.name        = "_WaterDistortionTemp";
                desc.clearBuffer = false;
                TextureHandle dest = renderGraph.CreateTexture(desc);

                // 1패스: 왜곡 + 안개 적용
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "WaterDistortion", out var passData, profilingSampler))
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
                            data.material, 0);
                    });
                }

                // 2패스: 결과 복사
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "WaterDistortion_Copy", out var copyData, profilingSampler))
                {
                    copyData.source      = dest;
                    copyData.destination = source;
                    copyData.material    = null;

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

            [System.Obsolete]
            public override void Execute(
                ScriptableRenderContext context,
                ref RenderingData renderingData)
            {
                if (_mat == null) return;

                var cmd    = CommandBufferPool.Get("WaterDistortion");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples     = 1;

                RTHandle tempRT = RTHandles.Alloc(
                    desc.width, desc.height,
                    colorFormat: desc.graphicsFormat,
                    name: "_WaterTemp");

                Blitter.BlitCameraTexture(cmd, source, tempRT);
                Blitter.BlitCameraTexture(cmd, tempRT, source, _mat, 0);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                tempRT.Release();
            }
        }
    }
}