using UnityEditor;
using UnityEngine;
using System;

public class RenameObjectsTool : EditorWindow
{
	Vector2 scrollPosition;

	string prefix;
	string suffix;
	string replace;
	string replace_By;
	string insert_Before;
	string insert_Before_Word;
	string insert_After;
	string insert_After_Word;

    [MenuItem("Window/RenameObjectsTool")]
	public static void ShowWindow ()
	{
        GetWindow(typeof(RenameObjectsTool));
	}

    void OnGUI () 
	{
        //***************
        scrollPosition = GUILayout.BeginScrollView(scrollPosition,GUILayout.Width(0),GUILayout.Height(0));
		//***************

        //Title
		GUI.color = Color.cyan;
		GUILayout.Label ("RenameObjectsTool", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Select objects in Hierarchy or ProjectView", EditorStyles.miniLabel);
        GUI.color = Color.white;
      
        //Body
        prefix = EditorGUILayout.TextField("Add Prefix", prefix);
        suffix = EditorGUILayout.TextField("Add Suffix", suffix);
        GUILayout.Space (10);
        replace = EditorGUILayout.TextField("Replace Text", replace);
        replace_By = EditorGUILayout.TextField("Replace By", replace_By);
        GUILayout.Space (10);
        insert_Before = EditorGUILayout.TextField("Insert Before Text", insert_Before);
        insert_Before_Word = EditorGUILayout.TextField("Insert Before By", insert_Before_Word);
        GUILayout.Space (10);
        insert_After = EditorGUILayout.TextField("Insert After Text", insert_After);
        insert_After_Word = EditorGUILayout.TextField("Insert After By", insert_After_Word);
        GUILayout.Space (10);
        if (GUILayout.Button ("Change Names"))
            ChangeObjNames();
        GUILayout.Space (30);

        //***************
        GUILayout.EndScrollView();
		//***************
	}

 	void ChangeObjNames()
	{
		UnityEngine.Object[] list = Selection.objects;
        Debug.Log("Selected" + list.Length);
        foreach (UnityEngine.Object go in list)
        {
            string newName = Rename(go.name);
            go.name = newName; //This will work for hierarchy objects
            string path = AssetDatabase.GetAssetPath(go);
            AssetDatabase.RenameAsset(path,newName); //This will work for ProjectView files
        }
        AssetDatabase.Refresh();
	}

    private string Rename(string input)
    {
        string output = input;
        output = prefix+output+suffix;
        if(replace != null && replace != "") output = output.Replace(replace,replace_By);
        if(insert_Before != null && insert_Before != "") output = output.Insert(output.IndexOf(insert_Before),insert_Before_Word);
        if(insert_After != null && insert_After != "") output = output.Insert(output.IndexOf(insert_After)+insert_After.Length,insert_After_Word);

        return output;
    }

}