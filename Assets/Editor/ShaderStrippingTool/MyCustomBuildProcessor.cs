using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class MyCustomBuildProcessor : IPreprocessShaders
{
    public static int variantCount = 0;

    public MyCustomBuildProcessor()
    {
        SVL.list = new List<ShaderCompiledVariant>();
        SVL.list.Clear();
        variantCount = 0;
    }

    public int callbackOrder { get { return 10; } }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            ShaderKeyword[] sk = data[i].shaderKeywordSet.GetShaderKeywords();
            for (int k = 0; k < sk.Length; ++k)
            {
                ShaderCompiledVariant scv = new ShaderCompiledVariant();

                //scv.id = id;
                scv.shaderName = shader.name;
                scv.passName = ""+snippet.passName;
                scv.passType = ""+snippet.passType.ToString();
                scv.shaderType = ""+snippet.shaderType.ToString();

                scv.graphicsTier = ""+data[i].graphicsTier;
                scv.shaderCompilerPlatform = ""+data[i].shaderCompilerPlatform;
                //scv.shaderRequirements = ""+data[i].shaderRequirements;

                //scv.platformKeywordName = ""+data[i].platformKeywordSet.ToString();
                //scv.isplatformKeywordEnabled = ""+data[i].platformKeywordSet.IsEnabled(BuiltinShaderDefine.SHADER_API_DESKTOP);
                bool isLocal = ShaderKeyword.IsKeywordLocal(sk[k]);
                scv.shaderKeywordName = ( isLocal? "[Local] " : "[Global] " ) + ShaderKeyword.GetKeywordName(shader,sk[k]); //sk[k].GetKeywordName();
                scv.shaderKeywordType = ""+ShaderKeyword.GetKeywordType(shader,sk[k]); //""+sk[k].GetKeywordType().ToString();
                scv.shaderKeywordIndex = ""+sk[k].index;
                scv.isShaderKeywordValid = ""+sk[k].IsValid();
                scv.isShaderKeywordEnabled = ""+data[i].shaderKeywordSet.IsEnabled(sk[k]);

                SVL.list.Add(scv);
                variantCount++;

                //Just to verify API is correct
                string globalShaderKeywordName = ShaderKeyword.GetGlobalKeywordName(sk[k]);
                if( !isLocal && globalShaderKeywordName != ShaderKeyword.GetKeywordName(shader,sk[k]) ) Debug.Log("Bug. ShaderKeyword.GetGlobalKeywordName() and  ShaderKeyword.GetKeywordName() is wrong");
                ShaderKeywordType globalShaderKeywordType = ShaderKeyword.GetGlobalKeywordType(sk[k]);
                if( !isLocal && globalShaderKeywordType != ShaderKeyword.GetKeywordType(shader,sk[k]) ) Debug.Log("Bug. ShaderKeyword.GetGlobalKeywordType() and  ShaderKeyword.GetKeywordType() is wrong");
            }
        }
        ShaderStrippingTool.sorted = false;
    }
}