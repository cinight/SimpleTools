using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class StrippingExample_Shader : IPreprocessShaders
{
    public StrippingExample_Shader()
    {

    }

    public int callbackOrder { get { return 0; } }
    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            //Get a string of keywords
            string variantText = "";
            foreach(ShaderKeyword s in data[i].shaderKeywordSet.GetShaderKeywords())
            {
                variantText += " " +s.name;
            }

            bool wantToStrip = false;

            //Only stripping specific shader's specific keyword
            if(
                shader.name == "Test/MyShader" && variantText.Contains("_USELESSKEYWORD")
            )
            {
                wantToStrip = true;
            }

            if ( wantToStrip )
            {
                //Strip the variant
                data.RemoveAt(i);
                --i;
            }
        }
    }
}

//==============================

class StrippingExample_ComputeShader : IPreprocessComputeShaders
{
    public StrippingExample_ComputeShader()
    {

    }

    public int callbackOrder { get { return 0; } }
    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            //Get a string of keywords
            string variantText = "";
            foreach(ShaderKeyword s in data[i].shaderKeywordSet.GetShaderKeywords())
            {
                variantText += " " +s.name;
            }

            bool wantToStrip = false;

            //Only stripping specific shader's specific keyword
            if(
                shader.name == "Test/MyShader" && variantText.Contains("_USELESSKEYWORD")
            )
            {
                wantToStrip = true;
            }

            if ( wantToStrip )
            {
                //Strip the variant
                data.RemoveAt(i);
                --i;
            }
        }
    }
}