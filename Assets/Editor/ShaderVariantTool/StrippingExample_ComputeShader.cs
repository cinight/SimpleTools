using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class StrippingExample_ComputeShader : IPreprocessComputeShaders
{
    private int m_TotalShaderVariantsInputCount = 0;
    private int m_TotalShaderVariantsOutputCount = 0;

    public StrippingExample_ComputeShader()
    {
    }

    public int callbackOrder { get { return 0; } }

    // Strip entire shader
    public bool StripComputeShader(ComputeShader shader)
    {
        if (
            shader.name == "EXAMPLE/Hidden/VideoDecode" ||
            shader.name == "EXAMPLE/BoatAttack/PackedPBR"
            )
            return true;
        return false;
    }

    // Strip entire kernel
    public bool StripComputeKernel(string kernelName)
    {
        if (
            kernelName == "EXAMPLE/CoC Temporal Filter" ||
            kernelName == "EXAMPLE2"
            )
            return true;
        return false;
    }

    // Strip specific keywords
    // shaderKeywordSet.IsEnabled always return false for local keywords
    // public bool StripComputeVariant(ShaderCompilerData data)
    // {
    //     if (
    //         data.shaderKeywordSet.IsEnabled(new ShaderKeyword("EXAMPLE_UNITY_COLORSPACE_GAMMA")) ||
    //         data.shaderKeywordSet.IsEnabled(new ShaderKeyword("EXAMPLE_DISTORT"))
    //         )
    //         return true;
    //     return false;
    // }

    // Strip specific keywords by name
    public bool StripComputeVariant(string variantText)
    {
        if (
            variantText.Contains("_xxxGOKEY1") ||
            variantText.Contains("_xxxLOKEY2")
            )
            return true;
        return false;
    }

    public bool StripLogic(ComputeShader shader, string kernelName, string variantText)
    {
        if (StripComputeShader(shader))
            return true;

        if (StripComputeKernel(kernelName))
            return true;

        if (shader.name.IndexOf("xxxCompute_ShaderVariant") >= 0)
            if (StripComputeVariant(variantText))
                return true;

        return false;
    }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        int initDataCount = data.Count;

        for (int i = 0; i < data.Count; ++i)
        {
            //Get a string of keywords
            string variantText = "";
            foreach(ShaderKeyword s in data[i].shaderKeywordSet.GetShaderKeywords())
            {
                variantText += " " +ShaderKeyword.GetKeywordName(shader,s);
            }

            if ( StripLogic(shader, kernelName, variantText) )
            {
                Debug.Log(CText("Stripped compute shader variant: "+shader.name + "(" + kernelName + ") Variant:" + variantText ));
                
                //Strip the variant
                data.RemoveAt(i);
                --i;
            }
        }

        m_TotalShaderVariantsInputCount += initDataCount;
        m_TotalShaderVariantsOutputCount += data.Count;


        //For summary stripping result
        //float percentageCurrent = (float)data.Count / (float)initDataCount * 100f;
        //float percentageTotal = (float)m_TotalShaderVariantsOutputCount / (float)m_TotalShaderVariantsInputCount * 100f;
        //string result = "STRIPPING RESULT: " + shader.name + "(" + snippet.shaderType.ToString() + ") - Remaining shader variants = " + data.Count + " / " + initDataCount + " = " + percentageCurrent + "% - Total = " + m_TotalShaderVariantsOutputCount + " / " + m_TotalShaderVariantsInputCount + " = " + percentageTotal + "%";
        
        //if( percentageCurrent > 90f)  Debug.Log(RText(result));
        //else if( percentageCurrent > 50f)  Debug.Log(YText(result)); 
        //else Debug.Log(GText(result));
    }

   private string GText(string text) { return "<color=#0f0>" + text + "</color>"; }
   private string YText(string text) { return "<color=#ff0>" + text + "</color>"; }
   private string RText(string text) { return "<color=#f80>" + text + "</color>"; }
   private string CText(string text) { return "<color=#0ff>" + text + "</color>"; }
}