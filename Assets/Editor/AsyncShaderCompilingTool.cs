using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;


public class AsyncShaderCompilingTool : EditorWindow
{
	Vector2 scrollPosition;

    //int materialCount = 0;
    UnityEngine.Object[] objects;
    List<Material> materials;
    bool forceSync = false;
    bool allowAsyncCompilation = true;
    bool anythingCompiling = false;

    [MenuItem("Window/AsyncShaderCompiling Tool")]
	public static void ShowWindow ()
	{
		EditorWindow.GetWindow (typeof(AsyncShaderCompilingTool));
	}

    public void Update()
    {
        Repaint();
    }

    void OnGUI () 
	{
        //***************
        scrollPosition = GUILayout.BeginScrollView(scrollPosition,GUILayout.Width(0),GUILayout.Height(0));
		//***************

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.richText = true;

        if(materials == null) materials = new List<Material>();

        //Status - Anything Compiling
        GUILayout.Label ("Status", EditorStyles.boldLabel);
        anythingCompiling = ShaderUtil.anythingCompiling;
        GUI.color = anythingCompiling? Color.magenta : Color.green;
        GUILayout.Label ("Anything compiling : "+anythingCompiling);
        GUI.color = Color.white;
        GUILayout.Space (10);

        //General settings - Allow Async Compilation
        GUILayout.Label ("General", EditorStyles.boldLabel);
        allowAsyncCompilation = EditorGUILayout.Toggle("Allow Async Compilation?",allowAsyncCompilation);
        GUILayout.Space (10);

        //Set Material settings - Force Sync / Pass Index / isPassCompiled
        GUILayout.Label ("Per Material", EditorStyles.boldLabel);
        forceSync = EditorGUILayout.Toggle("Force sync?",forceSync);
        //passIndexMax = EditorGUILayout.IntField("Pass Index Max.",passIndexMax);
 
        //Buttons
        GUILayout.Space (10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button ("Update Selection")) UpdateSelection();
        if (GUILayout.Button ("Update & Compile all passes")) Apply();
        EditorGUILayout.EndHorizontal();

        //Show selected material
        GUILayout.Label ("Materials:", EditorStyles.boldLabel);
        if(materials.Count <= 0)
        {
            GUI.color = Color.cyan;
            GUILayout.Label ("Select material(s) in ProjectView and click Update Selection");
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.cyan;
            GUILayout.Label ("Selected "+materials.Count+" material(s)");
            GUI.color = Color.white;
        }

        //Material list header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label ("Id",style);
        GUILayout.Label ("Material Name",style);
        GUILayout.Label ("Pass compilation status",style);
        EditorGUILayout.EndHorizontal();

        //Display each selected material isPassCompiled status
        for(int i = 0; i < materials.Count; i++)
        {
            if(materials[i] != null)
            {
                EditorGUILayout.BeginHorizontal();

                //id
                GUILayout.Label (i.ToString(),style);

                //name
                GUILayout.Label ("<color=#0ff>"+materials[i].name+"</Color>",style);

                //status
                string text = "";
                for(int j=0; j<materials[i].passCount; j++)
                {
                    if (ShaderUtil.IsPassCompiled(materials[i], j)) text += "<color=#0f0>Y</Color>";
                    else text += "<color=#f0f>N</Color>";
                }
                GUILayout.Label (text,style);

                EditorGUILayout.EndHorizontal();
            }
        }

        //Gap
        GUILayout.Space (15);

        //***************
		GUILayout.EndScrollView();
		//***************
	}

	void UpdateSelection()
	{
        objects = Selection.objects;
        materials.Clear();

        if( objects != null && objects.Length >0 )
        {
            for(int i = 0; i < objects.Length; i++)
            {
                Material mat =  objects[i] as Material;
                if(mat != null)
                {
                    materials.Add(mat);
                }
            }
        }
	}

	void Apply()
	{
        UpdateSelection();

        bool previousState = ShaderUtil.allowAsyncCompilation;

        ShaderUtil.allowAsyncCompilation = allowAsyncCompilation;

        for(int i = 0; i < materials.Count; i++)
        {
            if(materials[i] != null)
            {
                for(int j=0; j<materials[i].passCount; j++)
                {
                    ShaderUtil.CompilePass(materials[i], j, forceSync);
                }
            }
        }

        ShaderUtil.allowAsyncCompilation = previousState;
	}

}