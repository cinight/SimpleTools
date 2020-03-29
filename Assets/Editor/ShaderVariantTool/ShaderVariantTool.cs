using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class ShaderVariantTool : EditorWindow
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

    [MenuItem("Window/ShaderVariantTool")]
	public static void ShowWindow ()
	{
		var window = EditorWindow.GetWindow (typeof(ShaderVariantTool));
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
            GUILayout.Label ( "Shader Count : " + SVL.shaderlist.Count, EditorStyles.wordWrappedLabel );
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
        if(SVL.shaderlist.Count >0 && SVL.rowData.Count > 0)
        {
            for(int k=1; k < SVL.rowData.Count; k++) //first row is title so start with 1
            {
                string shaderName = SVL.rowData[k][0];
                int shaderIndex = SVL.shaderlist.FindIndex( o=> o.name == shaderName );
                CompiledShader currentShader = SVL.shaderlist[shaderIndex];
                
                if(shaderName != SVL.rowData[k-1][0]) //show title
                {
                    GUI.backgroundColor = originalBackgroundColor;
                    currentShader.guiEnabled = EditorGUILayout.Foldout( currentShader.guiEnabled, shaderName + " (" + currentShader.noOfVariantsForThisShader + ")" );
                    SVL.shaderlist[shaderIndex] = currentShader;
                }

                //Show the shader variants
                if( currentShader.guiEnabled )
                {
                    EditorGUILayout.BeginHorizontal();
                    for(int i=1;i<SVL.columns.Length;i++)
                    {
                        string t = SVL.rowData[k][i];

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
                GUI.backgroundColor = originalBackgroundColor;
            }
        }

        //Scroll End
        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        EditorGUILayout.Separator();
	}
}
//===================================================================================================
public static class SVL
{
    public static double buildTime = 0;
    public static int variantCount = 0;
    public static List<CompiledShaderVariant> variantlist = new List<CompiledShaderVariant>();
    public static List<CompiledShader> shaderlist = new List<CompiledShader>();
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
        int variantDuplicates = variantlist.Count( o=>
                o.shaderName == variantlist[k].shaderName && 
                o.passType == variantlist[k].passType && 
                o.passName == variantlist[k].passName && 
                o.shaderType == variantlist[k].shaderType && 
                o.graphicsTier == variantlist[k].graphicsTier && 
                o.shaderCompilerPlatform == variantlist[k].shaderCompilerPlatform && 
                o.shaderKeywordName == variantlist[k].shaderKeywordName && 
                o.shaderKeywordType == variantlist[k].shaderKeywordType && 
                o.shaderKeywordIndex == variantlist[k].shaderKeywordIndex && 
                o.isShaderKeywordValid == variantlist[k].isShaderKeywordValid && 
                o.isShaderKeywordEnabled == variantlist[k].isShaderKeywordEnabled
            );
        return variantDuplicates;
    }

    public static void Sorting()
    {
        //sort the list according to shader name
        variantlist = variantlist.OrderBy(o=>o.shaderName).ThenBy(o=>o.shaderType).ThenBy(o=>o.shaderKeywordIndex).ToList();

        //count the duplicates
        for(int k=0; k < variantlist.Count; k++)
        {
            CompiledShaderVariant temp = variantlist[k];
            temp.variantDuplicates = GetVariantDuplicateCount(k);
            variantlist[k] = temp;
        }

        //remove duplicates
        int n = 0;
        while(n < variantlist.Count)
        {
            if( GetVariantDuplicateCount(n) > 1)
            {
                variantlist.Remove(variantlist[n]);
            }
            else
            {
                n++;
            }
        }

        //make string lists
        rowData.Clear();
        rowData.Add(columns);
        for(int k=0; k < variantlist.Count; k++)
        {
            rowData.Add(new string[] {
                variantlist[k].shaderName,
                variantlist[k].passType,
                variantlist[k].passName,
                variantlist[k].shaderType,
                variantlist[k].graphicsTier,
                variantlist[k].shaderCompilerPlatform,
                variantlist[k].shaderKeywordName,
                variantlist[k].shaderKeywordType,
                variantlist[k].shaderKeywordIndex,
                variantlist[k].isShaderKeywordValid,
                variantlist[k].isShaderKeywordEnabled,
                variantlist[k].variantDuplicates+""});
        }

        //clean up
        variantlist.Clear();
    }
}
//===================================================================================================
public struct CompiledShader
{
    public string name;
    public bool guiEnabled;
    public int noOfVariantsForThisShader;
};
public struct CompiledShaderVariant
{
    //shader
    public string shaderName;

    //snippet
    public string passType;
    public string passName;
    public string shaderType;

    //data
    public string graphicsTier;
    public string shaderCompilerPlatform;
    //public string shaderRequirements;

    //data - PlatformKeywordSet
    //public string platformKeywordName;
    //public string isplatformKeywordEnabled; //from PlatformKeywordSet

    //data - ShaderKeywordSet
    public string shaderKeywordName; //ShaderKeyword.GetKeywordName
    public string shaderKeywordType; //ShaderKeyword.GetKeywordType
    public string shaderKeywordIndex; //ShaderKeyword.index
    public string isShaderKeywordValid; //from ShaderKeyword.IsValid()
    public string isShaderKeywordEnabled; //from ShaderKeywordSet
    //public ShaderKeyword shaderKeyword; //from ShaderKeywordSet.GetShaderKeywords
    //public string isShaderKeywordLocal { get; set; } //ShaderKeyword.IsKeywordLocal
    //public string globalShaderKeywordName { get; set; }//ShaderKeyword.GetGlobalKeywordName
    //public string globalShaderKeywordType { get; set; } //ShaderKeyword.GetGlobalKeywordType
    
    //for sorting
    public int variantDuplicates;
};


