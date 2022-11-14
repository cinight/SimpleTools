using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.IMGUI.Controls;

namespace GfxQA.ShaderVariantTool
{
    public class ShaderVariantTool : EditorWindow
    {
        private Vector2 scrollPosition;
        public static string savedFile = "";

        //For Build summary style
        private CultureInfo culture = new CultureInfo("is-IS");
        private GUIStyle variantDisplayStyle = null;
        private string format = "{0, -50}{1, 30}";
        private Font monospacedFont;
        private List<string[]> buildRows;

        //For Shader summary table
        private bool shader_guiEnabled = true;
        private MultiColumnHeader shader_columnHeader;
        private MultiColumnHeaderState.Column[] shader_columns;
        private List<string[]> shaderRows;
        private List<bool> shaderRows_Expanded;
        private int visibleShaderColumns = 6; //lineData.Length //don't want to show all the columns
        private int defaultSortColumn = 4; //Sorting default is no. variant after stripping (i.e. variant in build)

        //For Variant keyword table
        private MultiColumnHeader variant_columnHeader;
        private MultiColumnHeaderState.Column[] variant_columns;
        private List<string[]> variantRows;
        private int visibleVariantColumns = 0;
        private int defaultVariantSortColumn = 1; //Sorting default is no. variant after stripping (i.e. variant in build)
        private float widthPadding = 10;

        //Table styles
        private Color columnColor1 = new Color(0.3f,0.3f,0.3f,1);
        private Color columnColor2 = new Color(0.28f,0.28f,0.28f,1);
        private Color columnColorbg = new Color(0.7f,0.7f,0.7f,1);
        private Color columnColorShader = new Color(0,0.2f,0.2f);
        private Color columnColorShaderHover = new Color(0,0.3f,0.3f);
        private GUIStyle background = null;
        private GUIStyle foldoutTitleStyle = null;

#region CSV file reading functions

        private List<string> CSVFileNames;
        private int selectedCSVFile = 0;
        private string prevSelectedCSVFile = "";

        private void GetCSVFileNames()
        {
            if(CSVFileNames == null)
            {
                CSVFileNames = new List<string>();
            }
            else
            {
                CSVFileNames.Clear();
            }

            var info = new DirectoryInfo(Helper.GetCSVFolderPath());
            var fileInfo = info.GetFiles();
            foreach(FileInfo file in fileInfo)
            {
                if(file.Name.Contains("ShaderVariants_") && file.FullName.Contains(".csv"))
                {
                    CSVFileNames.Add(file.FullName);
                }
            }

            //sort file names, top is newest
            CSVFileNames = CSVFileNames.OrderByDescending(o=>o).ToList();
        }

        private string[] ReadCSVFile()
        {
            string fileData  = System.IO.File.ReadAllText(CSVFileNames[selectedCSVFile]);
            string[] lines = fileData.Split("\n"[0]);
            return lines;
        }

        private string[] GetLineCells(string[] lines, int lineID)
        {
            return lines[lineID].Trim().Split(","[0]);
        }

        private string NumberSeperator(string input)
        {
            int outNum = 0;
            bool success = int.TryParse(input, out outNum);
            if(success)
            {
                return outNum.ToString("N0");
            }
            else
            {
                return input;
            }
        }

        void UpdateCSVDataForGUI()
        {
            //Read the CSV file lines
            string[] lines = ReadCSVFile();
            if(lines.Length <= 0)
            {
                //TODO some error messages when there is no line to read
            }
            else
            {
                int lineID = 0;
                string[] lineData = GetLineCells(lines,lineID);

                //Load build summary data
                if(buildRows == null) buildRows = new List<string[]>();
                buildRows.Clear();
                while(lineData[0]!="")
                {
                    buildRows.Add(lineData);

                    //Next line
                    lineID++;
                    lineData = GetLineCells(lines,lineID);
                }

                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Shader number list header
                shader_columns = new MultiColumnHeaderState.Column[visibleShaderColumns];
                for(int i=0; i<shader_columns.Length; i++)
                {
                    MultiColumnHeaderState.Column col = new MultiColumnHeaderState.Column();
                    col.headerContent = new GUIContent(lineData[i]);
                    col.minWidth = 100;
                    col.autoResize = true;
                    col.headerTextAlignment = TextAlignment.Left;
                    col.sortingArrowAlignment = TextAlignment.Right;
                    shader_columns[i] = col;
                }
                MultiColumnHeader.DefaultStyles.columnHeader.fontStyle = FontStyle.Bold;
                shader_columnHeader = new MultiColumnHeader(new MultiColumnHeaderState(shader_columns));
                shader_columnHeader.canSort = true;
                shader_columnHeader.ResizeToFit();
                if(shader_columnHeader.sortedColumnIndex < 0) shader_columnHeader.sortedColumnIndex = defaultSortColumn;
                
                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Collect shader summary rows
                if(shaderRows == null) shaderRows = new List<string[]>();
                if(shaderRows_Expanded == null) shaderRows_Expanded = new List<bool>();
                shaderRows.Clear();
                shaderRows_Expanded.Clear();
                while(lineData[0]!="")
                {
                    shaderRows.Add(lineData);
                    shaderRows_Expanded.Add(false);

                    //Next line
                    lineID++;
                    lineData = GetLineCells(lines,lineID);
                }

                //Sort shader rows
                OnShaderSortingChanged(null);

                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Variant list header
                visibleVariantColumns = lineData.Length -1;//skip the shader name column
                if(variant_columns == null || variant_columnHeader == null)
                {
                    variant_columns = new MultiColumnHeaderState.Column[visibleVariantColumns];
                    for(int i=0; i<variant_columns.Length; i++)
                    {
                        MultiColumnHeaderState.Column col = new MultiColumnHeaderState.Column();
                        col.headerContent = new GUIContent(lineData[i+1]);
                        col.minWidth = 20;
                        col.autoResize = true;
                        col.headerTextAlignment = TextAlignment.Left;
                        col.sortingArrowAlignment = TextAlignment.Right;
                        variant_columns[i] = col;
                    }

                    MultiColumnHeader.DefaultStyles.columnHeader.fontStyle = FontStyle.Bold;
                    variant_columnHeader = new MultiColumnHeader(new MultiColumnHeaderState(variant_columns));
                    variant_columnHeader.canSort = false; //no sorting
                    variant_columnHeader.ResizeToFit();
                }

                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Collect variant rows
                if(variantRows == null) variantRows = new List<string[]>();
                variantRows.Clear();
                while(lineData[0]!="")
                {
                    variantRows.Add(lineData);

                    //Next line
                    lineID++;
                    lineData = GetLineCells(lines,lineID);
                }

                //Sort variant list by compile count
                variantRows = variantRows.OrderByDescending(o=>int.Parse(o[defaultVariantSortColumn])).ToList();

                Debug.Log("ShaderVariantTool - successfully loaded "+CSVFileNames[selectedCSVFile]);
            }
        }

        private void OnShaderSortingChanged(MultiColumnHeader header)
        {
            if(shader_columnHeader.sortedColumnIndex > 0)
            {
                //sort numbers
                if(shader_columnHeader.IsSortedAscending(shader_columnHeader.sortedColumnIndex))
                {
                    shaderRows = shaderRows.OrderBy(o=>float.Parse(o[shader_columnHeader.sortedColumnIndex])).ToList();
                }
                else
                {
                    shaderRows = shaderRows.OrderByDescending(o=>float.Parse(o[shader_columnHeader.sortedColumnIndex])).ToList();
                }
            }
            else
            {
                //sort string Alphabetical Order
                if(shader_columnHeader.IsSortedAscending(shader_columnHeader.sortedColumnIndex))
                {
                    shaderRows = shaderRows.OrderBy(o=>o[shader_columnHeader.sortedColumnIndex]).ToList();
                }
                else
                {
                    shaderRows = shaderRows.OrderByDescending(o=>o[shader_columnHeader.sortedColumnIndex]).ToList();
                }
            }
        }

#endregion

        [MenuItem("Window/ShaderVariantTool")]
        public static void ShowWindow ()
        {
            var window = EditorWindow.GetWindow (typeof(ShaderVariantTool));
            window.name = "ShaderVariantTool";
            window.titleContent = new GUIContent("ShaderVariantTool");
        }

        void OnEnable()
        {
            //InitGUIStyles(); //Can't call it here..
        }

        private void InitGUIStyles()
        {
            //Build summary styles setup
            if (variantDisplayStyle == null)
            {
                if(monospacedFont==null) monospacedFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
                variantDisplayStyle = new GUIStyle(GUI.skin.label);//EditorStyles.label);
                variantDisplayStyle.font = monospacedFont;
                variantDisplayStyle.wordWrap = true;
                variantDisplayStyle.hover.textColor = Color.cyan;
            }

            //shader & variant table row background
            if(background == null)
            {
                background = new GUIStyle 
                { 
                    normal = 
                    { 
                        background = Texture2D.whiteTexture,
                        textColor = Color.white
                    } 
                };
            }

            //shader list foldout
            if(foldoutTitleStyle == null)
            {
                foldoutTitleStyle = new GUIStyle("foldout");
                foldoutTitleStyle.fontStyle = FontStyle.Bold;
            }
        }

        private void CopyValueMenu(Rect valueRect, string textToCopy)
        {
            GUIContent s_CopyPropertyText = EditorGUIUtility.TrTextContent("Copy");
            var e = Event.current;

            // Copy function
            if (valueRect.Contains(e.mousePosition))
            {
                e.Use();

                GenericMenu menu = new GenericMenu();
                menu.AddItem(s_CopyPropertyText, false, delegate 
                {
                    EditorGUIUtility.systemCopyBuffer = textToCopy;
                });
                menu.ShowAsContext();
            }
        }

        void OnGUI () 
        {
            InitGUIStyles();

            Color originalBackgroundColor = GUI.backgroundColor;

#region CSV file selection GUI

            //This should be always on
            ShaderVariantTool_BuildPreprocess.deletePlayerCacheBeforeBuild = true;

            //Scroll Start
            scrollPosition = GUILayout.BeginScrollView(scrollPosition,GUILayout.Width(0),GUILayout.Height(0));
            
            //Select CSV file to read from
            if (GUILayout.Button ("Update CSV File List",GUILayout.Width(200)) || CSVFileNames == null)
            {
                GetCSVFileNames();
            }

            if(CSVFileNames.Count == 0 )
            {
                //Hint for user
                GUI.color = Color.cyan;
                GUILayout.Label ("Build the player and see the variants list here.", EditorStyles.wordWrappedLabel);
                GUI.color = Color.white;

                GUILayout.EndScrollView();
                return;
            }

            // string CSVFileCompare = CSVFileNames[selectedCSVFile].Replace(@"\", @"/");
            // if(savedFile != "" && CSVFileCompare != savedFile)
            // {
            //     GetCSVFileNames();
            //     prevSelectedCSVFile = -1;
            //     Debug.Log(CSVFileCompare + " ---- " + savedFile);
            // }

            //Show dropdown list
            selectedCSVFile = EditorGUILayout.Popup("Select CSV File", selectedCSVFile, CSVFileNames.ToArray());
            if(prevSelectedCSVFile != CSVFileNames[selectedCSVFile])
            {
                UpdateCSVDataForGUI();
                prevSelectedCSVFile = CSVFileNames[selectedCSVFile];
            }

            //Show folder button
            if (GUILayout.Button ("Show in explorer",GUILayout.Width(200)))
            {
                string path = CSVFileNames[selectedCSVFile];
                EditorUtility.RevealInFinder(path.Replace(@"/", @"\"));
            }
            GUILayout.Space(10);

#endregion
#region Build summary GUI

            GUI.skin.settings.doubleClickSelectsWord = true;

            //To prevent error after script compiles when tool is opened
            if(buildRows == null)
            {
                UpdateCSVDataForGUI();
                prevSelectedCSVFile = CSVFileNames[selectedCSVFile];
            }

            for(int i=0; i<buildRows.Count; i++)
            {
                //Add some spaces
                    if( buildRows[i][0] =="Shader Count" || buildRows[i][0] =="ComputeShader Count" )
                {
                    GUILayout.Space(10);
                }

                //Fill the table
                if( buildRows[i][0] !="Build Path" )//Do not show build path
                {
                    string content = String.Format( format, buildRows[i][0], NumberSeperator(buildRows[i][1]) );
                    GUILayout.Label ( content , variantDisplayStyle );
                    //Make text copyable
                    if (Event.current.type == EventType.ContextClick) CopyValueMenu(GUILayoutUtility.GetLastRect(), content);
                }
            }

#endregion
#region Shader & variant GUI

            //Show foldout title
            GUILayout.Space(15);
            GUI.color = Color.cyan;
            shader_guiEnabled = EditorGUILayout.Foldout( shader_guiEnabled, "Shader summary numbers", foldoutTitleStyle );
            GUI.color = Color.white;

            //Show shader table
            Rect shaderRect = GUILayoutUtility.GetLastRect();
            shaderRect.position += new Vector2(0,30);
            shaderRect.width = position.width;
            if(shader_guiEnabled)
            {
                //Shader header
                shader_columnHeader.OnGUI(shaderRect, 1); //TODO - check if we need xScroll
                
                //Apply sorting
                shader_columnHeader.sortingChanged += OnShaderSortingChanged;

                //Shader rows
                int uiRowCount = 0;
                Rect contentRect = new Rect();
                for(int i=0; i<shaderRows.Count; i++)
                {
                    string[] rowlineData = shaderRows[i];
                    if(!shaderRows_Expanded[i])
                    {
                        //For each column
                        for(int j=0; j<visibleShaderColumns; j++)
                        {
                            contentRect = shader_columnHeader.GetColumnRect(j);
                            contentRect.position += new Vector2(0,shaderRect.position.y+contentRect.height*(uiRowCount+1));

                            //mouse hovercheck for row colors
                            Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                            Vector2 contentScreenPos = GUIUtility.GUIToScreenPoint(contentRect.position);
                            if(
                                mousePos.x >= position.x && mousePos.x <= position.xMax &&
                                mousePos.y >= contentScreenPos.y && mousePos.y <= contentScreenPos.y+contentRect.height
                            )
                            {
                                GUI.backgroundColor = columnColorShaderHover;
                            }
                            else
                            {
                                GUI.backgroundColor = uiRowCount%2 == 0 ? columnColor1 :columnColor2;
                            }

                            //Show the shader summary number
                            string content = rowlineData[j];
                            shaderRows_Expanded[i] = GUI.Toggle(contentRect,shaderRows_Expanded[i],content,background);
                        }
                    }
                    else
                    {
                        //Show the shader name
                        GUI.backgroundColor = columnColorShader;
                        string shaderName = rowlineData[0];
                        contentRect = shader_columnHeader.GetColumnRect(0);
                        contentRect.width = position.width;
                        contentRect.position += new Vector2(0,shaderRect.position.y+contentRect.height*(uiRowCount+1));
                        shaderRows_Expanded[i] = GUI.Toggle(contentRect,shaderRows_Expanded[i],shaderName,background);
                        GUI.backgroundColor = originalBackgroundColor;
                        uiRowCount++;

                        //Show the shader summary numbers
                        GUI.backgroundColor = columnColorbg;
                        for(int j=1;j<visibleShaderColumns;j++)
                        {
                            contentRect.position += new Vector2(0,contentRect.height);
                            String content = String.Format( format, shader_columnHeader.GetColumn(j).headerContent.text, NumberSeperator(rowlineData[j]) );
                            GUI.Label ( contentRect, content, variantDisplayStyle );
                            //Make text copyable
                            if (Event.current.type == EventType.ContextClick) CopyValueMenu(contentRect, content);
                            uiRowCount++;
                        }

                        //Show variant header
                        Rect variantRect = shader_columnHeader.GetColumnRect(0);
                        variantRect.position += new Vector2(widthPadding,shaderRect.position.y+contentRect.height*(uiRowCount+2));
                        variantRect.width = position.width - widthPadding*2;
                        variant_columnHeader.OnGUI(variantRect, 1); //TODO xScroll
                        uiRowCount++;
                        uiRowCount++;
                        
                        //Show variant rows
                        for(int k=0; k<variantRows.Count; k++)
                        {
                            if(variantRows[k][0] == shaderName)
                            {
                                //row colors
                                GUI.backgroundColor = uiRowCount%2 == 0 ? columnColor1 :columnColor2;

                                for(int j=0;j<visibleVariantColumns;j++)
                                {
                                    contentRect = variant_columnHeader.GetColumnRect(j);
                                    contentRect.position += new Vector2(widthPadding,shaderRect.position.y+contentRect.height*(uiRowCount+1));
                                    string content = variantRows[k][j+1];
                                    GUI.Label(contentRect, content, background);
                                    //Make text copyable
                                    if (Event.current.type == EventType.ContextClick) CopyValueMenu(contentRect, content);
                                }
                                uiRowCount++;
                            }
                        }
                        GUI.backgroundColor = columnColorbg;
                    }

                    uiRowCount++;
                }
                GUI.backgroundColor = originalBackgroundColor;
                GUILayout.Space(EditorGUIUtility.singleLineHeight * (uiRowCount) + shaderRect.height);
            }
#endregion
            //Scroll End
            GUILayout.Space(50);
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
            EditorGUILayout.Separator();
            Repaint(); //repaint everyframe so that mouse hover effect won't have delay
        }
    }
    //===================================================================================================
    public static class SVL
    {
        //build process indicators
        public static string buildProcessIDTitleStart = "ShaderVariantTool_Start:";
        public static string buildProcessIDTitleEnd = "ShaderVariantTool_End:";
        public static string buildProcessID = ""; //For recognising position in EditorLog
        public static bool buildProcessStarted = false;

        //the big total
        public static double buildTime = 0;
        public static string buildTimeString = "";
        public static uint compiledTotalCount = 0; //The number of data that the tool processed
        public static uint variantTotalCount = 0; //shader variant count + compute variant count

        //shader variant
        public static uint variantOriginalCount = 0;
        public static uint variantAfterPrefilteringCount = 0;
        public static uint variantAfterBuiltinStrippingCount = 0;
        public static uint variantCompiledCount = 0;
        public static uint variantInCache = 0;
        public static uint normalShaderCount = 0;
        public static uint variantFromShader = 0;
        public static uint shaderDynamicVariant = 0; //This will always hit shadercache == will not compile

        //compute shader variant
        public static uint variantFromCompute = 0;
        public static uint computeShaderCount = 0;
        public static uint computeDynamicVariant = 0; //This will always hit shadercache == will not compile

        //invalid or disabled keywords for final error logging
        public static string invalidKey = "";
        public static string disabledKey = "";
        
        //data
        public static List<CompiledShaderVariant> variantlist = new List<CompiledShaderVariant>();
        public static List<CompiledShader> shaderlist = new List<CompiledShader>();
        public static List<string[]> rowData = new List<string[]>();
        public static string[] columns = new string[] 
        {
            "Shader",
            "Compiled Count",
            "Keyword Name",
            "PassName",
            "ShaderType",
            "PassType",
            "KernelName",
            "GfxTier",
            "Build Target",
            "Compiler Platform",
            "Platform Keywords",
            "Keyword Type",
            //"Require",
            //"Keyword Index",
            //"Keyword Valid",
            //"Keyword Enabled",
            
        };

        public static void ResetBuildList()
        {
            if(!buildProcessStarted)
            {
                shaderlist.Clear();
                variantlist.Clear();
                buildTime = 0;
                buildTimeString = "";
                compiledTotalCount = 0;
                variantTotalCount = 0;
                variantOriginalCount = 0;
                variantAfterPrefilteringCount = 0;
                variantAfterBuiltinStrippingCount = 0;
                variantCompiledCount = 0;
                variantInCache = 0;
                variantFromCompute = 0;
                variantFromShader = 0;
                computeShaderCount = 0;
                normalShaderCount = 0;
                shaderDynamicVariant = 0;
                computeDynamicVariant = 0;

                //For reading EditorLog, we can extract the contents
                buildProcessID = System.DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, buildProcessIDTitleStart+buildProcessID);

                buildProcessStarted = true;
            }
        }

        public static void Sorting()
        {
            //sort the list according to shader name
            variantlist = variantlist.OrderBy(o=>o.shaderName).ThenBy(o=>o.shaderType).ThenBy(o=>o.shaderKeywordName).ToList();

            //Unique item and duplicate counts
            Dictionary<CompiledShaderVariant, int> uniqueSet = new Dictionary<CompiledShaderVariant, int>();

            //count duplicates
            for(int i=0; i<variantlist.Count; i++)
            {
                //is a duplicate
                if( uniqueSet.ContainsKey(variantlist[i]) )
                {
                    //add to duplicate count
                    uniqueSet[variantlist[i]]++;
                }
                //new unique item
                else
                {
                    //add to unique list
                    uniqueSet.Add(variantlist[i],1);
                }
            }

            //remove duplicates
            variantlist = variantlist.Distinct().ToList();

            //make string lists
            rowData.Clear();
            rowData.Add(columns);
            for(int k=0; k < variantlist.Count; k++)
            {
                rowData.Add(new string[] {
                    variantlist[k].shaderName,
                    uniqueSet[variantlist[k]].ToString(),
                    variantlist[k].shaderKeywordName,
                    variantlist[k].passName,
                    variantlist[k].shaderType,
                    variantlist[k].passType,
                    variantlist[k].kernelName,
                    variantlist[k].graphicsTier,
                    variantlist[k].buildTarget,
                    variantlist[k].shaderCompilerPlatform,
                    //variantlist[k].shaderRequirements,
                    variantlist[k].platformKeywords,
                    variantlist[k].shaderKeywordType,
                    //variantlist[k].shaderKeywordIndex,
                    //variantlist[k].isShaderKeywordValid,
                    //variantlist[k].isShaderKeywordEnabled,
                });
            }

            //clean up
            variantlist.Clear();
        }
    }
    //===================================================================================================
    public struct CompiledShader
    {
        public bool isComputeShader;
        public string name;
        public bool guiEnabled;
        public uint noOfVariantsForThisShader;
        public uint dynamicVariantForThisShader;
        public uint editorLog_originalVariantCount;
        public uint editorLog_prefilteredVariantCount;
        public uint editorLog_builtinStrippedVariantCount;
        public uint editorLog_remainingVariantCount;
        public uint editorLog_compiledVariantCount;
        public uint editorLog_variantInCacheCount;
        public float editorLog_totalProcessTime;
    };
    public struct CompiledShaderVariant
    {
        //shader
        public string shaderName;

        //snippet
        public string passType;
        public string passName;
        public string shaderType;

        //compute kernel
        public string kernelName;

        //data
        public string graphicsTier;
        public string buildTarget; 
        public string shaderCompilerPlatform;
        //public string shaderRequirements;

        //data - PlatformKeywordSet
        public string platformKeywords;
        //public string isplatformKeywordEnabled; //from PlatformKeywordSet

        //data - ShaderKeywordSet
        public string shaderKeywordName; //ShaderKeyword.GetKeywordName
        public string shaderKeywordType; //ShaderKeyword.GetKeywordType
        //public string shaderKeywordIndex; //ShaderKeyword.index
        //public string isShaderKeywordValid; //from ShaderKeyword.IsValid()
        //public string isShaderKeywordEnabled; //from ShaderKeywordSet
    };

}
