using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class ShaderVariantTool_ComputePreprocess : IPreprocessComputeShaders
{
    public ShaderVariantTool_ComputePreprocess()
    {
        //Debug.Log("ShaderVariantTool_ComputePreprocess starts now..");
        SVL.ResetBuildList();
    }

    public int callbackOrder { get { return 10; } }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        int newVariantsForThisShader = 0;

        //The real variant count
        newVariantsForThisShader+=data.Count;

        //Go through all the variants
        for (int i = 0; i < data.Count; ++i)
        {
            ShaderKeyword[] sk = data[i].shaderKeywordSet.GetShaderKeywords();

            //The default variant
            if(sk.Length==0)
            {
                CompiledShaderVariant scv_default = new CompiledShaderVariant();
                //scv.id = id;
                scv_default.shaderName = shader.name;
                scv_default.kernelName = kernelName;
                scv_default.passName = "--";
                scv_default.passType = "--";
                scv_default.shaderType = "--";
                scv_default.graphicsTier = "--";
                scv_default.shaderCompilerPlatform = "--";
                scv_default.shaderKeywordName = "No Keyword / All Off";
                scv_default.shaderKeywordType = "--";
                scv_default.shaderKeywordIndex = "-1";
                scv_default.isShaderKeywordValid = "--";
                scv_default.isShaderKeywordEnabled = "--";
                SVL.variantlist.Add(scv_default);
                SVL.compiledTotalCount++;
            }

            for (int k = 0; k < sk.Length; ++k)
            {
                CompiledShaderVariant scv = new CompiledShaderVariant();

                //scv.id = id;
                scv.shaderName = shader.name;
                scv.kernelName = kernelName;
                scv.passName = "--";
                scv.passType = "--";
                scv.shaderType = "--";

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

                SVL.variantlist.Add(scv);
                SVL.compiledTotalCount++;

                //Just to verify API is correct
                string globalShaderKeywordName = ShaderKeyword.GetGlobalKeywordName(sk[k]);
                if( !isLocal && globalShaderKeywordName != ShaderKeyword.GetKeywordName(shader,sk[k]) ) Debug.LogError("Bug. ShaderKeyword.GetGlobalKeywordName() and  ShaderKeyword.GetKeywordName() is wrong");
                ShaderKeywordType globalShaderKeywordType = ShaderKeyword.GetGlobalKeywordType(sk[k]);
                if( !isLocal && globalShaderKeywordType != ShaderKeyword.GetKeywordType(shader,sk[k]) ) Debug.LogError("Bug. ShaderKeyword.GetGlobalKeywordType() and  ShaderKeyword.GetKeywordType() is wrong");
            }
        }

        //Add to shader list
        int compiledShaderId = SVL.shaderlist.FindIndex( o=> o.name == shader.name );
        if( compiledShaderId == -1 )
        {
            CompiledShader newCompiledShader = new CompiledShader();
            newCompiledShader.name = shader.name;
            newCompiledShader.guiEnabled = false;
            newCompiledShader.noOfVariantsForThisShader = 0;
            SVL.shaderlist.Add(newCompiledShader);
            compiledShaderId=SVL.shaderlist.Count-1;
        }

        //Add variant count to shader
        CompiledShader compiledShader = SVL.shaderlist[compiledShaderId];
        compiledShader.noOfVariantsForThisShader += newVariantsForThisShader;
        SVL.shaderlist[compiledShaderId] = compiledShader;

        //Add to total count
        SVL.variantTotalCount+=newVariantsForThisShader;
    }
}