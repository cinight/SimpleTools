using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace GfxQA.ShaderVariantTool
{
    class ShaderVariantTool_ShaderPreprocess : IPreprocessShaders
    {
        public ShaderVariantTool_ShaderPreprocess()
        {
            SVL.ResetBuildList();
        }

        public int callbackOrder { get { return 10; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
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
                    scv_default.passName = snippet.passName;
                    scv_default.passType = snippet.passType.ToString();
                    scv_default.shaderType = snippet.shaderType.ToString();
                    scv_default.kernelName = "--";
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
                    scv.passName = snippet.passName;
                    scv.passType = snippet.passType.ToString();
                    scv.shaderType = snippet.shaderType.ToString();
                    scv.kernelName = "--";

                    scv.graphicsTier = data[i].graphicsTier.ToString();
                    scv.shaderCompilerPlatform = data[i].shaderCompilerPlatform.ToString();
                    //scv.shaderRequirements = ""+data[i].shaderRequirements;
                    //scv.platformKeywordName = ""+data[i].platformKeywordSet.ToString();
                    //scv.isplatformKeywordEnabled = ""+data[i].platformKeywordSet.IsEnabled(BuiltinShaderDefine.SHADER_API_DESKTOP);

                    bool isLocal = ShaderKeyword.IsKeywordLocal(sk[k]);
                    LocalKeyword lkey = new LocalKeyword(shader,sk[k].name);
                    scv.shaderKeywordName = ( isLocal? "[Local]" : "[Global]" ) + sk[k].name; //sk[k].GetKeywordName();
                    scv.shaderKeywordType = isLocal? "--" : ShaderKeyword.GetGlobalKeywordType(sk[k]).ToString(); //""+sk[k].GetKeywordType().ToString();
                    scv.shaderKeywordIndex = sk[k].index.ToString();
                    scv.isShaderKeywordValid = sk[k].IsValid().ToString();
                    scv.isShaderKeywordEnabled = data[i].shaderKeywordSet.IsEnabled(sk[k]).ToString();

                    SVL.variantlist.Add(scv);
                    SVL.compiledTotalCount++;

                    //Just to verify API is correct
                    //string globalShaderKeywordName = ShaderKeyword.GetGlobalKeywordName(sk[k]);
                    //if( !isLocal && globalShaderKeywordName != ShaderKeyword.GetKeywordName(shader,sk[k]) ) Debug.LogError("Bug. ShaderKeyword.GetGlobalKeywordName() and  ShaderKeyword.GetKeywordName() is wrong");
                    //ShaderKeywordType globalShaderKeywordType = ShaderKeyword.GetGlobalKeywordType(sk[k]);
                    //if( !isLocal && globalShaderKeywordType != ShaderKeyword.GetKeywordType(shader,sk[k]) ) Debug.LogError("Bug. ShaderKeyword.GetGlobalKeywordType() and  ShaderKeyword.GetKeywordType() is wrong");
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
}