using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;


public class ScreenCaptureEditorTool : EditorWindow
{
    string msg = "no error";

    [MenuItem("Window/SimpleScreenCapture")]
	public static void ShowWindow ()
	{
		EditorWindow.GetWindow (typeof(ScreenCaptureEditorTool));
	}

    public void Update()
    {
        Repaint();
    }

    void OnGUI () 
	{
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.richText = true;

		GUI.color = Color.cyan;
		GUILayout.Label ("ScreenCapture", EditorStyles.boldLabel);
        GUI.color = Color.white;
        GUILayout.Space(15);

        //Take screenshot button
		if (GUILayout.Button ("Make screenshot of GameView"))
			DoJob();

        //Saved file
        GUILayout.Label( msg , style);

        //Show folder button
        if (GUILayout.Button ("Show in explorer"))
        {
            string folderPath = Application.dataPath;
            folderPath = folderPath.Replace(@"/", @"\");   // explorer doesn't like front slashes
            System.Diagnostics.Process.Start("explorer.exe", "/select,"+folderPath);
        }
        GUILayout.Space(15);
	}

	void DoJob()
	{
        GUI.color = Color.white;
        Scene scene = SceneManager.GetActiveScene();
        string fileName = "Capture_"+scene.name+DateTime.Now.ToString("yyyyMMdd_hh-mm-ss")+".PNG";
        ScreenCapture.CaptureScreenshot(fileName);

        msg = "Saved : <color=#00ff00>"+fileName+"</color>";
	}

}