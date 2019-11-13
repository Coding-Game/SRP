using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private ScriptableRenderContext scriptableRenderContext;
    private Camera camera;

#if UNITY_EDITOR
    private static string bufferName = "Render Camera";
#endif
    private CommandBuffer commandBuffer = new CommandBuffer() { name = bufferName };
    private CullingResults cullingResults;
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");


    public void Render(ScriptableRenderContext scriptableRenderContext, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        this.scriptableRenderContext = scriptableRenderContext;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull()) return;

        Setup();

        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();

        Submit();
    }

    private void Setup()
    {
        scriptableRenderContext.SetupCameraProperties(camera);
#if UNITY_EDITOR
        commandBuffer.BeginSample(bufferName);
#endif
        CameraClearFlags flags = camera.clearFlags;
        commandBuffer.ClearRenderTarget( flags <= CameraClearFlags.Depth, 
                                         flags == CameraClearFlags.Color,
                                         flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        ExecuteCommandBuffer();
    }

    private bool Cull()
    {
        if(camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
        {
            cullingResults = scriptableRenderContext.Cull(ref scriptableCullingParameters);
            return true;
        }

        return false;
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableInstancing = useGPUInstancing,
            enableDynamicBatching = useDynamicBatching
        };
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        scriptableRenderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        scriptableRenderContext.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        scriptableRenderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void ExecuteCommandBuffer()
    {
        scriptableRenderContext.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }

    private void Submit()
    {
#if UNITY_EDITOR
        commandBuffer.EndSample(bufferName);
        ExecuteCommandBuffer();
#endif
        scriptableRenderContext.Submit();
    }
}
