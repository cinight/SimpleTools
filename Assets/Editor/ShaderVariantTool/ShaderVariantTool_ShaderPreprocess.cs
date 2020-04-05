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

class ShaderVariantTool_ShaderPreprocess : IPreprocessShaders
{
    public ShaderVariantTool_ShaderPreprocess()
    {
        SVL.shaderlist.Clear();
        SVL.variantlist.Clear();
        SVL.compiledTotalCount = 0;
        SVL.variantTotalCount = 0;
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
                scv_default.passName = ""+snippet.passName;
                scv_default.passType = ""+snippet.passType.ToString();
                scv_default.shaderType = ""+snippet.shaderType.ToString();
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

class ShaderVariantTool_BuildPreprocess : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 10; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        SVL.buildTime = EditorApplication.timeSinceStartup;
    }
}

class ShaderVariantTool_BuildPostprocess : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 10; } }

    public void OnPostprocessBuild(BuildReport report)
    {
        //Calculate build time
        SVL.buildTime = EditorApplication.timeSinceStartup - SVL.buildTime;

        //Sort the results and make row data
        SVL.Sorting();

        //Prepare CSV string
        List<string[]> outputRows = new List<string[]>();

        //Write Overview Result
        outputRows.Add( new string[] { "Build Time (seonds)" , SVL.buildTime.ToString("0.000") } );
        outputRows.Add( new string[] { "Shader Count" , "" + SVL.shaderlist.Count } );
        outputRows.Add( new string[] { "Total Variant Count" , ""+SVL.variantTotalCount } );
        //outputRows.Add( new string[] { "Total Data Count" , ""+SVL.compiledTotalCount } );
        outputRows.Add( new string[] { "" } );

        //Write Shader Result
        outputRows.Add( new string[] { "Shader" , "Variant Count" } );
        for(int i = 0; i < SVL.shaderlist.Count; i++)
        {
            outputRows.Add( new string[] { SVL.shaderlist[i].name , ""+SVL.shaderlist[i].noOfVariantsForThisShader } );
        }
        outputRows.Add( new string[] { "" } );

        //Write Each variant Result
        for(int i = 0; i < SVL.rowData.Count; i++)
        {
            outputRows.Add( SVL.rowData[i] );
        }

        //Prepare CSV string
        int length = outputRows.Count;
        string delimiter = ",";
        StringBuilder sb = new StringBuilder();
        for (int index = 0; index < length; index++)
            sb.AppendLine(string.Join(delimiter, outputRows[index]));
        
        //Write to CSV file
        ShaderVariantTool.folderPath = Application.dataPath.Replace("/Assets","/");
        ShaderVariantTool.savedFile = ShaderVariantTool.folderPath+"ShaderVariants_"+DateTime.Now.ToString("yyyyMMdd_hh-mm-ss")+".csv";
        StreamWriter outStream = System.IO.File.CreateText(ShaderVariantTool.savedFile);
        outStream.WriteLine(sb);
        outStream.Close();

        //CleanUp
        outputRows.Clear();

        // TO DO - read the editor log shader compiled info
        //Debug.Log("MyCustomBuildProcessor.OnPostprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
    }
}