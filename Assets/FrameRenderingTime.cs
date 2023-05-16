using UnityEngine;
using UnityEngine.Rendering;

public class FrameRenderingTime : MonoBehaviour
{
    private double time = 0;

    void Start()
    {
        RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
        RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
    }

    void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        time = Time.realtimeSinceStartupAsDouble;
    }

    void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        double renderFrameTime = Time.realtimeSinceStartupAsDouble - time;

        renderFrameTime *= 1000.0f; //ms
        string fst = System.String.Format("{0:F2}ms / " , renderFrameTime);
        Debug.Log("renderFrameTime: "+fst+"   realtimeSinceStartupAsDouble "+time);
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
        RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
    }
}