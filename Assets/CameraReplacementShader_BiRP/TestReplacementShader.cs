using UnityEngine;

// BuiltinRP only
// https://docs.unity3d.com/Manual/SL-ShaderReplacement.html
public class TestReplacementShader : MonoBehaviour
{
    public Camera cam;
    public Shader replaceShader;
    
    public enum RenderTypes
    {
        All,
        RenderType,
    }
    
    public RenderTypes renderType = RenderTypes.RenderType;
    
    void OnEnable()
    {
        Setup();
    }
    
    void OnValidate()
    {
        Setup();
    }

    void OnDisable()
    {
        Cleanup();
    }
    
    void OnDestroy()
    {
        Cleanup();
    }
    
    private void Setup()
    {
        string renderTypeStr = "";
        if (renderType != RenderTypes.All)
        {
            renderTypeStr = renderType.ToString();
        }
        cam.SetReplacementShader (replaceShader, renderTypeStr);

        Debug.Log("Configured Replacement Shader.");
    }
    
    private void Cleanup()
    {
        cam.ResetReplacementShader();
        
        Debug.Log("Reset Replacement Shader.");
    }
}

