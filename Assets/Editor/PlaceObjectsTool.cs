using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;


public class PlaceObjectsTool : EditorWindow
{
	Vector2 scrollPosition;
    string  errorMsg = "";
    Transform[] objs;
    bool isParentGameObject = false;
    int selectedCount = 0;
    int childCount = 0;

	bool    tg_distrbute = false;
    bool    tg_ran = false;
    bool    tg_ran_pos = false;
    bool    tg_ran_rot = false;
    bool    tg_ran_sca = false;

    //Distribute evenly
    Vector3 spacing            = Vector3.one; //-1 means no limit
    Vector3Int amount             = new Vector3Int(10,10,10);

    //Align
    int align = 1;
    string[] alignList = new string[] 
    {
        "-X", //0
        "+X", //1
        "-Y", //2
        "+Y", //3
        "-Z", //4
        "+Z", //5
    };

    //Random
    Vector3 ran_pos_min        = Vector3.zero;
    Vector3 ran_pos_max        = Vector3.zero;
    Vector3 ran_rot_min        = Vector3.zero;
    Vector3 ran_rot_max        = Vector3.zero;
    Vector3 ran_sca_min        = Vector3.one;
    Vector3 ran_sca_max        = Vector3.one;

    [MenuItem("Window/Place Objects Tool")]
	public static void ShowWindow ()
	{
		EditorWindow.GetWindow (typeof(PlaceObjectsTool));
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

        //Selection
        GUILayout.Space(10);
		GUILayout.Label ("Select objects OR select 1 parent object in Hierarchy", EditorStyles.wordWrappedLabel);
        GUI.color = Color.cyan;
        UpdateSelectionStatus();
        if(errorMsg == "") GUILayout.Label("Objects Selected : " + (isParentGameObject? ""+childCount : ""+selectedCount) );
        GUI.color = Color.white;
        GUILayout.Space(15);

        //Distribute Evenly
        tg_distrbute = EditorGUILayout.BeginToggleGroup ("Distribute Evenly", tg_distrbute); // Untick for free random
            EditorGUI.indentLevel++;
            spacing = EditorGUILayout.Vector3Field("Spacing", spacing);
            amount = EditorGUILayout.Vector3IntField("Amount", amount);
            EditorGUI.indentLevel--;
        EditorGUILayout.EndToggleGroup();
        GUILayout.Space(15);

        //Random title
        tg_ran = EditorGUILayout.BeginToggleGroup ("Random", tg_ran); // Untick for free random
        if (GUILayout.Button("Reset Random Numbers")) ResetRandom();
        //Random Position
        tg_ran_pos = EditorGUILayout.Foldout(tg_ran_pos, "Random Position");
        if(tg_ran_pos)
        {
            ran_pos_min = EditorGUILayout.Vector3Field("Min", ran_pos_min);
            ran_pos_max = EditorGUILayout.Vector3Field("Max", ran_pos_max);
        }
        //Random Rotation
        tg_ran_rot = EditorGUILayout.Foldout (tg_ran_rot, "Random Rotation");
        if(tg_ran_rot)
        {
            ran_rot_min = EditorGUILayout.Vector3Field("Min", ran_rot_min);
            ran_rot_max = EditorGUILayout.Vector3Field("Max", ran_rot_max);
        }
        //Random Scale
        tg_ran_sca = EditorGUILayout.Foldout (tg_ran_sca, "Random Scale");
        if(tg_ran_sca)
        {
            ran_sca_min = EditorGUILayout.Vector3Field("Min", ran_sca_min);
            ran_sca_max = EditorGUILayout.Vector3Field("Max", ran_sca_max);
        }
        EditorGUILayout.EndToggleGroup();
        GUILayout.Space(15);

        //Buttons
        Color original = GUI.backgroundColor;
        EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button ("Place Objects")) PlaceObjects();
            GUI.backgroundColor = original;
            if (GUILayout.Button("Reset Object Transforms")) ResetTransform();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(15);

        //Align Objects
        EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField("Align Objects");
        align = EditorGUILayout.Popup("Align to", align, alignList);
        if (GUILayout.Button ("Align Objects")) AlignObjects();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(15);

        //Error
        GUI.color = Color.red;
        if(errorMsg != "")
        {
            GUILayout.Label("Error", EditorStyles.boldLabel);
            GUILayout.Label(errorMsg, EditorStyles.wordWrappedLabel);
        }
        GUILayout.Space(15);

        //***************
		GUILayout.EndScrollView();
		//***************
	}

    void UpdateSelectionStatus()
    {
        if (Selection.activeTransform != null)
        {
            selectedCount = Selection.gameObjects.Length;
            childCount = Selection.activeTransform.childCount;
            isParentGameObject = selectedCount == 1 && childCount > 0 ? true : false;
        }
        else
        {
            selectedCount = 0;
            childCount = 0;
        }
    }

    void SetSelectionObjects()
    {
        errorMsg = "";

        if (Selection.activeTransform == null)
        {
            errorMsg += "\n"+"No object is selected in Hierarchy";
            return;
        }

        UpdateSelectionStatus();

        //For enabling undo to work
        for(int i=0; i<objs.Length; i++)
        {
            Undo.RecordObject(objs[i],"PlaceObjectsTool");
        }

        if(!isParentGameObject)
        {
            int size = selectedCount;
            objs = new Transform[size];
            for(int i=0; i<objs.Length; i++)
            {
                objs[i] = Selection.gameObjects[i].transform;
            }
        }
        else
        {
            int size = childCount;
            objs = new Transform[size];
            //0 is parent itself so we need the temp array to skip the first one
            var temp = Selection.activeTransform.GetComponentsInChildren<Transform>(); 
            for(int i=0; i<objs.Length; i++)
            {
                objs[i] = temp[i+1];
            }
        }

        //Prevent no object selected
        if (objs.Length <= 0)
        {
            errorMsg += "\n"+"No object is selected in Hierarchy";
        }
    }

    void ResetRandom()
    {
        ran_pos_min = Vector3.zero;
        ran_pos_max = Vector3.zero;
        ran_rot_min = Vector3.zero;
        ran_rot_max = Vector3.zero;
        ran_sca_min = Vector3.one;
        ran_sca_max = Vector3.one;
    }

    void ResetTransform()
    {
        SetSelectionObjects();

        //Prevent error
        if(errorMsg != "") return;

        for (int i = 0; i < objs.Length; i++)
        {
            objs[i].localPosition = Vector3.zero;
            objs[i].localEulerAngles = Vector3.zero;
            objs[i].localScale = Vector3.one;
        }
    }

    void PlaceObjects()
	{
        SetSelectionObjects();

        // Prevent negative amount
        if ( amount.x <= 0 || amount.y <= 0 || amount.z <= 0 )
        {
            errorMsg += "\n"+"Please put positive integer number for Amount";
        }

        //Prevent error
        if(errorMsg != "") return;

        //Distribute evenly
        if (tg_distrbute)
        {
            //Coordinate
            Vector3 coordinate = Vector3.zero;
            Vector3 coordinate_max = Vector3.zero;

            //Temp
            Vector3 temp_p = Vector3.zero;
            Vector3 temp_r = Vector3.zero;
            Vector3 temp_s = Vector3.one;

            for (int i = 0; i < objs.Length; i++)
            {
                temp_p.x = coordinate.x * spacing.x;
                temp_p.y = coordinate.y * spacing.y;
                temp_p.z = coordinate.z * spacing.z;
                objs[i].localPosition = temp_p;

                if (coordinate.x < amount.x - 1)
                {
                    coordinate.x++;
                    coordinate_max.x = Mathf.Max(coordinate_max.x, coordinate.x);
                }
                else
                {
                    coordinate.x = 0;
                    if (coordinate.y < amount.y - 1)
                    {
                        coordinate.y++;
                        coordinate_max.y = Mathf.Max(coordinate_max.y, coordinate.y);
                    }
                    else
                    {
                        coordinate.y = 0;
                        coordinate.z++;
                        coordinate_max.z = Mathf.Max(coordinate_max.z, coordinate.z);
                    }
                }
            }
        }

        //Random position
        if (tg_ran && tg_ran_pos)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                if(tg_distrbute)
                {
                    //Small Variations
                    objs[i].localPosition += RandomV3(ran_pos_min, ran_pos_max);
                }
                else
                {
                    objs[i].localPosition = RandomV3(ran_pos_min, ran_pos_max);
                }
            }
        }

        //Random rotation
        if (tg_ran && tg_ran_rot)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                if(tg_distrbute)
                {
                    //Small Variations
                    objs[i].localEulerAngles += RandomV3(ran_rot_min, ran_rot_max);
                }
                else
                {
                    Vector3 rot = RandomV3(ran_rot_min, ran_rot_max);
                    objs[i].localRotation = Quaternion.Euler(rot.x, rot.y, rot.z);
                }
            }
        }

        //Random scale
        if (tg_ran && tg_ran_sca)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i].localScale = RandomV3(ran_sca_min, ran_sca_max);
                
            }
        }
    }

    private Vector3 RandomV3(Vector3 rmin, Vector3 rmax)
    {
        Vector3 v;
        v.x = UnityEngine.Random.Range(rmin.x, rmax.x);
        v.y = UnityEngine.Random.Range(rmin.y, rmax.y);
        v.z = UnityEngine.Random.Range(rmin.z, rmax.z);
        return v;
    }

    void AlignObjects()
    {
        SetSelectionObjects();

        //Prevent error
        if(errorMsg != "") return;

        float edge = 0;

        switch(align)
        {
            case 0: 
                for (int i = 0; i < objs.Length; i++) edge = Mathf.Min(edge,objs[i].localPosition.x);
                for (int i = 0; i < objs.Length; i++) objs[i].localPosition = new Vector3( edge , objs[i].localPosition.y , objs[i].localPosition.z );
            break;
            case 1: 
                for (int i = 0; i < objs.Length; i++) edge = Mathf.Max(edge,objs[i].localPosition.x);
                for (int i = 0; i < objs.Length; i++) objs[i].localPosition = new Vector3( edge , objs[i].localPosition.y , objs[i].localPosition.z );
            break;
            case 2: 
                for (int i = 0; i < objs.Length; i++) edge = Mathf.Min(edge,objs[i].localPosition.y);
                for (int i = 0; i < objs.Length; i++) objs[i].localPosition = new Vector3( objs[i].localPosition.x , edge , objs[i].localPosition.z );
            break;
            case 3: 
                for (int i = 0; i < objs.Length; i++) edge = Mathf.Max(edge,objs[i].localPosition.y);
                for (int i = 0; i < objs.Length; i++) objs[i].localPosition = new Vector3( objs[i].localPosition.x , edge , objs[i].localPosition.z );
            break;
            case 4: 
                for (int i = 0; i < objs.Length; i++) edge = Mathf.Min(edge,objs[i].localPosition.z);
                for (int i = 0; i < objs.Length; i++) objs[i].localPosition = new Vector3( objs[i].localPosition.x , objs[i].localPosition.y , edge );
            break;
            case 5:
                for (int i = 0; i < objs.Length; i++) edge = Mathf.Max(edge,objs[i].localPosition.z);
                for (int i = 0; i < objs.Length; i++) objs[i].localPosition = new Vector3( objs[i].localPosition.x , objs[i].localPosition.y , edge );
            break;
        }
    }

}