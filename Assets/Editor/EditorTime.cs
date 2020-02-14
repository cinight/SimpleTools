using UnityEditor;
using UnityEngine;
using System;

public class EditorTime : EditorWindow
{
	Vector2 scrollPosition;
    float threshold = 0.17f;
    double prevtime = 0;

    [MenuItem("Window/EditorTime")]
	public static void ShowWindow ()
	{
        GetWindow(typeof(EditorTime));
	}

    void Update()
    {
        Repaint();

        // double dt = Time.unscaledDeltaTime;
        double dt = EditorApplication.timeSinceStartup - prevtime;
        if (dt <= threshold )
        {
            Debug.Log("Update Unscaled Delta Time : <color=#00ff00>Below threshold " + threshold + "</color>");
        }
        else
        {
            Debug.Log("Update Unscaled Delta Time : <color=#ffff00>" + dt.ToString("0.000") + "</color> seconds at "+ DateTime.Now);
        }
        prevtime = EditorApplication.timeSinceStartup;
        EditorUtility.SetDirty(this);
    }

    void OnGUI () 
	{
        //***************
        scrollPosition = GUILayout.BeginScrollView(scrollPosition,GUILayout.Width(0),GUILayout.Height(0));
		//***************

		GUI.color = Color.cyan;
		GUILayout.Label ("Editor Delta Time", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("See result in Console", EditorStyles.miniLabel);
        GUI.color = Color.white;
        GUILayout.Space(15);
        threshold = EditorGUILayout.FloatField("Threshold", threshold);

        //***************
        GUILayout.EndScrollView();
		//***************
	}

}