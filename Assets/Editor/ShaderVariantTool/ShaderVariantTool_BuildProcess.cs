using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace GfxQA.ShaderVariantTool
{
    // BEFORE BUILD
    class ShaderVariantTool_BuildPreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 10; } }

        //Because of the new incremental build changes, 
        //Development build won't trigger OnShaderProcess - 1338940
        public static bool deletePlayerCacheBeforeBuild = true;
        public void DeletePlayerCacheBeforeBuild()
        {
            if(deletePlayerCacheBeforeBuild)
            {
                string path = Application.dataPath.Replace("Assets","")+"Library/PlayerDataCache";
                if( Directory.Exists(path) )
                {
                    Directory.Delete(path,true);
                }
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            DeletePlayerCacheBeforeBuild();
            SVL.ResetBuildList();
            SVL.buildTime = EditorApplication.timeSinceStartup;
        }
    }

    // AFTER BUILD
    class ShaderVariantTool_BuildPostprocess : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 10; } }

        public void OnPostprocessBuild(BuildReport report)
        {
            //For reading EditorLog, we can extract the contents
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, SVL.buildProcessIDTitleEnd+SVL.buildProcessID);

            //Because of the new incremental build changes, Development build won't trigger OnShaderProcess - 1338940
            if(SVL.shaderlist.Count == 0)
            {
                Debug.LogError("No shader or variant are logged. Please go to Window > ShaderVariantTool > turn ON Delete PlayerCache Before Build checkbox.");
            } 

            //Calculate shader count by type
            SVL.normalShaderCount = (uint)SVL.shaderlist.Count - SVL.computeShaderCount;
            SVL.variantFromShader = SVL.variantTotalCount - SVL.variantFromCompute;

            //Reading EditorLog
            string editorLogPath = Helper.GetEditorLogPath();
            ReadShaderCompileInfo(SVL.buildProcessIDTitleStart+SVL.buildProcessID, editorLogPath, false);

            //Calculate build time
            SVL.buildTime = EditorApplication.timeSinceStartup - SVL.buildTime;
            SVL.buildTimeString = Helper.TimeFormatString(SVL.buildTime);

            //Sort the results and make row data
            SVL.Sorting();

            //Prepare CSV string
            List<string[]> outputRows = new List<string[]>();

            //Get Unity & branch version
            string version_changeset = Convert.ToString(InternalEditorUtility.GetUnityRevision(), 16);
            string version_branch = InternalEditorUtility.GetUnityBuildBranch();
            string unity_version = Application.unityVersion +" "+ version_branch+" ("+version_changeset+")";

            //Get Graphics API list
            var gfxAPIsList = PlayerSettings.GetGraphicsAPIs(report.summary.platform);
            string gfxAPIs = ""; 
            for(int i=0; i<gfxAPIsList.Length; i++)
            {
                gfxAPIs += gfxAPIsList[i].ToString()+ " ";
            }

            //Build size
            var buildSize = report.summary.totalSize / 1024;

            //Write Overview Result
            outputRows.Add( new string[] { "Unity" , unity_version } );
            outputRows.Add( new string[] { "Platform" , report.summary.platform.ToString() } );
            outputRows.Add( new string[] { "Graphics API" , gfxAPIs } );
            outputRows.Add( new string[] { "Build Path" , report.summary.outputPath } );
            outputRows.Add( new string[] { "Build Size (Kb)" , buildSize.ToString() } );
            //outputRows.Add( new string[] { "Build Time (seconds)" , ""+report.summary.totalTime } );
            outputRows.Add( new string[] { "Build Time" , SVL.buildTimeString } );

            //Write Overview - Shader
            //outputRows.Add( new string[] { "Shaders ----------------------------------" } );
            outputRows.Add( new string[] { "Shader Count" , SVL.normalShaderCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count original" , SVL.variantOriginalCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count after Prefiltering" , SVL.variantAfterPrefilteringCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count after Builtin-Stripping" , SVL.variantAfterBuiltinStrippingCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count after Scriptable-Stripping" , SVL.variantFromShader+
            " (cached:" + SVL.variantInCache + " compiled:" + SVL.variantCompiledCount +")" } );
            outputRows.Add( new string[] { "Shader Dynamic Branch variant count" , SVL.shaderDynamicVariant.ToString() } );

            //Write Overview - Compute
            //outputRows.Add( new string[] { "ComputeShaders ----------------------------------" } );
            outputRows.Add( new string[] { "ComputeShader Count" , SVL.computeShaderCount.ToString() } );
            outputRows.Add( new string[] { "ComputeShader Variant Count in Build" , SVL.variantFromCompute.ToString() } );
            outputRows.Add( new string[] { "ComputeShader Dynamic Branch variant count" , SVL.computeDynamicVariant.ToString() } );
            outputRows.Add( new string[] { "" } );

            //Write Shader Result from Editor Log
            outputRows = AddEditorLogShaderInfo(outputRows, SVL.shaderlist);
            outputRows.Add( new string[] { "" } );

            //Write Each variant Result
            for(int i = 0; i < SVL.rowData.Count; i++)
            {
                outputRows.Add( SVL.rowData[i] );
            }

            //Save File
            string fileName = "ShaderVariants_"+DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
            ShaderVariantTool.savedFile = SaveCSVFile(outputRows, fileName);

            //CleanUp
            outputRows.Clear();

            //Let user know the tool has successfully done it's job
            Debug.Log("Build is done and ShaderVariantTool has done gathering data. Find the details on Windows > ShaderVariantTool, or look at the generated CSV report at: "+ShaderVariantTool.savedFile);

            SVL.buildProcessStarted = false;
        }

        public static List<string[]> AddEditorLogShaderInfo(List<string[]> outputRows, List<CompiledShader> shaderlist)
        {
            //Write Shader Result
            outputRows.Add( new string[] 
            { 
                "Shader", 
                "Variant Count original",
                "Variant Count after Prefiltering", 
                "Variant Count after Builtin-Stripping",
                "Variant after Scriptable-Stripping", 
                "Dynamic branch variants",
                "Variant in Cache",
                "Compiled Variant Count", 
                "Stripping+Compilation Time"
            } );

            //Compute shader do not have Editor log info
            //string computeMsg = "This is compute shader. Numbers for prefiltering and stripping is not available.";
            for(int i = 0; i <shaderlist.Count; i++)
            {
                outputRows.Add( new string[] 
                { 
                    shaderlist[i].name, 
                    shaderlist[i].isComputeShader? "-1" : shaderlist[i].editorLog_originalVariantCount.ToString(),
                    shaderlist[i].isComputeShader? "-1" : shaderlist[i].editorLog_prefilteredVariantCount.ToString(),
                    shaderlist[i].isComputeShader? "-1" : shaderlist[i].editorLog_builtinStrippedVariantCount.ToString(),
                    shaderlist[i].isComputeShader? shaderlist[i].noOfVariantsForThisShader.ToString() : shaderlist[i].editorLog_remainingVariantCount.ToString(),
                    shaderlist[i].dynamicVariantForThisShader.ToString(),
                    shaderlist[i].isComputeShader? "-1" : shaderlist[i].editorLog_variantInCacheCount.ToString(),
                    shaderlist[i].isComputeShader? "-1" : shaderlist[i].editorLog_compiledVariantCount.ToString(),
                    Helper.TimeFormatString( shaderlist[i].editorLog_totalProcessTime )
                } );
            }

            return outputRows;
        }

        public static string SaveCSVFile(List<string[]> outputRows, string fileName)
        {
            //Prepare CSV string
            int length = outputRows.Count;
            string delimiter = ",";
            StringBuilder sb = new StringBuilder();
            for (int index = 0; index < length; index++)
                sb.AppendLine(string.Join(delimiter, outputRows[index]));
            
            //Write to CSV file
            string savedFile = Helper.GetCSVFolderPath()+fileName+".csv";
            StreamWriter outStream = System.IO.File.CreateText(savedFile);
            outStream.WriteLine(sb);
            outStream.Close();

            //CleanUp
            outputRows.Clear();

            return savedFile;
        }

        public static List<CompiledShader> ReadShaderCompileInfo(string startTimeStamp, string editorLogPath, bool isLogReader)
        {
            //For making sure there is no bug
            uint variantCountinBuild = 0;
            uint skippedVariantsTotalCount = 0;

            //Read EditorLog
            string fromtext = startTimeStamp;
            string totext = startTimeStamp.Replace(SVL.buildProcessIDTitleStart,SVL.buildProcessIDTitleEnd);
            string currentLine = "";
            bool startFound = false;
            bool endFound = false;
            FileStream fs = new FileStream(editorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            List<CompiledShader> compiledShaderInfoFromEditorLog = new List<CompiledShader>();
            using (StreamReader sr = new StreamReader(fs))
            {
                while (!startFound)
                {
                    currentLine = sr.ReadLine();
                    if(currentLine.Contains(fromtext))
                    {
                        startFound = true;
                    }
                }
                while (!endFound)
                {
                    currentLine = sr.ReadLine();
                    if(currentLine.Contains(totext))
                    {
                        endFound = true;
                    }
                    else
                    {
                        if(currentLine.Contains("Compiling shader "))
                        {
                            //Shader name
                            string shaderName = Helper.ExtractString(currentLine, "Compiling shader " , " pass " );
                            shaderName = shaderName.Replace("\"","");

                            //Shader pass
                            string passName = Helper.ExtractString(currentLine, " pass " , "\" (" );
                            passName = passName.Replace("\"","");

                            //Shader program e.g. vert / frag
                            string programName = Helper.ExtractString(currentLine, passName+"\" (","");
                            programName = programName.Replace("\"","").Replace(")","");

                            //Sometime the log is like this
                            /*
                            Compiling shader "Universal Render Pipeline/Lit" pass "ForwardLit" (fp)
                            [2.97s] 100M / ~402M prepared
                            [11.34s] 200M / ~402M prepared
                            [19.22s] 300M / ~402M prepared
                            [27.65s] 400M / ~402M prepared
                                Full variant space:         402653184
                                After settings filtering:   402653184
                                After built-in stripping:   3145728
                                After scriptable stripping: 896
                                Processed in 28.45 seconds
                                starting compilation...
                                [ 61s] 360 / 896 variants ready
                                [123s] 537 / 896 variants ready
                                [189s] 660 / 896 variants ready
                                [254s] 795 / 896 variants ready
                                finished in 301.68 seconds. Local cache hits 8 (0.00s CPU time), remote cache hits 0 (0.00s CPU time), compiled 888 variants (4504.33s CPU time), skipped 0 variants
                                Prepared data for serialisation in 0.02s
                            */
                            while(!currentLine.Contains("Full variant space:"))
                            {
                                currentLine = sr.ReadLine();
                            }

                            //Full variant space (variant count before prefiltering & stripping)
                            //currentLine = sr.ReadLine();
                            uint totalVariantInt = 0;
                            if(currentLine.Contains("Full variant space:"))
                            {
                                string totalVariant = Helper.ExtractString(currentLine, "Full variant space:" , "" );
                                totalVariant = totalVariant.Replace(" ","");
                                totalVariantInt = uint.Parse(totalVariant);
                            }

                            //After settings filtering (variant count after prefiltering)
                            currentLine = sr.ReadLine();
                            uint prefilteredVariantInt = 0;
                            if(currentLine.Contains("After settings filtering:"))
                            {
                                string prefilteredVariant = Helper.ExtractString(currentLine, "After settings filtering:" , "" );
                                prefilteredVariant = prefilteredVariant.Replace(" ","");
                                prefilteredVariantInt = uint.Parse(prefilteredVariant);
                            }

                            //After builtin stripping
                            currentLine = sr.ReadLine();
                            uint builtinStrippedVariantInt = 0;
                            if(currentLine.Contains("After built-in stripping:"))
                            {
                                string builtinStrippedVariant = Helper.ExtractString(currentLine, "After built-in stripping:" , "" );
                                builtinStrippedVariant = builtinStrippedVariant.Replace(" ","");
                                builtinStrippedVariantInt = uint.Parse(builtinStrippedVariant);
                            }

                            //After scriptable stripping (final variant count in build)
                            currentLine = sr.ReadLine(); //After scriptable stripping:
                            uint remainingVariantInt = 0;
                            if(currentLine.Contains("After scriptable stripping:"))
                            {
                                string remainingVariant = Helper.ExtractString(currentLine, "After scriptable stripping:" , "" );
                                remainingVariant = remainingVariant.Replace(" ","");
                                remainingVariantInt = uint.Parse(remainingVariant);
                            }
                            else
                            {
                                Debug.LogError("Cannot find line [After scriptable stripping:]. CurrentLine = "+currentLine);
                            }

                            //remaining variant & stripping time
                            //string remainingVariant = Helper.ExtractString(currentLine, "" , " / " );
                            //int remainingVariantInt = int.Parse(remainingVariant);

                            //total variant
                            //string totalVariant = Helper.ExtractString(currentLine, "/ " , " variants" );
                            //totalVariant = totalVariant.Replace(" ","");
                            //int totalVariantInt = int.Parse(totalVariant);
                            
                            //stripping time
                            currentLine = sr.ReadLine();
                            string strippingTime = Helper.ExtractString(currentLine, "Processed in " , " seconds" );

                            //jump to line of compilation time
                            if(remainingVariantInt > 0)
                            {
                                currentLine = sr.ReadLine();
                                while (!currentLine.Contains("finished in ") || currentLine.Contains("variants ready"))
                                {
                                    currentLine = sr.ReadLine();
                                }
                            }
                            
                            //compilation time and compiled variant count (time is faster if there are cached variants)
                            string remainingText = currentLine;
                            string startString = "";
                            string endString = "";
                            string compileTime = "0.00";
                            string compiledVariants = "0";
                            string localCache = "0";
                            string remoteCache = "0";
                            string skippedVariants = "0";
                            if(remainingVariantInt > 0)
                            {
                                //Compile time
                                startString = "finished in ";
                                endString = " seconds. ";
                                compileTime = Helper.ExtractString(remainingText,startString,endString,false);
                                compileTime = compileTime.Replace(" ","");
                                remainingText = Helper.GetRemainingString(remainingText,endString);

                                //Local cache hit
                                startString = "Local cache hits ";
                                endString = " (";
                                localCache = Helper.ExtractString(remainingText,startString,endString,false);
                                localCache = localCache.Replace(" ","");
                                remainingText = Helper.GetRemainingString(remainingText,endString);

                                //Remote cache hit
                                startString = "remote cache hits ";
                                endString = " (";
                                remoteCache = Helper.ExtractString(remainingText,startString,endString,false);
                                remoteCache = remoteCache.Replace(" ","");
                                remainingText = Helper.GetRemainingString(remainingText,endString);

                                //Compiled variants
                                startString = "), compiled ";
                                endString = " variants (";
                                compiledVariants = Helper.ExtractString(remainingText,startString,endString,false);
                                compiledVariants = compiledVariants.Replace(" ","");
                                remainingText = Helper.GetRemainingString(remainingText,endString);

                                //Skipped variants
                                startString = " skipped ";
                                endString = " variants";
                                skippedVariants = Helper.ExtractString(remainingText,startString,endString,false);
                                skippedVariants = skippedVariants.Replace(" ","");
                                remainingText = Helper.GetRemainingString(remainingText,endString);
                            }
                            uint compiledVariantsInt = uint.Parse(compiledVariants);
                            uint localCacheInt = uint.Parse(localCache);
                            uint remoteCacheInt = uint.Parse(remoteCache);
                            uint skippedVariantsInt = uint.Parse(skippedVariants);
                            skippedVariantsTotalCount += skippedVariantsInt;
                            float strippingTimeFloat = float.Parse(strippingTime);
                            float compileTimeFloat = float.Parse(compileTime);
                            //---------- Temp object info ------------//
                            CompiledShader temp = new CompiledShader();
                            temp.isComputeShader = false;
                            temp.name = shaderName;
                            temp.editorLog_originalVariantCount = totalVariantInt;
                            temp.editorLog_prefilteredVariantCount = prefilteredVariantInt;
                            temp.editorLog_builtinStrippedVariantCount = builtinStrippedVariantInt;
                            temp.editorLog_compiledVariantCount = compiledVariantsInt;
                            temp.editorLog_totalProcessTime = strippingTimeFloat+compileTimeFloat;
                            temp.editorLog_remainingVariantCount = remainingVariantInt;
                            temp.editorLog_variantInCacheCount = localCacheInt+remoteCacheInt;
                            temp.dynamicVariantForThisShader = 0;
                            //---------- Add to temp list ------------//
                            int templistID = compiledShaderInfoFromEditorLog.IndexOf(compiledShaderInfoFromEditorLog.Find(x => x.name.Equals(temp.name)));
                            if(templistID == -1)
                            {
                                //Add a new shader record
                                compiledShaderInfoFromEditorLog.Add(temp);
                            }
                            else
                            {
                                //Add to existing shader record
                                compiledShaderInfoFromEditorLog[templistID] = AccumulateToCompiledShader(temp,compiledShaderInfoFromEditorLog[templistID]);
                            }

                            //Bug checking
                            if(remainingVariantInt != compiledVariantsInt + temp.editorLog_variantInCacheCount + skippedVariantsInt)
                            {
                                string debugText = "Shader: "+shaderName +" Pass: "+ passName +" Program: "+ programName + "\n";
                                debugText += "Remaining "+remainingVariantInt +" != compiled "+compiledVariantsInt+" + cache "+ temp.editorLog_variantInCacheCount + "\n";
                                debugText += "This is a bug. Please contact @mingwai on slack and provide Editor.log";
                                Debug.LogError(debugText);
                            }
                            //Debug
                            // if(shaderName == "Universal Render Pipeline/Lit")
                            // {
                            //     string debugText = shaderName +"-"+ passName +"-"+ programName + "\n";
                            //     debugText += totalVariant +"-"+ remainingVariant +"-time-"+ strippingTime + "\n";
                            //     debugText += compileTime +"-"+ compiledVariants + "\n";
                            //     DebugLog(debugText);    
                            // }
                        }
                    }
                }
            }

            if(!isLogReader)
            {
                for(int i=0; i<compiledShaderInfoFromEditorLog.Count;i++)
                {
                    int listID = SVL.shaderlist.IndexOf(SVL.shaderlist.Find(x => x.name.Equals(compiledShaderInfoFromEditorLog[i].name)));
                    SVL.shaderlist[listID] = AccumulateToCompiledShader(compiledShaderInfoFromEditorLog[i],SVL.shaderlist[listID]);
                    //---------- For total countinvariantInCache ------------//
                    SVL.variantOriginalCount += compiledShaderInfoFromEditorLog[i].editorLog_originalVariantCount;
                    SVL.variantAfterPrefilteringCount +=  compiledShaderInfoFromEditorLog[i].editorLog_prefilteredVariantCount;
                    SVL.variantAfterBuiltinStrippingCount += compiledShaderInfoFromEditorLog[i].editorLog_builtinStrippedVariantCount;
                    SVL.variantCompiledCount += compiledShaderInfoFromEditorLog[i].editorLog_compiledVariantCount;
                    SVL.variantInCache += compiledShaderInfoFromEditorLog[i].editorLog_variantInCacheCount;
                    variantCountinBuild += compiledShaderInfoFromEditorLog[i].editorLog_remainingVariantCount; //for making sure no bug
                }

                //Bug check - in case my codes in OnProcessShader VS reading EditorLog counts different result
                if( SVL.variantFromShader != variantCountinBuild)
                {
                    Debug.LogError("ShaderVariantTool error. "+
                    "Tool counted there are "+SVL.variantFromShader+" shader variants in build, "+
                    "but Editor Log counted "+variantCountinBuild+". Please contact @mingwai on slack.");
                }
                uint variantCacheAndCompiledSum = SVL.variantInCache + SVL.variantCompiledCount + skippedVariantsTotalCount;
                if( variantCacheAndCompiledSum != variantCountinBuild)
                {
                    Debug.LogError("ShaderVariantTool error. "+
                    "The sum of shader variants in EditorLog ("+variantCacheAndCompiledSum+") is not equal to the sum of shader variants collected by ShaderVariantTool ("+
                    variantCountinBuild+"). Please contact @mingwai on slack. This could be related to exisiting known issue: Case 1389276");
                }

                //Print invalid / disabled keyword error
                if(SVL.invalidKey != "") Debug.LogError("Some shader keywords are invalid: "+SVL.invalidKey);
                if(SVL.disabledKey != "") Debug.LogWarning("Some shader keywords are disabled but they are not being stripped: "+SVL.disabledKey);
            }

            return compiledShaderInfoFromEditorLog;
        }

        private static CompiledShader AccumulateToCompiledShader(CompiledShader src, CompiledShader dst)
        {
            dst.editorLog_builtinStrippedVariantCount += src.editorLog_builtinStrippedVariantCount;
            dst.editorLog_prefilteredVariantCount += src.editorLog_prefilteredVariantCount;
            dst.editorLog_originalVariantCount += src.editorLog_originalVariantCount;
            dst.editorLog_compiledVariantCount += src.editorLog_compiledVariantCount;
            dst.editorLog_totalProcessTime += src.editorLog_totalProcessTime;
            dst.editorLog_remainingVariantCount += src.editorLog_remainingVariantCount;
            dst.editorLog_variantInCacheCount += src.editorLog_variantInCacheCount;
            dst.dynamicVariantForThisShader += src.dynamicVariantForThisShader;

            return dst;
        }
    }
}