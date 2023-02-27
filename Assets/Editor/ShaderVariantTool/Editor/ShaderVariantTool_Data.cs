using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace GfxQA.ShaderVariantTool
{
    
    //===================================================================================================

    public class ShaderItem
    {
        public bool isComputeShader = false;
        public string name;
        public string assetPath = "";
        
        public uint count_variant_before = 0; //includes dynamic variants
        public uint count_dynamicVariant_before = 0;
        public uint count_variant_after = 0; //includes dynamic variants
        public uint count_dynamicVariant_after = 0;

        public UInt64 editorLog_variantOriginalCount = 0;
        public UInt64 editorLog_variantAfterPrefilteringCount = 0;
        public UInt64 editorLog_variantAfterBuiltinStrippingCount = 0;
        public UInt64 editorLog_variantAfterSciptableStrippingCount = 0;
        public uint editorLog_variantCompiledCount = 0;
        public uint editorLog_variantInCache = 0;
        public float editorLog_timeCompile = 0;
        public float editorLog_timeStripping = 0;

        public List<KeywordItem> keywordItems = new List<KeywordItem>();

        public int FindMatchingVariantItem(KeywordItem scv)
        {
            //find the matching SCV
            int matchedId = keywordItems.FindIndex
            ( e =>
                e.shaderName == scv.shaderName &&
                e.passName == scv.passName &&
                e.passType == scv.passType &&
                e.shaderType == scv.shaderType &&
                e.kernelName == scv.kernelName &&
                e.graphicsTier == scv.graphicsTier &&
                e.buildTarget == scv.buildTarget &&
                e.shaderCompilerPlatform == scv.shaderCompilerPlatform &&
                e.platformKeywords == scv.platformKeywords &&
                e.shaderKeywordName == scv.shaderKeywordName &&
                e.shaderKeywordType == scv.shaderKeywordType
            );

            return matchedId;
        }

        //Use in Preprocess_Before
        public void SetMatchedKeywordItem(KeywordItem scv)
        {
            int matchedID = FindMatchingVariantItem(scv);
            if(matchedID == -1)
            {
                scv.appearCount_before++;
                keywordItems.Add(scv);
            }
            else
            {
                keywordItems[matchedID].appearCount_before ++;
            }
        }
        
        //Use in Preprocess_After
        public void SetMatchedKeywordItemCount(KeywordItem scv)
        {
            int matchedID = FindMatchingVariantItem(scv);
            keywordItems[matchedID].appearCount_after ++;
        }
        
        //Use in Build Postprocessor
        public void SetKeywordDeclareType()
        {
            //Not able to open this
            if(assetPath.EndsWith("unity_builtin_extra"))
            {
                foreach(KeywordItem item in keywordItems)
                {
                    item.shaderKeywordDeclareType = "N/A for builtin rescource";
                }
                return;
            }
            
            //Read shader code and find the #pragma lines
            string shaderCode = System.IO.File.ReadAllText(assetPath);
            string pattern = @"#pragma\s(\w+)\s(.*)";
            MatchCollection matches = Regex.Matches(shaderCode, pattern);
            List<Match> matchesList = matches.ToList();
            foreach(KeywordItem item in keywordItems)
            {
                //match.Groups[1] is declare type
                //match.Groups[2] is keywords
                Match m = matchesList.Find( o => o.Groups[2].Value.Contains(item.shaderKeywordName));
                if(m != null)
                {
                    item.shaderKeywordDeclareType = m.Groups[1].Value;
                }
            }
        }
    }

    //===================================================================================================

    public class KeywordItem
    {
        //shader
        public string shaderName;

        //snippet
        public string passType = "--";
        public string passName = "--";
        public string shaderType = "--";

        //compute kernel
        public string kernelName = "--";

        //data
        public string graphicsTier = "--";
        public string buildTarget = "--"; 
        public string shaderCompilerPlatform = "--";

        //data - PlatformKeywordSet
        public string platformKeywords = "--";

        //data - ShaderKeywordSet
        public string shaderKeywordName = "No Keyword / All Off";
        public string shaderKeywordType = "--";
        public string shaderKeywordDeclareType = "--";
        public bool isDynamic = false;

        //how many times this variant appears in IPreprocessShaders
        public int appearCount_before = 0;
        public int appearCount_after = 0;

        //Constructor for normal shader
        public KeywordItem(Shader shader, ShaderSnippetData snippet, ShaderCompilerData data, ShaderKeyword sk, bool defaultVariant)
        {
            shaderName = shader.name;
            passName = snippet.passName;
            passType = snippet.passType.ToString();
            shaderType = snippet.shaderType.ToString();
            graphicsTier = data.graphicsTier.ToString();
            buildTarget = data.buildTarget.ToString();
            shaderCompilerPlatform = data.shaderCompilerPlatform.ToString();
            platformKeywords = Helper.GetPlatformKeywordList(data.platformKeywordSet);

            if(!defaultVariant)
            {
                //shaderKeywordName
                LocalKeyword lkey = new LocalKeyword(shader,sk.name);
                isDynamic = lkey.isDynamic;
                shaderKeywordName = sk.name;
                
                //shaderKeywordType
                shaderKeywordType = ShaderKeyword.GetGlobalKeywordType(sk).ToString(); //""+sk[k].GetKeywordType().ToString();
                
                //Bug checking
                if( !sk.IsValid() )
                {
                    Debug.LogError("ShaderVariantTool error #E06. Shader "+shaderName+" Keyword "+shaderKeywordName+" is invalid.");
                }
                if( !data.shaderKeywordSet.IsEnabled(sk) )
                {
                    Debug.LogWarning("ShaderVariantTool error #E07. Shader "+shaderName+" Keyword "+shaderKeywordName+" is not enabled. You can create a custom shader stripping script to strip it.");
                }
            }
        }

        //Constructor for compute shader
        public KeywordItem(ComputeShader shader, string kernelname, ShaderCompilerData data, ShaderKeyword sk, bool defaultVariant)
        {
            shaderName = shader.name;
            kernelName = kernelname;
            graphicsTier = data.graphicsTier.ToString();
            buildTarget = data.buildTarget.ToString();
            shaderCompilerPlatform = data.shaderCompilerPlatform.ToString();
            platformKeywords = Helper.GetPlatformKeywordList(data.platformKeywordSet);

            if(!defaultVariant)
            {
                //shaderKeywordName
                LocalKeyword lkey = new LocalKeyword(shader,sk.name);
                isDynamic = lkey.isDynamic;
                shaderKeywordName = sk.name;
                
                //shaderKeywordType
                shaderKeywordType = ShaderKeyword.GetGlobalKeywordType(sk).ToString(); //""+sk[k].GetKeywordType().ToString();
                
                //Bug checking
                if( !sk.IsValid() )
                {
                    Debug.LogError("ShaderVariantTool error #E04. Shader "+shaderName+" Keyword "+shaderKeywordName+" is invalid.");
                }
                if( !data.shaderKeywordSet.IsEnabled(sk) )
                {
                    Debug.LogWarning("ShaderVariantTool error #E05. Shader "+shaderName+" Keyword "+shaderKeywordName+" is not enabled. You can create a custom shader stripping script to strip it.");
                }
            }
        }
    };
}
