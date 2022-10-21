using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using UnityEditor.Rendering;
using System.Text;

namespace GfxQA.ReadCompiledShaderTool
{
    public class ReadCompiledShaderTool : EditorWindow
    {
        public string path = "";//"C:\\Users\\XYZ\\Documents\\Compiled-ABC.shader";
        public Shader shader;

        //Variant Data
        private List<VariantCompileStat> sList = new List<VariantCompileStat>();
        struct VariantCompileStat
        {
            public int subshaderID;
            public int passID;
            public string passName;
            public string variant;

            public int countMath;
            public int countTmpReg;
            public int countBranches;
            public int countTextures;
        }

        //For menu selection
        private int selectedSubShaderPass = 0;
        private List<SubShaderPass> pList = new List<SubShaderPass>();
        private List<string> pListMenu = new List<string>();
        struct SubShaderPass
        {
            public int subshaderID;
            public int passID;
            public string passName;
            public string summary;
        }

        [MenuItem("Window/ReadCompiledShaderTool")]
        public static void ShowWindow ()
        {
            var window = EditorWindow.GetWindow (typeof(ReadCompiledShaderTool));
            window.name = "ReadCompiledShaderTool";
            window.titleContent = new GUIContent("ReadCompiledShaderTool");
        }

        void Awake()
        {

        }

        void OnGUI () 
        {
            Color originalBackgroundColor = GUI.backgroundColor;

            //Title
            GUI.color = Color.yellow;
            GUILayout.Label (
                //"How to use: \n"+
                // "1. Select a shader, on Inspector, click on the little triangle next to Compile and Show Code \n"+
                // "2. Select only D3D on the list \n"+
                // "3. If you want all the variants (i.e. even unused ones), untick Skip unused shader_features \n"+
                // "4. Click Compile and Show Code. It will take awhile depends on how big your shader is \n"+
                // "5. After the compiled shader code is opened, save the file to somewhere, copy the path of the file \n"+
                // "    e.g. C:\\Users\\XYZ\\Documents\\Compiled-ABC.shader \n"+
                // "6. Paste the path into box below"
                "This only works for showing D3D compiler math numbers. \n"
            );
            //GUILayout.Space(10);
            GUI.color = Color.white;

            //path
            //path = GUILayout.TextField(path);

            //shader file
            shader = (Shader)EditorGUILayout.ObjectField(shader, typeof(Shader), true);
            if (shader != null && GUILayout.Button ("Compile shader",GUILayout.Width(200)))
            {
                path = "";
                CleanUpData();
                CompileShader();
            }

            //open compiled shader file
            if (path != "" )
            {
                GUI.color = Color.green;
                GUILayout.Label ("Compiled: "+path , EditorStyles.wordWrappedLabel);
                GUI.color = Color.white;
                if(GUILayout.Button ("Open Compiled shader File",GUILayout.Width(200)))
                {
                    Application.OpenURL (path);
                }
            }
            
            //read data
            // if (path != "" && GUILayout.Button ("Read data",GUILayout.Width(200)))
            // {
            //     ReadCompiledD3DShader(path);
            // }

            GUILayout.Space(20);

            //Subshader Pass selection
            if(pList.Count>0)
            {
                selectedSubShaderPass = EditorGUILayout.Popup("Select Subshader / Pass", selectedSubShaderPass, pListMenu.ToArray());
                GUILayout.Space(10);
                ShowData();
            }

            //End Window
            GUILayout.FlexibleSpace();
            EditorGUILayout.Separator();
        }

        private void CompileShader()
        {
            // Editor/Mono/ShaderUtil.bindings.cs
            // extern internal static void OpenCompiledShader(Shader shader, int mode, int externPlatformsMask, bool includeAllVariants, bool preprocessOnly, bool stripLineDirectives);
            // extern internal static void CompileShaderForTargetCompilerPlatform(Shader shader, ShaderCompilerPlatform platform);

            const bool INCLUDE_ALL_VARIANTS = false;
            System.Type t = typeof(ShaderUtil);
            MethodInfo dynMethod = t.GetMethod("OpenCompiledShader", BindingFlags.NonPublic | BindingFlags.Static);
            int defaultMask = (1 << System.Enum.GetNames(typeof(UnityEditor.Rendering.ShaderCompilerPlatform)).Length - 1);
            dynMethod.Invoke(null, new object[] { shader, 1, defaultMask, INCLUDE_ALL_VARIANTS, false, true});

            //This does not generate the compiled file
            // System.Type t = typeof(ShaderUtil);
            // MethodInfo dynMethod = t.GetMethod("CompileShaderForTargetCompilerPlatform", BindingFlags.NonPublic | BindingFlags.Static);
            // dynMethod.Invoke(null, new object[] { shader, ShaderCompilerPlatform.D3D});

            //Compiled shader file stored in project Temp folder, with name e.g. Compiled-Unlit-NewUnlitShader.shader
            path = Application.dataPath.Replace("Assets","Temp")+"/Compiled-"+shader.name.Replace("/","-")+".shader";
            Debug.Log("Compiled Shader : "+path);

            ReadCompiledD3DShader(path);
        }

        private void ShowData()
        {
            SubShaderPass selected = pList[selectedSubShaderPass];

            //Results Sum
            int r_countVariant_sum = 0;
            int r_countMath_sum = 0;
            int r_countTmpReg_sum = 0;
            int r_countBranches_sum = 0;
            int r_countTextures_sum = 0;

            //Results Avg
            int r_countMath_avg = 0;
            int r_countTmpReg_avg = 0;
            int r_countBranches_avg = 0;
            int r_countTextures_avg = 0;

            //Results Max
            int r_countMath_max = 0;
            int r_countTmpReg_max = 0;
            int r_countBranches_max = 0;
            int r_countTextures_max = 0;

            //The king sList IDs
            int countMath_king = 0;
            int countTmpReg_king = 0;
            int countBranches_king = 0;
            int countTextures_king = 0;

            for(int i=0; i<sList.Count; i++)
            {
                bool shouldLogResult = false;

                //Subshader
                if(selected.passID == -1 && sList[i].subshaderID == selected.subshaderID)
                {
                    shouldLogResult = true;
                }
                //Pass
                else if(sList[i].subshaderID == selected.subshaderID && sList[i].passID == selected.passID)
                {
                    shouldLogResult = true;
                }

                //Count into result
                if(shouldLogResult)
                {
                    //Sum
                    r_countVariant_sum ++;
                    r_countMath_sum += sList[i].countMath;
                    r_countTmpReg_sum += sList[i].countTmpReg;
                    r_countBranches_sum += sList[i].countBranches;
                    r_countTextures_sum += sList[i].countTextures;

                    //King
                    if( sList[i].countMath > sList[countMath_king].countMath ) countMath_king = i;
                    if( sList[i].countTmpReg > sList[countTmpReg_king].countTmpReg ) countTmpReg_king = i;
                    if( sList[i].countBranches > sList[countBranches_king].countBranches ) countBranches_king = i;
                    if( sList[i].countTextures > sList[countTextures_king].countTextures ) countTextures_king = i;
                }
            }

            if(r_countVariant_sum > 0)
            {
                //Avg
                r_countMath_avg = r_countMath_sum / r_countVariant_sum;
                r_countTmpReg_avg = r_countTmpReg_sum / r_countVariant_sum;
                r_countBranches_avg = r_countBranches_sum / r_countVariant_sum;
                r_countTextures_avg = r_countTextures_sum / r_countVariant_sum;

                //Max
                r_countMath_max = sList[countMath_king].countMath;
                r_countTmpReg_max = sList[countTmpReg_king].countTmpReg;
                r_countBranches_max = sList[countBranches_king].countBranches;
                r_countTextures_max = sList[countTextures_king].countTextures;
            }

            //Width for the columns & style
            float currentSize = this.position.width;
            float[] columnWidth = new float[]
            {
                0.5f,
                0.3f,
                0.3f,
                0.3f,
                2f
            };
            float widthForEach = currentSize / columnWidth.Length;
            GUILayoutOption[] columnLayoutOption = new GUILayoutOption[columnWidth.Length];
            for(int i=0; i<columnLayoutOption.Length; i++)
            {
                columnLayoutOption[i] = GUILayout.Width(Mathf.RoundToInt(widthForEach * columnWidth[i]));
            }
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold;
            GUIStyle kingButtonStyle = new GUIStyle(GUI.skin.label);

            //Output Result

            if(selected.passID != -1)
            {
                GUI.color = Color.cyan;
                GUILayout.Label("Summary: ");
                GUI.color = Color.white;
                string summary = pList[selectedSubShaderPass-1].summary; //Compensate offset
                if(summary == "") summary = "N/A";
                GUILayout.Label(summary); 
            }

            GUI.color = Color.cyan;
            GUILayout.Label("Details (all shader stages): ");
            GUI.color = Color.white;
            GUILayout.Label("Variant count: "+r_countVariant_sum);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            int layoutID = 0;
            EditorGUILayout.LabelField("Data",headerStyle,columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField("Math",columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField("Temp Registers",columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField("Branches",columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField("Textures",columnLayoutOption[layoutID]);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            layoutID++;
            EditorGUILayout.LabelField("Sum",headerStyle,columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countMath_sum.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countTmpReg_sum.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countBranches_sum.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countTextures_sum.ToString(),columnLayoutOption[layoutID]);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            layoutID++;
            EditorGUILayout.LabelField("Avg",headerStyle,columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countMath_avg.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countTmpReg_avg.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countBranches_avg.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countTextures_avg.ToString(),columnLayoutOption[layoutID]);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            layoutID++;
            EditorGUILayout.LabelField("Max",headerStyle,columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countMath_max.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countTmpReg_max.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countBranches_max.ToString(),columnLayoutOption[layoutID]);
            EditorGUILayout.LabelField(r_countTextures_max.ToString(),columnLayoutOption[layoutID]);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            layoutID++;
            EditorGUILayout.LabelField("Max numbers is from this variant (Click to copy)",headerStyle,columnLayoutOption[layoutID]);
            
            string kingText = sList[countMath_king].variant;
            if(GUILayout.Button(kingText,kingButtonStyle,columnLayoutOption[layoutID]))
            {
                GUIUtility.systemCopyBuffer = kingText;
            }

            kingText = sList[countTmpReg_king].variant;
            if(GUILayout.Button(kingText,kingButtonStyle,columnLayoutOption[layoutID]))
            {
                GUIUtility.systemCopyBuffer = kingText;
            }

            kingText = sList[countBranches_king].variant;
            if(GUILayout.Button(kingText,kingButtonStyle,columnLayoutOption[layoutID]))
            {
                GUIUtility.systemCopyBuffer = kingText;
            }

            kingText = sList[countTextures_king].variant;
            if(GUILayout.Button(kingText,kingButtonStyle,columnLayoutOption[layoutID]))
            {
                GUIUtility.systemCopyBuffer = kingText;
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
    
        }
        
        private void CleanUpData()
        {
            sList.Clear();
            pList.Clear();
            pListMenu.Clear();
        }

        private void ReadCompiledD3DShader(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //For iteration of file texts
            bool variantstart = false;
            string keywordsText = "";

            //Data for packing into VariantCompileStat
            int data_subshaderID = -1;
            int data_passID = -1;
            string data_passName = "";
            string data_variant = "";
            int data_countMath = 0;
            int data_countTmpReg = 0;
            int data_countBranches = 0;
            int data_countTextures = 0;

            //CleanUp
            CleanUpData();

            using (StreamReader sr = new StreamReader(fs))
            {
                string currentLine = "";
                while(currentLine != null) //while(!sr.EndOfStream) does not work
                {
                    currentLine = sr.ReadLine();
                    if(currentLine != null)
                    {
                        //SUBSHADER
                        if(currentLine.Contains("SubShader { "))
                        {
                            data_subshaderID ++;
                            data_passID = -1;

                            //Pack into struct
                            SubShaderPass newData = new SubShaderPass();
                            newData.subshaderID = data_subshaderID;
                            newData.passID = -1;
                            newData.passName = "";
                            pList.Add(newData);
                        }

                        //PASS
                        else if(currentLine.Contains("  Name "+'"'))
                        {
                            data_passID ++;
                            data_passName = currentLine.Replace("  Name ", "");

                            //Pack into struct
                            SubShaderPass newData = new SubShaderPass();
                            newData.subshaderID = data_subshaderID;
                            newData.passID = data_passID;
                            newData.passName = data_passName;
                            pList.Add(newData);
                        }

                        //VARIANT START
                        else if(currentLine.Contains("Keywords: "))
                        {
                            keywordsText = currentLine.Replace("Keywords: ","");
                            variantstart = true;
                        }

                        //STATS for VARIANT
                        else if(currentLine.Contains("// Stats: "))
                        {
                            //VARIANT
                            if(variantstart)
                            {
                                data_variant = keywordsText;
                                keywordsText = "";
                                variantstart = false;
                            }

                            /*
                            // Stats: 79 math, 7 temp registers, 33 branches
                            // Stats: 0 math, 1 textures
                            */

                            //DATA
                            string data = currentLine;
                            data = data.Replace("// Stats: ","");

                            //math count
                            string temp = ExtractString(data, ""," math",true);
                            data_countMath = int.Parse(temp);
                            data = data.Replace(data_countMath+" math, ","");

                            //textures count
                            if(data.Contains("textures"))
                            {
                                temp = ExtractString(data, ""," textures",true);
                                data_countTextures = int.Parse(temp);
                                data = data.Replace(data_countTextures+" textures, ","");
                            }

                            //register count
                            if(data.Contains("temp registers"))
                            {
                                temp = ExtractString(data, ""," temp registers",true);
                                data_countTmpReg = int.Parse(temp);
                                data = data.Replace(data_countTmpReg+" temp registers, ","");
                            }

                            //branch count
                            if(data.Contains("branches"))
                            {
                                temp = ExtractString(data, ""," branches",true);
                                data_countBranches = int.Parse(temp);
                            }                       

                            //Pack into struct
                            VariantCompileStat newData = new VariantCompileStat();
                            newData.subshaderID = data_subshaderID;
                            newData.passID = data_passID;
                            newData.passName = data_passName;
                            newData.variant = data_variant;
                            newData.countMath = data_countMath;
                            newData.countTmpReg = data_countTmpReg;
                            newData.countBranches = data_countBranches;
                            newData.countTextures = data_countTextures;
                            sList.Add(newData);
                        }

                        //STATS for PASS (SUMMARY OF PASS)
                        else if(currentLine.Contains("// Stats for "))
                        {
                            // Stats for Vertex shader:
                            //        d3d11: 9 math
                            // Stats for Fragment shader:
                            //        d3d11: 0 math, 1 texture

                            string summary = "";
                            summary += currentLine + "\n";
                            currentLine = sr.ReadLine();
                            summary += currentLine + "\n";
                            
                            //Summary comes before the pass, i.e. Subshader > summary > Pass, so this ID is offset by 1.
                            //Offset compensates in ShowData()
                            int pListID = pList.Count-1;
                            var temp = pList[pListID];
                            temp.summary += summary;
                            pList[pListID] = temp;
                        }
                    }
                }
            }

            //Add options to Subshader/Pass menu list
            foreach(SubShaderPass pData in pList)
            {
                if(pData.passID == -1)
                {
                    //Subshader
                    pListMenu.Add("Subshader: "+pData.subshaderID);
                }
                else
                {
                    //Pass
                    pListMenu.Add("Subshader: "+pData.subshaderID+" > Pass: "+pData.passName);
                }
            }
        }

        //==========HELPER===========
        private static string ExtractString(string line, string from, string to, bool takeLastIndexOfTo = true)
        {
            int pFrom = 0;
            if(from != "")
            {
                int index = line.IndexOf(from);
                if(index >= 0) pFrom = index + from.Length;
            }
            
            int pTo = line.Length;
            if(to != "")
            {
                int index = line.LastIndexOf(to);
                if(!takeLastIndexOfTo)
                {
                    index = line.IndexOf(to);
                }

                if(index >= 0) pTo = index;
            }

            return line.Substring(pFrom, pTo - pFrom);
        }
    }
}



