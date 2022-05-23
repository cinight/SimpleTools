using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using System.Linq;
using UnityEditor.SceneManagement;

public class ScenesThatContainThis : EditorWindow
{
    private string msg = "";
    private MonoScript searchType;
    private Dictionary<string, int> result = new Dictionary<string, int>();

    [MenuItem("Window/ScenesThatContainThis")]
	public static void ShowWindow ()
	{
		EditorWindow.GetWindow (typeof(ScenesThatContainThis));
	}

    public void Update()
    {
        Repaint();
    }

    void OnGUI () 
	{
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.wordWrap = true;
        labelStyle.richText = true;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.wordWrap = true;
        buttonStyle.richText = true;
        buttonStyle.alignment = TextAnchor.MiddleLeft;

		GUI.color = Color.cyan;
		GUILayout.Label ("Scenes that contain objects that are using this script:", EditorStyles.boldLabel);

        searchType = (MonoScript)EditorGUILayout.ObjectField("Assign a script",searchType, typeof(MonoScript), false);

        GUI.color = Color.white;
        GUILayout.Space(15);

        //Button
		if (GUILayout.Button ("Let's check") && searchType != null) DoJob();

        GUILayout.Space(15);

        //Result
        GUILayout.Label (msg, labelStyle);
        if(result.Count>0)
        {
            for( int i=0; i<result.Count; i++ )
            {
                string scenepath = result.ElementAt(i).Key;
                int objcount = result[scenepath];

                if(GUILayout.Button(scenepath + " <color=#ffff00ff>"+objcount + " objects"+"</color>",buttonStyle))
                {
                    OpenScene(scenepath);
                }
            }
            GUI.color = Color.cyan;
            if (GUILayout.Button ("Remove component from all the scenes")) RemoveComponent();
            GUI.color = Color.white;
        }

        GUILayout.Space(15);
	}

	void DoJob()
	{
        GUI.color = Color.white;
        msg = "Search for : <color=#00ffffff>"+searchType.GetClass().ToString()+"</color>"+"\n";

        //Get all the scenes in project
        var scenesGUIDs = AssetDatabase.FindAssets("t:Scene",new[] {"Assets/"});
        msg += "Search in : "+scenesGUIDs.Length+" scenes"+"\n";

        if(result == null) result = new Dictionary<string, int>();
        result.Clear();
        for(int i=0; i<scenesGUIDs.Length; i++)
        {
            //Open the scene
            string scenePath = AssetDatabase.GUIDToAssetPath(scenesGUIDs[i]);
            OpenScene(scenePath);
            Scene currentScene = EditorSceneManager.GetSceneAt(0);
            EditorSceneManager.SetActiveScene(currentScene);

            //See if the scene contains the component we search for
            Object[] subsceneObjs = UnityEngine.GameObject.FindObjectsOfType(searchType.GetClass(),true);
            if(subsceneObjs.Length > 0)
            {
                result.Add(scenePath,subsceneObjs.Length);
            }
        }

        msg += "Result : "+result.Count+" scenes"+"\n";
        msg +="\n";
	}

    void RemoveComponent()
    {
        for( int i=0; i<result.Count; i++ )
        {
            string scenepath = result.ElementAt(i).Key;

            //Open the scene
            OpenScene(scenepath);
            Scene currentScene = EditorSceneManager.GetSceneAt(0);
            EditorSceneManager.SetActiveScene(currentScene);

            //Search adn remove the component
            Object[] subsceneObjs = UnityEngine.GameObject.FindObjectsOfType(searchType.GetClass(),true);
            for( int j=0; j<subsceneObjs.Length; j++ )
            {
                DestroyImmediate (subsceneObjs[j]);
            }

            //Save the scene
            EditorSceneManager.SaveScene(currentScene);
        }
    }

    private void OpenScene(string path)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Scene currentScene = EditorSceneManager.GetSceneAt(0);
        EditorSceneManager.SetActiveScene(currentScene);
    }
}