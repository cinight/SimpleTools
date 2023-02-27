using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace GfxQA.ShaderVariantTool
{
    public class ShaderVariantTool_LogReader : EditorWindow
    {
        private List<string> timeStamps;
        private int selectedTimeStamp = 0;
        private string editorLogPath = "";
        private string savedFile = "";
        private CultureInfo culture = new CultureInfo("is-IS");

        [MenuItem("Window/ShaderVariantTool_LogReader")]
        public static void ShowWindow ()
        {
            var window = EditorWindow.GetWindow (typeof(ShaderVariantTool_LogReader));
            window.name = "ShaderVariantTool LogReader";
            window.titleContent = new GUIContent("ShaderVariantTool LogReader");
        }

        public void Awake()
        {
            editorLogPath = Helper.GetEditorLogPath();
        }

        private void UpdateTimeStampList()
        {
            timeStamps = new List<string>();
            FileStream fs = new FileStream(editorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (StreamReader sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if(line.Contains(SVL.buildProcessIDTitleStart))
                    {
                        timeStamps.Add(line);
                    }
                }
            }
        }

        // public void OnDestroy()
        // {
        // }

        void OnGUI () 
        {
            Color originalBackgroundColor = GUI.backgroundColor;

            //Width for the columns & style
            float currentSize = this.position.width;
            GUIStyle background = new GUIStyle 
            { 
                normal = 
                { 
                    background = Texture2D.whiteTexture,
                    textColor = Color.white
                } 
            };

            //Title
            GUI.color = Color.cyan;
            GUILayout.Label ("This log reader is for reading previous builds, which only reads information about variant count before/after stripping per shader. \n"+"Therefore detailed shader keywords and compute shader information is not available. So please only use this if the original ShaderVariantTool doesn't work.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            GUI.color = Color.white;

            //Editor.Log path textbox
            editorLogPath = GUILayout.TextField(editorLogPath);

            //TimeStamp select: drop down and a button to update drop down
            if(timeStamps== null) UpdateTimeStampList();
            selectedTimeStamp = EditorGUILayout.Popup("Select timestamp", selectedTimeStamp, timeStamps.ToArray());
            if (GUILayout.Button ("Update TimeStampList",GUILayout.Width(200)))
            {
                UpdateTimeStampList();
            }

            //Read log and save the CSV file
            if (GUILayout.Button ("Read log",GUILayout.Width(200)))
            {
                //Write File
                List<string[]> outputRows = ShaderVariantTool_BuildPostprocess.WriteCSVFile(null,editorLogPath,timeStamps[selectedTimeStamp]);

                //Save File
                string fileName = "ShaderVariant_ReadLog_"+timeStamps[selectedTimeStamp].Replace(SVL.buildProcessIDTitleStart,"");
                savedFile = ShaderVariantTool_BuildPostprocess.SaveCSVFile(outputRows, fileName);
                Debug.Log("ShaderVariantTool_LogReader has generated CSV report at: "+savedFile);

                //CleanUp
                outputRows.Clear();
            }

            //Display CSV generation result
            if(savedFile != "")
            {
                //Saved file path
                GUI.color = Color.green;
                GUILayout.Label ( "Saved: "+savedFile, EditorStyles.wordWrappedLabel);
            }

            //End Window
            GUILayout.FlexibleSpace();
            EditorGUILayout.Separator();
        }
    }
}
