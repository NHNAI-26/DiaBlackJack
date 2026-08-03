using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace DiaBlackJack.Rendering
{
    public sealed class PostProcessOutlineRendererFeature : ScriptableRendererFeature
    {
        private const string MaskShaderName = "Hidden/NHN/Post Process Outline Mask";
        private const string CompositeShaderName =
            "Hidden/NHN/Post Process Outline Composite";

        [SerializeField] private RenderPassEvent injectionPoint =
            RenderPassEvent.AfterRenderingPostProcessing;

        private OutlinePass _pass;
        private Material _maskMaterial;
        private Material _compositeMaterial;

        public override void Create()
        {
            _maskMaterial = CoreUtils.CreateEngineMaterial(Shader.Find(MaskShaderName));
            _compositeMaterial =
                CoreUtils.CreateEngineMaterial(Shader.Find(CompositeShaderName));
            _pass = new OutlinePass
            {
                renderPassEvent = injectionPoint
            };
            _pass.Setup(_maskMaterial, _compositeMaterial);
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (_maskMaterial == null ||
                _compositeMaterial == null ||
                !PostProcessOutlineRegistry.HasTargets)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_maskMaterial);
            CoreUtils.Destroy(_compositeMaterial);
        }

        private sealed class OutlinePass : ScriptableRenderPass
        {
            private static readonly int MaskTextureId =
                Shader.PropertyToID("_NHNPostProcessOutlineMask");
            private static readonly int TempColorTextureId =
                Shader.PropertyToID("_NHNPostProcessOutlineTempColor");
            private static readonly int MainTextureId =
                Shader.PropertyToID("_MainTex");
            private static readonly int MaskTexelSizeId =
                Shader.PropertyToID("_NHNPostProcessOutlineMask_TexelSize");
            private static readonly int OutlineColorId =
                Shader.PropertyToID("_NHNPostProcessOutlineColor");
            private static readonly int OutlineWidthPixelsId =
                Shader.PropertyToID("_NHNPostProcessOutlineWidthPixels");

            private static Mesh _fullscreenMesh;

            private readonly List<PostProcessOutlineRegistry.Target> _targets =
                new List<PostProcessOutlineRegistry.Target>();

            private Material _maskMaterial;
            private Material _compositeMaterial;

            public void Setup(Material maskMaterial, Material compositeMaterial)
            {
                _maskMaterial = maskMaterial;
                _compositeMaterial = compositeMaterial;
            }

#if UNITY_2023_3_OR_NEWER
            private sealed class PassData
            {
                public TextureHandle ColorTexture;
                public UniversalCameraData CameraData;
                public Material MaskMaterial;
                public Material CompositeMaterial;
                public List<PostProcessOutlineRegistry.Target> Targets;
            }

            public override void RecordRenderGraph(
                RenderGraph renderGraph,
                ContextContainer frameData)
            {
                PostProcessOutlineRegistry.FillTargets(_targets);
                if (_targets.Count == 0)
                {
                    return;
                }

                using (var builder =
                    renderGraph.AddUnsafePass<PassData>(
                        "NHN Post Process Outline",
                        out PassData passData))
                {
                    UniversalResourceData resourceData =
                        frameData.Get<UniversalResourceData>();
                    passData.ColorTexture = resourceData.activeColorTexture;
                    passData.CameraData = frameData.Get<UniversalCameraData>();
                    passData.MaskMaterial = _maskMaterial;
                    passData.CompositeMaterial = _compositeMaterial;
                    passData.Targets =
                        new List<PostProcessOutlineRegistry.Target>(_targets);

                    builder.AllowPassCulling(false);
                    builder.UseTexture(
                        resourceData.activeColorTexture,
                        AccessFlags.ReadWrite);
                    builder.SetRenderFunc(
                        (PassData data, UnsafeGraphContext context) =>
                        {
                            CommandBuffer cmd =
                                CommandBufferHelpers.GetNativeCommandBuffer(
                                    context.cmd);
                            ExecutePass(data, cmd);
                        });
                }
            }

            private static void ExecutePass(PassData data, CommandBuffer cmd)
            {
                RenderTextureDescriptor descriptor =
                    data.CameraData.cameraTargetDescriptor;
                RenderTargetIdentifier source = data.ColorTexture;
                ExecutePass(
                    data.Targets,
                    descriptor,
                    source,
                    data.MaskMaterial,
                    data.CompositeMaterial,
                    cmd);
            }
#else
            public override void Execute(
                ScriptableRenderContext context,
                ref RenderingData renderingData)
            {
            }
#endif

            private static void ExecutePass(
                List<PostProcessOutlineRegistry.Target> targets,
                RenderTextureDescriptor descriptor,
                RenderTargetIdentifier source,
                Material maskMaterial,
                Material compositeMaterial,
                CommandBuffer cmd)
            {
                if (targets == null ||
                    targets.Count == 0 ||
                    maskMaterial == null ||
                    compositeMaterial == null)
                {
                    return;
                }

                RenderTextureDescriptor maskDescriptor = descriptor;
                maskDescriptor.depthBufferBits = 0;
                maskDescriptor.msaaSamples = 1;
                maskDescriptor.colorFormat = RenderTextureFormat.R8;

                cmd.GetTemporaryRT(MaskTextureId, maskDescriptor, FilterMode.Point);
                RenderTargetIdentifier maskTarget = new RenderTargetIdentifier(
                    MaskTextureId,
                    0,
                    CubemapFace.Unknown,
                    -1);
                cmd.SetRenderTarget(maskTarget);
                cmd.ClearRenderTarget(false, true, Color.clear);

                Color outlineColor = Color.clear;
                float outlineWidthPixels = 0f;
                for (int i = 0; i < targets.Count; i++)
                {
                    PostProcessOutlineRegistry.Target target = targets[i];
                    Renderer renderer = target.Renderer;
                    if (renderer == null ||
                        !renderer.enabled ||
                        !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    outlineColor = target.Color;
                    outlineWidthPixels = Mathf.Max(
                        outlineWidthPixels,
                        target.WidthPixels);

                    int subMeshCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                    {
                        cmd.DrawRenderer(renderer, maskMaterial, subMesh, 0);
                    }
                }

                if (outlineWidthPixels <= 0f)
                {
                    cmd.ReleaseTemporaryRT(MaskTextureId);
                    return;
                }

                RenderTextureDescriptor colorDescriptor = descriptor;
                colorDescriptor.depthBufferBits = 0;
                colorDescriptor.msaaSamples = 1;

                cmd.GetTemporaryRT(
                    TempColorTextureId,
                    colorDescriptor,
                    FilterMode.Bilinear);
                cmd.SetGlobalTexture(MaskTextureId, maskTarget);
                compositeMaterial.SetVector(
                    MaskTexelSizeId,
                    new Vector4(
                        1f / maskDescriptor.width,
                        1f / maskDescriptor.height,
                        maskDescriptor.width,
                        maskDescriptor.height));
                compositeMaterial.SetColor(OutlineColorId, outlineColor);
                compositeMaterial.SetFloat(
                    OutlineWidthPixelsId,
                    outlineWidthPixels);

                FullScreenBlit(
                    cmd,
                    source,
                    TempColorTextureId,
                    compositeMaterial,
                    0);
                FullScreenBlit(
                    cmd,
                    TempColorTextureId,
                    source,
                    compositeMaterial,
                    1);

                cmd.ReleaseTemporaryRT(TempColorTextureId);
                cmd.ReleaseTemporaryRT(MaskTextureId);
            }

            private static void FullScreenBlit(
                CommandBuffer cmd,
                RenderTargetIdentifier source,
                RenderTargetIdentifier destination,
                Material material,
                int passIndex)
            {
                destination = new RenderTargetIdentifier(
                    destination,
                    0,
                    CubemapFace.Unknown,
                    -1);
                cmd.SetRenderTarget(destination);
                cmd.SetGlobalTexture(MainTextureId, source);
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, material, 0, passIndex);
            }

            private static Mesh FullscreenMesh
            {
                get
                {
                    if (_fullscreenMesh != null)
                    {
                        return _fullscreenMesh;
                    }

                    _fullscreenMesh = new Mesh();
                    _fullscreenMesh.SetVertices(new List<Vector3>
                    {
                        new Vector3(-1f, -1f, 0f),
                        new Vector3(-1f, 1f, 0f),
                        new Vector3(1f, -1f, 0f),
                        new Vector3(1f, 1f, 0f)
                    });
                    _fullscreenMesh.SetUVs(0, new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(0f, 1f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 1f)
                    });
                    _fullscreenMesh.SetIndices(
                        new[] { 0, 1, 2, 2, 1, 3 },
                        MeshTopology.Triangles,
                        0,
                        false);
                    _fullscreenMesh.UploadMeshData(true);
                    return _fullscreenMesh;
                }
            }
        }
    }
}
