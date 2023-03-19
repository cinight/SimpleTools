using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            //This should be always on
            ShaderVariantTool_BuildPreprocess.deletePlayerCacheBeforeBuild = true;
            DeletePlayerCacheBeforeBuild();

            //IPreprocessShaders happens before IPreprocessBuildWithReport, 
            //so doing the initial time logging in ShaderVariantTool_ShaderPreprocess instead
            SVL.ResetBuildList();
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

            //Reading EditorLog
            string editorLogPath = Helper.GetEditorLogPath();

            //Write File
            var outputRows = WriteCSVFile(report, editorLogPath, SVL.buildProcessIDTitleStart+SVL.buildProcessID);

            //Save File
            string fileName = "ShaderVariants_"+SVL.buildProcessID;//DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
            string savedFile = SaveCSVFile(outputRows, fileName);

            //CleanUp
            outputRows.Clear();

            //Let user know the tool has successfully done it's job
            Debug.Log("Build is done and ShaderVariantTool has done gathering data. Find the details on Windows > ShaderVariantTool, or look at the generated CSV report at: "+savedFile);

            SVL.buildProcessStarted = false;
        }

        public static List<string[]> WriteCSVFile(BuildReport report, string editorLogPath, string startTimeStamp)
        {
            ReadShaderCompileInfo(startTimeStamp, editorLogPath, false);

            //Prepare CSV string
            List<string[]> outputRows = new List<string[]>();

            //========================================

            if(report != null)
            {
                //Unity & branch version
                string version_changeset = Convert.ToString(InternalEditorUtility.GetUnityRevision(), 16);
                string version_branch = InternalEditorUtility.GetUnityBuildBranch();
                string unity_version = Application.unityVersion +" "+ version_branch+" ("+version_changeset+")";
                outputRows.Add( new string[] { "Unity" , unity_version } );

                //Platform
                outputRows.Add( new string[] { "Platform" , report.summary.platform.ToString() } );

                //Get Graphics API list
                var gfxAPIsList = PlayerSettings.GetGraphicsAPIs(report.summary.platform);
                string gfxAPIs = ""; 
                for(int i=0; i<gfxAPIsList.Length; i++)
                {
                    gfxAPIs += gfxAPIsList[i].ToString()+ " ";
                }
                outputRows.Add( new string[] { "Graphics API" , gfxAPIs } );

                //Build path
                outputRows.Add( new string[] { "Build Path" , report.summary.outputPath } );

                //Build size
                var buildSize = report.summary.totalSize / 1024;
                outputRows.Add( new string[] { "Build Size (Kb)" , buildSize.ToString() } );

                //Build time (report.summary.totalTime is not accurate)
                SVL.buildTime = EditorApplication.timeSinceStartup - SVL.buildTime;
                string buildTimeString = Helper.TimeFormatString(SVL.buildTime);
                outputRows.Add( new string[] { "Build Time" , buildTimeString } );

                //Set keywords declaration types
                foreach(ShaderItem si in SVL.shaderlist)
                {
                    si.SetKeywordDeclareType();
                }
            }

            //Calculate total shader numbers
            ShaderItem totalNormalShader = new ShaderItem();
            uint totalNormalShaderCount = 0;
            uint totalProgramInternalCount = 0;
            uint totalProgramUniqueCount = 0;
            ShaderItem totalComputeShader = new ShaderItem(); totalComputeShader.isComputeShader = true;
            uint totalComputeShaderCount = 0;
            foreach(ShaderItem si in SVL.shaderlist)
            {
                //For bug checking
                uint shaderInternalProgramCount = 0;

                if(si.isComputeShader)
                {
                    totalComputeShaderCount ++;

                    totalComputeShader.count_variant_before += si.count_variant_before;
                    totalComputeShader.count_dynamicVariant_before += si.count_dynamicVariant_before;
                    totalComputeShader.count_variant_after += si.count_variant_after;
                    totalComputeShader.count_dynamicVariant_after += si.count_dynamicVariant_after;

                    totalComputeShader.editorLog_variantOriginalCount += si.editorLog_variantOriginalCount;
                    totalComputeShader.editorLog_variantAfterPrefilteringCount += si.editorLog_variantAfterPrefilteringCount;
                    totalComputeShader.editorLog_variantAfterBuiltinStrippingCount += si.editorLog_variantAfterBuiltinStrippingCount;
                    totalComputeShader.editorLog_variantAfterSciptableStrippingCount += si.editorLog_variantAfterSciptableStrippingCount;
                    totalComputeShader.editorLog_variantCompiledCount += si.editorLog_variantCompiledCount;
                    totalComputeShader.editorLog_variantInCache += si.editorLog_variantInCache;
                    totalComputeShader.editorLog_timeCompile += si.editorLog_timeCompile;
                    totalComputeShader.editorLog_timeStripping += si.editorLog_timeStripping;
                }
                else
                {
                    totalNormalShaderCount ++;

                    totalNormalShader.count_variant_before += si.count_variant_before;
                    totalNormalShader.count_dynamicVariant_before += si.count_dynamicVariant_before;
                    totalNormalShader.count_variant_after += si.count_variant_after;
                    totalNormalShader.count_dynamicVariant_after += si.count_dynamicVariant_after;

                    totalNormalShader.editorLog_variantOriginalCount += si.editorLog_variantOriginalCount;
                    totalNormalShader.editorLog_variantAfterPrefilteringCount += si.editorLog_variantAfterPrefilteringCount;
                    totalNormalShader.editorLog_variantAfterBuiltinStrippingCount += si.editorLog_variantAfterBuiltinStrippingCount;
                    totalNormalShader.editorLog_variantAfterSciptableStrippingCount += si.editorLog_variantAfterSciptableStrippingCount;
                    totalNormalShader.editorLog_variantCompiledCount += si.editorLog_variantCompiledCount;
                    totalNormalShader.editorLog_variantInCache += si.editorLog_variantInCache;
                    totalNormalShader.editorLog_timeCompile += si.editorLog_timeCompile;
                    totalNormalShader.editorLog_timeStripping += si.editorLog_timeStripping;


                    foreach(ShaderProgram pgm in si.programs)
                    {
                        shaderInternalProgramCount += pgm.count_internal;
                        totalProgramInternalCount += pgm.count_internal;
                        totalProgramUniqueCount += pgm.count_unique;
                    }
                }

                //Bug Check - if the shader has errors, then the program count drops, the tool should print out error message to warn user too
                if( !si.isComputeShader && shaderInternalProgramCount != si.editorLog_variantAfterSciptableStrippingCount)
                {
                    Debug.LogError("ShaderVariantTool error #E08. "+
                    "Shader "+si.name+" has "+si.editorLog_variantAfterSciptableStrippingCount+" variants after ScriptableStripping, "+
                    "but Internal Program Count is not having same number: "+shaderInternalProgramCount+". "+"This might due to this shader has errors, so please check console for the shader errors.");
                }
            }

            //Bug check - in case the variants tracked in OnProcessShader VS reading EditorLog counts different result
            if( totalNormalShader.count_variant_after != totalNormalShader.editorLog_variantAfterSciptableStrippingCount )
            {
                Debug.LogError("ShaderVariantTool error #E01. "+
                "Tool counted there are "+totalNormalShader.count_variant_after+" shader variants in build, "+
                "but Editor Log counted "+totalNormalShader.editorLog_variantAfterSciptableStrippingCount+".");
            }
            if( totalNormalShader.count_variant_before != totalNormalShader.editorLog_variantAfterBuiltinStrippingCount )
            {
                Debug.LogError("ShaderVariantTool error #E02. "+
                "Tool counted there are "+totalNormalShader.count_variant_before+" variants before striptable-stripping, "+
                "but Editor Log counted "+totalNormalShader.editorLog_variantAfterBuiltinStrippingCount+".");
            }

            //Stripping and compile time
            string strippingTimeString = Helper.TimeFormatString(totalNormalShader.editorLog_timeStripping);
            string compileTimeString = Helper.TimeFormatString(totalNormalShader.editorLog_timeCompile);
            outputRows.Add( new string[] { "- Stripping Time" , strippingTimeString } );
            outputRows.Add( new string[] { "- Compilation Time" , compileTimeString } );

            //Shader counts
            outputRows.Add( new string[] { "Shader Count" , totalNormalShaderCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count original" , totalNormalShader.editorLog_variantOriginalCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count after Prefiltering" , totalNormalShader.editorLog_variantAfterPrefilteringCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count after Builtin-Stripping" , totalNormalShader.editorLog_variantAfterBuiltinStrippingCount.ToString() } );
            outputRows.Add( new string[] { "Shader Variant Count after Scriptable-Stripping" , totalNormalShader.editorLog_variantAfterSciptableStrippingCount.ToString() } );
            outputRows.Add( new string[] { "- Cached:" , totalNormalShader.editorLog_variantInCache.ToString() } );
            outputRows.Add( new string[] { "- Compiled:" , totalNormalShader.editorLog_variantCompiledCount.ToString() } );
            outputRows.Add( new string[] { "Shader Dynamic Branch variant count" , totalNormalShader.count_dynamicVariant_after.ToString() } );
            outputRows.Add( new string[] { "Shader Program Count" , "" } );
            outputRows.Add( new string[] { "- Internal:" , totalProgramInternalCount.ToString() } );
            outputRows.Add( new string[] { "- Unique:" , totalProgramUniqueCount.ToString() } );

            //Compute counts
            outputRows.Add( new string[] { "ComputeShader Count" , totalComputeShaderCount.ToString() } );
            outputRows.Add( new string[] { "Compute Variant Count after Builtin-Stripping" , totalComputeShader.count_variant_before.ToString() } );
            outputRows.Add( new string[] { "Compute Variant Count after Scriptable-Stripping" , totalComputeShader.count_variant_after.ToString() } );
            outputRows.Add( new string[] { "Compute Dynamic Branch variant count" , totalComputeShader.count_dynamicVariant_after.ToString() } );
            outputRows.Add( new string[] { "" } );

            //Shader table //remember to match the order & title in Main.uxml
            List<string> shaderTableHeaderRow = new List<string>();
            shaderTableHeaderRow.Add("Shader Name");
            shaderTableHeaderRow.Add("Original");
            shaderTableHeaderRow.Add("After Prefiltering");
            shaderTableHeaderRow.Add("After Builtin-Stripping");
            shaderTableHeaderRow.Add("After Scriptable-Stripping");
            shaderTableHeaderRow.Add("Dynamic Branch");
            shaderTableHeaderRow.Add("Variant in Cache");
            shaderTableHeaderRow.Add("Compiled Variant Count");
            shaderTableHeaderRow.Add("Stripping Time");
            shaderTableHeaderRow.Add("Compilation Time");
            shaderTableHeaderRow.Add("IsComputeShader");
            //Additional header due to per-graphics API program count
            foreach(ShaderItem si in SVL.shaderlist)
            {
                foreach(ShaderProgram pgm in si.programs)
                {
                    string program_header = "Program Count "+pgm.gfxAPI;

                    int shaderTableColumnId = shaderTableHeaderRow.FindIndex(e => e == program_header);
                    if(shaderTableColumnId == -1)
                    {
                        shaderTableHeaderRow.Add(program_header);
                    }
                }
            }
            outputRows.Add(shaderTableHeaderRow.ToArray());
            //Fill the table
            foreach(ShaderItem si in SVL.shaderlist)
            {
                if(si.isComputeShader)
                {
                    string[] shaderTableItemRow = new string[shaderTableHeaderRow.Count];
                    shaderTableItemRow[0] = si.name;
                    shaderTableItemRow[1] = "-1"; //Compute shader do not have Editor log original, prefilter counts
                    shaderTableItemRow[2] = "-1";
                    shaderTableItemRow[3] = si.count_variant_before.ToString();
                    shaderTableItemRow[4] = si.count_variant_after.ToString();
                    shaderTableItemRow[5] = si.count_dynamicVariant_after.ToString();
                    shaderTableItemRow[6] = si.editorLog_variantInCache.ToString();
                    shaderTableItemRow[7] = si.editorLog_variantCompiledCount.ToString();
                    shaderTableItemRow[8] = Helper.TimeFormatString( si.editorLog_timeStripping );
                    shaderTableItemRow[9] = Helper.TimeFormatString( si.editorLog_timeCompile );
                    shaderTableItemRow[10] = "True";
                    for(int k=11; k<shaderTableHeaderRow.Count; k++)
                    {
                        shaderTableItemRow[k] = "N/A";
                    }
                    outputRows.Add(shaderTableItemRow);
                }
                else
                {
                    string[] shaderTableItemRow = new string[shaderTableHeaderRow.Count];
                    shaderTableItemRow[0] = si.name;
                    shaderTableItemRow[1] = si.editorLog_variantOriginalCount.ToString();
                    shaderTableItemRow[2] = si.editorLog_variantAfterPrefilteringCount.ToString();
                    shaderTableItemRow[3] = si.editorLog_variantAfterBuiltinStrippingCount.ToString();
                    shaderTableItemRow[4] = si.editorLog_variantAfterSciptableStrippingCount.ToString();
                    shaderTableItemRow[5] = si.count_dynamicVariant_after.ToString();
                    shaderTableItemRow[6] = si.editorLog_variantInCache.ToString();
                    shaderTableItemRow[7] = si.editorLog_variantCompiledCount.ToString();
                    shaderTableItemRow[8] = Helper.TimeFormatString( si.editorLog_timeStripping );
                    shaderTableItemRow[9] = Helper.TimeFormatString( si.editorLog_timeCompile );
                    shaderTableItemRow[10] = "False";
                    foreach(ShaderProgram pgm in si.programs)
                    {
                        int shaderTableColumnId = shaderTableHeaderRow.FindIndex(e => e == "Program Count "+pgm.gfxAPI);
                        if(shaderTableColumnId != -1)
                        {
                            shaderTableItemRow[shaderTableColumnId] = "internal: "+pgm.count_internal+" | "+"unique: "+pgm.count_unique;
                        }
                    }
                    outputRows.Add(shaderTableItemRow);
                }
            }
            outputRows.Add( new string[] { "" } );

            //Variant Table Column header
            SVL.ReadUXMLForColumns();
            outputRows.Add(SVL.columns.ToArray());

            //Variant Table
            foreach(ShaderItem si in SVL.shaderlist)
            {
                //Each keyword item
                si.keywordItems = si.keywordItems.OrderByDescending(o=>o.appearCount_after).ToList();//.ThenBy(o=>o.shaderKeywordName)
                for(int k=0; k < si.keywordItems.Count; k++)
                {
                    KeywordItem ki = si.keywordItems[k];
                    outputRows.Add(new string[] 
                    {
                        ki.shaderName,
                        ki.appearCount_before.ToString(),
                        ki.appearCount_after.ToString(),
                        ki.shaderKeywordName,
                        ki.shaderKeywordDeclareType,
                        ki.passName,
                        ki.shaderType,
                        ki.passType,
                        ki.kernelName,
                        ki.graphicsTier,
                        ki.buildTarget,
                        ki.shaderCompilerPlatform,
                        ki.platformKeywords,
                        ki.shaderKeywordType,
                    });
                }
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

        public static void ReadShaderCompileInfo(string startTimeStamp, string editorLogPath, bool isLogReader)
        {
            //For bug checking
            UInt64 sum_variantCountinBuild = 0;
            UInt64 sum_variantCountinCache = 0;
            UInt64 sum_variantCountCompiled = 0;
            UInt64 sum_variantCountSkipped = 0;

            //Read EditorLog
            string fromtext = startTimeStamp;
            string totext = startTimeStamp.Replace(SVL.buildProcessIDTitleStart,SVL.buildProcessIDTitleEnd);
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
                    if(!currentLine.Contains("Compiling shader "))
                    {
                        currentLine = sr.ReadLine();
                    }

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
                            UInt64 totalVariantInt = 0;
                            if(currentLine.Contains("Full variant space:"))
                            {
                                string totalVariant = Helper.ExtractString(currentLine, "Full variant space:" , "" );
                                totalVariant = totalVariant.Replace(" ","");
                                totalVariantInt = UInt64.Parse(totalVariant);
                            }

                            //After settings filtering (variant count after prefiltering)
                            currentLine = sr.ReadLine();
                            UInt64 prefilteredVariantInt = 0;
                            if(currentLine.Contains("After settings filtering:"))
                            {
                                string prefilteredVariant = Helper.ExtractString(currentLine, "After settings filtering:" , "" );
                                prefilteredVariant = prefilteredVariant.Replace(" ","");
                                prefilteredVariantInt = UInt64.Parse(prefilteredVariant);
                            }

                            //After builtin stripping
                            currentLine = sr.ReadLine();
                            UInt64 builtinStrippedVariantInt = 0;
                            if(currentLine.Contains("After built-in stripping:"))
                            {
                                string builtinStrippedVariant = Helper.ExtractString(currentLine, "After built-in stripping:" , "" );
                                builtinStrippedVariant = builtinStrippedVariant.Replace(" ","");
                                builtinStrippedVariantInt = UInt64.Parse(builtinStrippedVariant);
                            }

                            //After scriptable stripping (final variant count in build)
                            currentLine = sr.ReadLine(); //After scriptable stripping:
                            UInt64 remainingVariantInt = 0;
                            if(currentLine.Contains("After scriptable stripping:"))
                            {
                                string remainingVariant = Helper.ExtractString(currentLine, "After scriptable stripping:" , "" );
                                remainingVariant = remainingVariant.Replace(" ","");
                                remainingVariantInt = UInt64.Parse(remainingVariant);
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
                            float strippingTimeFloat = float.Parse(strippingTime);
                            float compileTimeFloat = float.Parse(compileTime);

                            //---------- Log ShaderItem ------------//
                            int shaderItemId = SVL.shaderlist.FindIndex( o=> o.name == shaderName );
                            ShaderItem shaderItem;
                            if( shaderItemId == -1 )
                            {
                                //Make new ShaderItem
                                shaderItem = new ShaderItem();
                                shaderItem.name = shaderName;
                                SVL.shaderlist.Add(shaderItem);
                            }
                            else
                            {
                                //Get existing ShaderItem
                                shaderItem = SVL.shaderlist[shaderItemId];
                            }
                            shaderItem.editorLog_variantOriginalCount += totalVariantInt;
                            shaderItem.editorLog_variantAfterPrefilteringCount += prefilteredVariantInt;
                            shaderItem.editorLog_variantAfterBuiltinStrippingCount += builtinStrippedVariantInt;
                            shaderItem.editorLog_variantAfterSciptableStrippingCount += remainingVariantInt;
                            shaderItem.editorLog_variantCompiledCount += compiledVariantsInt;
                            shaderItem.editorLog_variantInCache += localCacheInt+remoteCacheInt;
                            shaderItem.editorLog_timeStripping += strippingTimeFloat;
                            shaderItem.editorLog_timeCompile += compileTimeFloat;

                            //Log shader program counts
                            /*
                                Serialized binary data for shader Hidden/VideoDecode in 0.00s
                                d3d11 (total internal programs: 30, unique: 19)
                                vulkan (total internal programs: 15, unique: 15)
                            */
                            currentLine = sr.ReadLine();
                            while(currentLine.Contains("Prepared data for serialisation in "))
                            {
                                currentLine = sr.ReadLine();
                            }
                            if (currentLine.Contains("Serialized binary data for shader "+shaderName))
                            {
                                currentLine = sr.ReadLine();
                                while(currentLine.Contains("total internal programs:"))
                                {
                                    string gfxAPI = Helper.ExtractString(currentLine,""," (total internal programs: ",false);
                                           gfxAPI = gfxAPI.Replace(" ","");
                                    string programCount_internal_string = Helper.ExtractString(currentLine,"total internal programs: ",", unique: ",false);
                                    string programCount_unique_string = Helper.ExtractString(currentLine,", unique: ",")",false);
                                    uint programCount_internal = uint.Parse(programCount_internal_string);
                                    uint programCount_unique = uint.Parse(programCount_unique_string); 

                                    ShaderProgram pgm = new ShaderProgram(gfxAPI, programCount_internal, programCount_unique);
                                    int matchedProgramId = shaderItem.FindMatchingProgramItem(pgm);
                                    if(matchedProgramId == -1)
                                    {
                                        shaderItem.programs.Add(pgm);
                                    }
                                    else
                                    {
                                        shaderItem.programs[matchedProgramId].count_internal += programCount_internal;
                                        shaderItem.programs[matchedProgramId].count_unique += programCount_unique;
                                    }
                                    currentLine = sr.ReadLine();
                                }
                            }

                            //Bug checking
                            sum_variantCountSkipped += skippedVariantsInt;
                            sum_variantCountinCache += localCacheInt+remoteCacheInt;
                            sum_variantCountCompiled += compiledVariantsInt;
                            sum_variantCountinBuild += remainingVariantInt;
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
            //Bug checking
            UInt64 variantCacheAndCompiledSum = sum_variantCountinCache + sum_variantCountCompiled + sum_variantCountSkipped;
            if( variantCacheAndCompiledSum != sum_variantCountinBuild)
            {
                Debug.LogError("ShaderVariantTool error #E03. "+
                "The sum of shader variants cached + compiled + skipped ("+variantCacheAndCompiledSum+") is not equal to the sum of remaning variants ("+
                sum_variantCountinBuild+") in EditorLog. Please contact @mingwai on slack. This could be related to exisiting known issue: Case 1389276");
            }
        }
    }
}