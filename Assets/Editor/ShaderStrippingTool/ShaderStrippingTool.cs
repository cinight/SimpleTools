using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class ShaderStrippingTool : EditorWindow
{
	Vector2 scrollPosition;

    public static string folderPath = "";
    public static string savedFile = "";
    
    //ColumnSetup
    Color columnColor1 = new Color(0.3f,0.3f,0.3f,1);
    Color columnColor2 = new Color(0.28f,0.28f,0.28f,1);

    float[] widthScale = new float[]
    {
        0, //we don't show shader name

        0.9f,
        1.8f,
        1.2f,

        0.6f,
        0.8f,
   
        2.3f,
        1f,
        0.6f,
        0.6f,
        0.6f,

        0.6f,
    };

    [MenuItem("Window/ShaderStrippingTool")]
	public static void ShowWindow ()
	{
		var window = EditorWindow.GetWindow (typeof(ShaderStrippingTool));
	}

    // public void Awake()
    // {
    // }

    // public void OnDestroy()
    // {
    // }

    void OnGUI () 
	{
        Color originalBackgroundColor = GUI.backgroundColor;

        //Title
        GUI.color = Color.cyan;
        GUILayout.Label ("Build the player and see the variants list here.", EditorStyles.wordWrappedLabel);
        GUI.color = Color.white;

        if(savedFile != "")
        {
            GUI.color = Color.green;

            //Result
            GUILayout.Label ( "Build Time : " + SVL.buildTime.ToString("0.000") + " seconds", EditorStyles.wordWrappedLabel );
            GUILayout.Label ( "Total Variant Count : " + SVL.variantCount, EditorStyles.wordWrappedLabel );

            //Saved file path
            GUILayout.Label ( "Saved: "+savedFile, EditorStyles.wordWrappedLabel);

            //Show folder button
            GUI.color = Color.white;
            if (GUILayout.Button ("Show in explorer",GUILayout.Width(200)))
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,"+folderPath.Replace(@"/", @"\")); // explorer doesn't like front slashes
            }
        }
        GUI.color = Color.white;
        GUILayout.Space(15);

        //Width for the columns & style
        float currentSize = this.position.width;
        float widthForEach = currentSize / (SVL.columns.Length-1+currentSize*0.0002f);
        GUIStyle background = new GUIStyle 
        { 
            normal = 
            { 
                background = Texture2D.whiteTexture,
                textColor = Color.white
            } 
        };

        //Column Titles
        EditorGUILayout.BeginHorizontal();
        for(int i=1;i<SVL.columns.Length;i++)
        {
            int al = i%2;
            GUI.backgroundColor = al ==0 ? columnColor1 :columnColor2;
            GUILayoutOption[] columnLayoutOption = new GUILayoutOption[]
            {
                GUILayout.Width(Mathf.RoundToInt(widthForEach*widthScale[i])),
                GUILayout.Height(55)
            };
            EditorGUILayout.LabelField (SVL.columns[i].Replace(" ","\n"),background,columnLayoutOption);
        }
        EditorGUILayout.EndHorizontal();

        //Reset color
        GUI.backgroundColor = originalBackgroundColor;
        GUI.color = Color.white;

        //Scroll Start
        scrollPosition = GUILayout.BeginScrollView(scrollPosition,GUILayout.Width(0),GUILayout.Height(0));      

        //Display result
        if(SVL.list != null)
        {
            //SORTING

            string currentShader = "";
            for(int k=0; k < SVL.list.Count; k++)
            {
                if(SVL.list[k].shaderName != currentShader)
                {
                    GUI.backgroundColor = originalBackgroundColor;
                    SVL.list[k].enabled = EditorGUILayout.Foldout( SVL.list[k].enabled, SVL.list[k].shaderName + " (" + SVL.list[k].noOfVariantsForThisShader + ")" );
                    currentShader = SVL.list[k].shaderName;
                }
                else
                {
                    SVL.list[k].enabled = SVL.list[k-1].enabled;
                }

                //Show the shader variants
                if( SVL.list[k].enabled )
                {
                    EditorGUILayout.BeginHorizontal();
                    PropertyInfo[] props = typeof(ShaderCompiledVariant).GetProperties();
                    for(int i=0;i<SVL.columns.Length;i++)
                    {
                        object value = props[i].GetValue(SVL.list[k]);
                        string t = value!=null ? value.ToString() : "-";

                        int al = i%2;
                        GUI.backgroundColor = al ==0 ? columnColor1 :columnColor2;
                        if(t == "True") background.normal.textColor = Color.green;
                        else if(t == "False") background.normal.textColor = Color.red;
                        else if(t.Contains("[Global]")) background.normal.textColor = Color.cyan;
                        else if(t.Contains("[Local]")) background.normal.textColor = Color.yellow;
                        else background.normal.textColor = Color.white;

                        EditorGUILayout.LabelField (t,background,GUILayout.Width(widthForEach*widthScale[i]));
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            GUI.backgroundColor = originalBackgroundColor;
        }

        //Scroll End
        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        EditorGUILayout.Separator ();
	}
}
//===================================================================================================
public static class SVL
{
    public static double buildTime = 0;
    public static int variantCount = 0;
    public static List<ShaderCompiledVariant> list = new List<ShaderCompiledVariant>();
    public static List<string[]> rowData = new List<string[]>();
    public static string[] columns = new string[] 
    {
        "Shader",
        "PassType",
        "PassName",
        "ShaderType",
        "GfxTier",
        "Platform",
        "Keyword Name",
        "Keyword Type",
        "Keyword Index",
        "Keyword Valid",
        "Keyword Enabled",
        "Duplicates"
    };

    public static int GetVariantDuplicateCount(int k)
    {
        int variantDuplicates = list.Count( o=>
                o.shaderName == list[k].shaderName && 
                o.passType == list[k].passType && 
                o.passName == list[k].passName && 
                o.shaderType == list[k].shaderType && 
                o.graphicsTier == list[k].graphicsTier && 
                o.shaderCompilerPlatform == list[k].shaderCompilerPlatform && 
                o.shaderKeywordName == list[k].shaderKeywordName && 
                o.shaderKeywordType == list[k].shaderKeywordType && 
                o.shaderKeywordIndex == list[k].shaderKeywordIndex && 
                o.isShaderKeywordValid == list[k].isShaderKeywordValid && 
                o.isShaderKeywordEnabled == list[k].isShaderKeywordEnabled
            );
        return variantDuplicates;
    }

    public static void Sorting()
    {
        //sort the list according to shader name
        list = list.OrderBy(o=>o.shaderName).ThenBy(o=>o.shaderType).ThenBy(o=>o.shaderKeywordIndex).ToList();

        //count the duplicates
        for(int k=0; k < list.Count; k++)
        {
            list[k].variantDuplicates = GetVariantDuplicateCount(k);
            list[k].noOfVariantsForThisShader = list.Count(o=>o.shaderName == list[k].shaderName);
        }

        //remove duplicates
        int n = 0;
        while(n < list.Count)
        {
            if( GetVariantDuplicateCount(n) > 1)
            {
                list.Remove(list[n]);
            }
            else
            {
                n++;
            }
        }

        //make string list
        rowData.Clear();
        rowData.Add(columns);
        for(int k=0; k < list.Count; k++)
        {
            rowData.Add(new string[] {
                list[k].shaderName,
                list[k].passType,
                list[k].passName,
                list[k].shaderType,
                list[k].graphicsTier,
                list[k].shaderCompilerPlatform,
                list[k].shaderKeywordName,
                list[k].shaderKeywordType,
                list[k].shaderKeywordIndex,
                list[k].isShaderKeywordValid,
                list[k].isShaderKeywordEnabled,
                list[k].variantDuplicates+""});
        }
    }
}
//===================================================================================================
public class ShaderCompiledVariant
{
    //id
    //public int id { get; set; }

    //snippet
    public string passType { get; set; }
    public string passName { get; set; }
    public string shaderType { get; set; }

    //data
    public string graphicsTier { get; set; }
    public string shaderCompilerPlatform { get; set; }
    //public PlatformKeywordSet platformKeywordSet;
    //public ShaderKeywordSet shaderKeywordSet;
    //public string shaderRequirements;

    //data - PlatformKeywordSet
    //public string platformKeywordName;
    //public string isplatformKeywordEnabled; //from PlatformKeywordSet

    //data - ShaderKeywordSet
    public string shaderKeywordName { get; set; } //ShaderKeyword.GetKeywordName
    public string shaderKeywordType { get; set; } //ShaderKeyword.GetKeywordType
    public string shaderKeywordIndex { get; set; } //ShaderKeyword.index
    public string isShaderKeywordValid { get; set; } //from ShaderKeyword.IsValid()
    public string isShaderKeywordEnabled { get; set; } //from ShaderKeywordSet
    //public ShaderKeyword shaderKeyword; //from ShaderKeywordSet.GetShaderKeywords
    //public string isShaderKeywordLocal { get; set; } //ShaderKeyword.IsKeywordLocal
    //public string globalShaderKeywordName { get; set; }//ShaderKeyword.GetGlobalKeywordName
    //public string globalShaderKeywordType { get; set; } //ShaderKeyword.GetGlobalKeywordType
    
    //==================================== Will not be columns

    //for GUI
    public bool enabled = false;

    //shader
    public string shaderName;
    public int noOfVariantsForThisShader = 0;

    //for sorting
    public int variantDuplicates { get; set; }
};


