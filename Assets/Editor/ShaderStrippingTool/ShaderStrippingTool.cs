using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Experimental.AssetImporters;
using Unity.Collections;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Reflection;

public class ShaderStrippingTool : EditorWindow
{
	Vector2 scrollPosition;

    public static bool sorted = false;
    
    //ColumnSetup
    Color columnColor1 = new Color(0.3f,0.3f,0.3f,1);
    Color columnColor2 = new Color(0.28f,0.28f,0.28f,1);
    string[] columns = new string[]
    {
        //"id",

        //"Shader\nName",

        "Snippet\nPassType",
        "Snippet\nPassName",
        "Snippet\nShaderType",

        "Data\nGraphics\nTier",
        "Data\nCompiler\nPlatform", //Shader Compiler Platform
        //"Data-SdrReq",

        "Keyword\nName",
        "Keyword\nType",
        "Keyword\nIndex",
        "Keyword\nValid",
        "Keyword\nEnabled",
        //"Keyword\nLocal",
        //"Keyword\nGlobalName",
        //"Keyword\nGlobalType"
        "Duplicates"
    };
    float[] widthScale = new float[]
    {
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

        //Title and page slot
        GUI.color = Color.cyan;
        GUILayout.Label ("Build the player and see the variants list here.", EditorStyles.wordWrappedLabel);
        if(SVL.list !=null)
        {
            GUILayout.Label ( "Variant Count : " + MyCustomBuildProcessor.variantCount, EditorStyles.wordWrappedLabel );
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label ( "List is null", EditorStyles.wordWrappedLabel);

            //Initial setup
            sorted = false;
        }
        GUI.color = Color.white;
        GUILayout.Space(15);

        //Width for the columns & style
        float currentSize = this.position.width;
        float widthForEach = currentSize / (columns.Length+currentSize*0.0002f);
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
        for(int i=0;i<columns.Length;i++)
        {
            int al = i%2;
            GUI.backgroundColor = al ==0 ? columnColor1 :columnColor2;
            GUILayoutOption[] columnLayoutOption = new GUILayoutOption[]
            {
                GUILayout.Width(Mathf.RoundToInt(widthForEach*widthScale[i])),
                GUILayout.Height(55)
            };
            EditorGUILayout.LabelField (columns[i],background,columnLayoutOption);
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
            if(!sorted) 
            {
                //sort the list according to shader name
                SVL.list = SVL.list.OrderBy(o=>o.shaderName).ThenBy(o=>o.shaderType).ThenBy(o=>o.shaderKeywordIndex).ToList();
                
                for(int k=0; k < SVL.list.Count; k++)
                {
                    //count the duplicates
                    SVL.list[k].variantDuplicates = SVL.GetVariantDuplicateCount(k);
                    SVL.list[k].noOfVariantsForThisShader = SVL.list.Count(o=>o.shaderName == SVL.list[k].shaderName);
                }

                //remove duplicates
                int n = 0;
                while(n < SVL.list.Count)
                {
                    if( SVL.GetVariantDuplicateCount(n) > 1)
                    {
                        SVL.list.Remove(SVL.list[n]);
                    }
                    else
                    {
                        n++;
                    }
                }

                //set the total shadercompilation time back to original set
                for(int k=0; k < SVL.list.Count; k++)
                {
                    SVL.list[k].shaderCompilationTime = MyCustomBuildProcessor.shaderCompilationTime[SVL.list[k].shaderName];
                }

                //Debug.Log("count="+SVL.list.Count);

                sorted = true;
            }
           
            string currentShader = "";
            for(int k=0; k < SVL.list.Count; k++)
            {
                //Shader foldout
                if(SVL.list[k].shaderName != currentShader)
                {
                    GUI.backgroundColor = originalBackgroundColor;
                    SVL.list[k].enabled = EditorGUILayout.Foldout( 
                        SVL.list[k].enabled, SVL.list[k].shaderName + 
                        " (" + SVL.list[k].noOfVariantsForThisShader + ")" +
                        "      Compilation Time = "+SVL.list[k].shaderCompilationTime.ToString("0.000")+" seconds"
                        );
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
                    for(int i=0;i<columns.Length;i++)
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

public static class SVL
{
    public static List<ShaderCompiledVariant> list;

    public static int GetVariantDuplicateCount(int k)
    {
        int variantDuplicates = SVL.list.Count( o=>
                o.shaderName == SVL.list[k].shaderName && 
                o.passType == SVL.list[k].passType && 
                o.passName == SVL.list[k].passName && 
                o.shaderType == SVL.list[k].shaderType && 
                o.graphicsTier == SVL.list[k].graphicsTier && 
                o.shaderCompilerPlatform == SVL.list[k].shaderCompilerPlatform && 
                o.shaderKeywordName == SVL.list[k].shaderKeywordName && 
                o.shaderKeywordType == SVL.list[k].shaderKeywordType && 
                o.shaderKeywordIndex == SVL.list[k].shaderKeywordIndex && 
                o.isShaderKeywordValid == SVL.list[k].isShaderKeywordValid && 
                o.isShaderKeywordEnabled == SVL.list[k].isShaderKeywordEnabled
            );
        return variantDuplicates;
    }
}

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
    public double shaderCompilationTime = 0;

    //for sorting
    public int variantDuplicates { get; set; }

};


