using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace GfxQA.ShaderVariantTool
{
    class ShaderVariantTool_ComputePreprocess_Before : IPreprocessComputeShaders
    {
        public ShaderVariantTool_ComputePreprocess_Before()
        {
            SVL.ResetBuildList();  
        }

        public int callbackOrder { get { return -100; } }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            //Log ShaderItem
            int shaderItemId = SVL.shaderlist.FindIndex( o=> o.name == shader.name );
            ShaderItem shaderItem;
            if( shaderItemId == -1 )
            {
                //Make new ShaderItem
                shaderItem = new ShaderItem();
                shaderItem.name = shader.name;
                shaderItem.isComputeShader = true;
                shaderItem.assetPath = AssetDatabase.GetAssetPath(shader);
                SVL.shaderlist.Add(shaderItem);
            }
            else
            {
                //Get existing ShaderItem
                shaderItem = SVL.shaderlist[shaderItemId];
            }

            //Log variant count
            shaderItem.count_variant_before += (uint)data.Count;

            //Go through all the variants
            for (int i = 0; i < data.Count; ++i)
            {
                ShaderKeyword[] sk = data[i].shaderKeywordSet.GetShaderKeywords();
                
                //Construct a KeywordItem
                if(sk.Length==0) //Default, all keywords are off
                {
                    KeywordItem scv = new KeywordItem(shader,kernelName,data[i],new ShaderKeyword(),true);
                    shaderItem.SetMatchedKeywordItem(scv);
                }
                else //Non-default variant
                {
                    bool variantIsDynamic = false;
                    for (int k = 0; k < sk.Length; ++k)
                    {
                        KeywordItem scv = new KeywordItem(shader,kernelName,data[i],sk[k],false);
                        shaderItem.SetMatchedKeywordItem(scv);

                        if( scv.isDynamic )
                        {
                            variantIsDynamic = true;
                        }
                    }
                    if(variantIsDynamic)
                    {
                        shaderItem.count_dynamicVariant_before++;
                    }
                }
            }

        }
    }

    class ShaderVariantTool_ComputePreprocess_After : IPreprocessComputeShaders
    {
        public int callbackOrder { get { return 10; } }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            //Get existing ShaderItem
            int shaderItemId = SVL.shaderlist.FindIndex( o=> o.name == shader.name );
            ShaderItem shaderItem = SVL.shaderlist[shaderItemId];

            //Log variant count
            shaderItem.count_variant_after += (uint)data.Count;

            //Go through all the variants
            for (int i = 0; i < data.Count; ++i)
            {
                ShaderKeyword[] sk = data[i].shaderKeywordSet.GetShaderKeywords();
                
                //Construct a KeywordItem
                if(sk.Length==0) //Default, all keywords are off
                {
                    KeywordItem scv = new KeywordItem(shader,kernelName,data[i],new ShaderKeyword(),true);
                    shaderItem.SetMatchedKeywordItemCount(scv);
                }
                else //Non-default variant
                {
                    bool variantIsDynamic = false;
                    for (int k = 0; k < sk.Length; ++k)
                    {
                        KeywordItem scv = new KeywordItem(shader,kernelName,data[i],sk[k],false);
                        shaderItem.SetMatchedKeywordItemCount(scv);
                        
                        if( scv.isDynamic )
                        {
                            variantIsDynamic = true;
                        }
                    }
                    if(variantIsDynamic)
                    {
                        shaderItem.count_dynamicVariant_after++;
                    }
                }
            }
        }
    }

}