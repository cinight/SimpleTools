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
            SVL.normalShaderCount = SVL.shaderlist.Count - SVL.computeShaderCount;
            SVL.variantFromShader = SVL.variantTotalCount - SVL.variantFromCompute;

            //Reading EditorLog
            ReadShaderCompileInfo();

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
            outputRows.Add( new string[] { "Shader Variant Count before Stripping" , SVL.variantBeforeStrippingCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count in Build" , SVL.variantFromShader+
            " (cached:" + SVL.variantInCache + " compiled:" + SVL.variantCompiledCount +")" } );

            //Write Overview - Compute
            //outputRows.Add( new string[] { "ComputeShaders ----------------------------------" } );
            outputRows.Add( new string[] { "ComputeShader Count" , SVL.computeShaderCount.ToString() } );
            outputRows.Add( new string[] { "ComputeShader Variant Count in Build" , SVL.variantFromCompute.ToString() } );
            
            //outputRows.Add( new string[] { "Total Data Count" , ""+SVL.compiledTotalCount } );
            outputRows.Add( new string[] { "" } );

            //Write Shader Result
            outputRows.Add( new string[] 
            { 
                "Shader", 
                "Variant Count before Stripping", 
                "Variant in Cache",
                "Compiled Variant Count", 
                "Variant Count in Build",
                "Stripping+Compilation Time"
            } );
            for(int i = 0; i < SVL.shaderlist.Count; i++)
            {
                outputRows.Add( new string[] 
                { 
                    SVL.shaderlist[i].name, 
                    SVL.shaderlist[i].editorLog_originalVariantCount.ToString(),
                    SVL.shaderlist[i].editorLog_variantInCacheCount.ToString(),
                    SVL.shaderlist[i].editorLog_compiledVariantCount.ToString(),
                    SVL.shaderlist[i].noOfVariantsForThisShader.ToString(),
                    //SVL.shaderlist[i].editorLog_renamingVariantCount.ToString(),
                    Helper.TimeFormatString( SVL.shaderlist[i].editorLog_totalProcessTime )
                } );
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

            //Let user know the tool has successfully done it's job
            Debug.Log("Build is done and ShaderVariantTool has done gathering data. Find the details on Windows > ShaderVariantTool, or look at the generated CSV reort at: "+ShaderVariantTool.savedFile);

            SVL.buildProcessStarted = false;
        }

        //[MenuItem("ShaderVariantTool/Debug/TestEditorLog")]
        private void ReadShaderCompileInfo()
        {
            //For making sure there is no bug
            int variantCountinBuild = 0;

            //Decide EditorLog path
            string editorLogPath = "";
            switch(Application.platform)
            {
                case RuntimePlatform.WindowsEditor: editorLogPath=Environment.GetEnvironmentVariable("AppData").Replace("Roaming","")+"Local\\Unity\\Editor\\Editor.log"; break;
                case RuntimePlatform.OSXEditor: editorLogPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library")+"/Logs/Unity/Editor.log"; break;
                case RuntimePlatform.LinuxEditor: editorLogPath="~/.config/unity3d/Editor.log"; break;
            }

            //Read EditorLog
            string fromtext = SVL.buildProcessIDTitleStart+SVL.buildProcessID;
            string totext = SVL.buildProcessIDTitleEnd+SVL.buildProcessID;
            string currentLine = "";
            bool startFound = false;
            bool endFound = false;
            FileStream fs = new FileStream(editorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

                            //skip the line bla bla bla prepared
                            currentLine = sr.ReadLine();
                            if (currentLine.Contains("prepared") && !currentLine.Contains("variants, prepared"))
                            {
                                currentLine = sr.ReadLine();
                            }

                            //remaining variant & stripping time
                            string remainingVariant = Helper.ExtractString(currentLine, "" , " / " );
                            int remainingVariantInt = int.Parse(remainingVariant);

                            //total variant
                            string totalVariant = Helper.ExtractString(currentLine, "/ " , " variants" );
                            totalVariant = totalVariant.Replace(" ","");
                            int totalVariantInt = int.Parse(totalVariant);
                            
                            //stripping time
                            string strippingTime = Helper.ExtractString(currentLine, "variants left after stripping, processed in " , " seconds" );

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
                            }
                            int compiledVariantsInt = int.Parse(compiledVariants);
                            int localCacheInt = int.Parse(localCache);
                            int remoteCacheInt = int.Parse(remoteCache);
                            float strippingTimeFloat = float.Parse(strippingTime);
                            float compileTimeFloat = float.Parse(compileTime);

                            //---------- Add to ShaderList ------------//
                            int listID = SVL.shaderlist.IndexOf(SVL.shaderlist.Find(x => x.name.Equals(shaderName)));
                            CompiledShader temp = SVL.shaderlist[listID];
                            temp.editorLog_originalVariantCount += totalVariantInt;
                            temp.editorLog_compiledVariantCount += compiledVariantsInt;
                            temp.editorLog_totalProcessTime += strippingTimeFloat+compileTimeFloat;
                            temp.editorLog_remainingVariantCount += remainingVariantInt;
                            temp.editorLog_variantInCacheCount += localCacheInt+remoteCacheInt;
                            SVL.shaderlist[listID] = temp;
                            //---------- For total countinvariantInCacheg ------------//
                            SVL.variantBeforeStrippingCount += totalVariantInt;
                            SVL.variantCompiledCount += compiledVariantsInt;
                            SVL.variantInCache += localCacheInt+remoteCacheInt;
                            variantCountinBuild += remainingVariantInt; //for making sure no bug
                            
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

            //Bug check - in case my codes in OnProcessShader VS reading EditorLog counts different result
            if( SVL.variantFromShader != variantCountinBuild)
            {
                Debug.LogError("ShaderVariantTool error. "+
                "Tool counted there are "+SVL.variantFromShader+" shader variants in build, "+
                "but Editor Log counted "+variantCountinBuild+". Please contact @mingwai on slack.");
            }
            int variantCacheAndCompiledSum = SVL.variantInCache + SVL.variantCompiledCount;
            if( variantCacheAndCompiledSum != variantCountinBuild)
            {
                Debug.LogError("ShaderVariantTool error. "+
                "The sum of "+SVL.variantInCache+" variants in cache + "+
                SVL.variantCompiledCount+" variants compiled "+variantCacheAndCompiledSum+" variants is not equal to the accumulated sum "+
                variantCountinBuild+" variants. Please contact @mingwai on slack. This could be related to exisiting known issue: Case 1389276");
            }
        }
    }
}