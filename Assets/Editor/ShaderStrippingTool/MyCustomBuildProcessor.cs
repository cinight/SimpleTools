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

class MyCustomBuildProcessor : IPreprocessShaders
{
    public MyCustomBuildProcessor()
    {
        SVL.list.Clear();
        SVL.variantCount = 0;
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
                SVL.variantCount++;

                //Just to verify API is correct
                string globalShaderKeywordName = ShaderKeyword.GetGlobalKeywordName(sk[k]);
                if( !isLocal && globalShaderKeywordName != ShaderKeyword.GetKeywordName(shader,sk[k]) ) Debug.Log("Bug. ShaderKeyword.GetGlobalKeywordName() and  ShaderKeyword.GetKeywordName() is wrong");
                ShaderKeywordType globalShaderKeywordType = ShaderKeyword.GetGlobalKeywordType(sk[k]);
                if( !isLocal && globalShaderKeywordType != ShaderKeyword.GetKeywordType(shader,sk[k]) ) Debug.Log("Bug. ShaderKeyword.GetGlobalKeywordType() and  ShaderKeyword.GetKeywordType() is wrong");
            }
        }
    }
}

class ShaderVariantTool_BuildPreprocess : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 10; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        SVL.buildTime = EditorApplication.timeSinceStartup;
    }
}

class MyCustomBuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 10; } }

    public void OnPostprocessBuild(BuildReport report)
    {
        //Calculate build time
        SVL.buildTime = EditorApplication.timeSinceStartup - SVL.buildTime;

        //Sort the results and make row data
        SVL.Sorting();

        //Write to CSV file
        //ShaderStrippingTool.savedFile = "";
        ShaderStrippingTool.folderPath = Application.dataPath.Replace("/Assets","/");
        string[][] output = new string[SVL.rowData.Count][];
        for(int i = 0; i < output.Length; i++)
        {
            output[i] = SVL.rowData[i];
        }
        int length = output.GetLength(0);
        string delimiter = ",";
        StringBuilder sb = new StringBuilder();
        for (int index = 0; index < length; index++)
            sb.AppendLine(string.Join(delimiter, output[index]));
        ShaderStrippingTool.savedFile = ShaderStrippingTool.folderPath+"ShaderVariants_"+DateTime.Now.ToString("yyyyMMdd_hh-mm-ss")+".csv";
        StreamWriter outStream = System.IO.File.CreateText(ShaderStrippingTool.savedFile);
        outStream.WriteLine(sb);
        outStream.Close();

        // TO DO - read the editor log shader compiled info
        //Debug.Log("MyCustomBuildProcessor.OnPostprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
    }
}