using UnityEditor;
using UnityEngine;

public class PlaceObjectsTool : EditorWindow
{
	Vector2 scrollPosition;

    //Selections
    Transform[] objs;
    bool isParentGameObject = false;
    int selectedCount = 0;
    int childCount = 0;

    //CopyPasteTransform
    Transform copyFromTrans;

    //ReplaceGameObject
    GameObject replaceSrc;

    //Toggles
	bool    tg_distrbute = false;
    bool    tg_ran = false;
    bool    tg_ran_pos = false;
    bool    tg_ran_rot = false;
    bool    tg_ran_sca = false;
    bool    tg_ran_scaUniform = false;
    bool    tg_ranSphere = false;

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
    private float tg_ranSphere_radius = 1f;

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
        UpdateSelectionStatus();
        if(childCount == 0 && selectedCount == 0)
        {
            EditorGUILayout.HelpBox("No object is selected in Hierarchy", MessageType.Error);
        }
        else
        {
            GUI.color = Color.cyan;
            GUILayout.Label("Objects Selected : " + (isParentGameObject? ""+childCount : ""+selectedCount) );
            GUI.color = Color.white;
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        //Distribute Evenly
        tg_distrbute = EditorGUILayout.BeginToggleGroup ("Distribute Evenly", tg_distrbute); // Untick for free random
            EditorGUI.indentLevel++;
            spacing = EditorGUILayout.Vector3Field("Spacing", spacing);
            amount = EditorGUILayout.Vector3IntField("Amount", amount);
            if ( amount.x <= 0 || amount.y <= 0 || amount.z <= 0 )
            {
                EditorGUILayout.HelpBox("Please put positive integer number for Amount", MessageType.Error);
            }
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
            tg_ran_scaUniform = EditorGUILayout.Toggle("Uniform Scale", tg_ran_scaUniform);
            if (tg_ran_scaUniform)
            {
                ran_sca_min.x = EditorGUILayout.FloatField("Min", ran_sca_min.x);
                ran_sca_max.x = EditorGUILayout.FloatField("Max", ran_sca_max.x);
            }
            else
            {
                ran_sca_min = EditorGUILayout.Vector3Field("Min", ran_sca_min);
                ran_sca_max = EditorGUILayout.Vector3Field("Max", ran_sca_max);
            }
        }
        EditorGUILayout.EndToggleGroup();
        GUILayout.Space(15);

        //Random title
        tg_ranSphere = EditorGUILayout.BeginToggleGroup ("Random In Sphere", tg_ranSphere);
        tg_ranSphere_radius = EditorGUILayout.FloatField("Radius", tg_ranSphere_radius);
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

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        //Align Objects
        EditorGUILayout.BeginHorizontal();
        align = EditorGUILayout.Popup("Align to", align, alignList);
        if (GUILayout.Button ("Align Objects")) AlignObjects();
        EditorGUILayout.EndHorizontal();
 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        //Place Objects to Ground
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button ("Move Objects to Ground")) MoveObjectToGround();
        EditorGUILayout.EndHorizontal();
 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        //Copy paste object transform
        copyFromTrans = (Transform)EditorGUILayout.ObjectField("Copy transform from",copyFromTrans, typeof(Transform), true);
        EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button ("Paste Transform")) CopyPasteTransform(0);
            GUI.backgroundColor = original;
            if (GUILayout.Button ("Paste Position")) CopyPasteTransform(1);
            if (GUILayout.Button ("Paste Rotation")) CopyPasteTransform(2);
            if (GUILayout.Button ("Paste Scale")) CopyPasteTransform(3);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        //Replace GameObject
        GUILayout.Label("Replace scene object by object", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        replaceSrc = (GameObject)EditorGUILayout.ObjectField("Source",replaceSrc, typeof(GameObject), true);
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button ("Replace objects")) ReplaceObjects();
            GUI.backgroundColor = original;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Cannot undo", MessageType.Warning);


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
        objs = null;
        if (Selection.activeTransform == null)
        {
            selectedCount = 0;
            childCount = 0;
            return;
        }

        UpdateSelectionStatus();

        if(!isParentGameObject)
        {
            int size = selectedCount;
            objs = new Transform[size];
            for(int i=0; i<objs.Length; i++)
            {
                objs[i] = Selection.gameObjects[i].transform;
                Undo.RecordObject(objs[i],"PlaceObjectsTool");
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
                Undo.RecordObject(objs[i],"PlaceObjectsTool");
            }
        }

        //Prevent no object selected
        if (objs.Length <= 0)
        {
            selectedCount = 0;
            childCount = 0;
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
        if(objs == null) return;

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

        //Prevent error
        if(objs == null) return;
        if( amount.x <= 0 || amount.y <= 0 || amount.z <= 0 ) return;

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
                if (tg_ran_scaUniform)
                {
                    float ran = Random.Range(ran_sca_min.x, ran_sca_max.x);
                    objs[i].localScale = new Vector3(ran, ran, ran);
                }
                else
                {
                    objs[i].localScale = RandomV3(ran_sca_min, ran_sca_max);
                }
            }
        }
        
        //Random in sphere
        if (tg_ranSphere)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i].localPosition = UnityEngine.Random.insideUnitSphere * tg_ranSphere_radius;
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

    void MoveObjectToGround()
    {
        SetSelectionObjects();

        //Prevent error
        if(objs == null) return;

        for (int i = 0; i < objs.Length; i++)
        {
            Vector3 pos = objs[i].transform.position;
            Vector3 sca = objs[i].transform.localScale;

            pos.y = sca.y * 0.5f;

            Undo.RecordObject(objs[i].transform, "MoveObjectToGround");
            objs[i].transform.position = pos;
        }
    }

    void AlignObjects()
    {
        SetSelectionObjects();

        //Prevent error
        if(objs == null) return;

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

    void CopyPasteTransform(int type)
    {
        SetSelectionObjects();

        //Prevent error
        if(objs == null) return;

        if(copyFromTrans != null)
        {
            //Paste transform
            for (int i = 0; i < objs.Length; i++)
            {
                if(type == 0 || type == 1) objs[i].localPosition = copyFromTrans.localPosition;
                if(type == 0 || type == 2) objs[i].localRotation = copyFromTrans.localRotation;
                if(type == 0 || type == 3) objs[i].localScale = copyFromTrans.localScale;
            }
        }
    }

    void ReplaceObjects()
    {
        SetSelectionObjects();

        //Prevent error
        if(objs == null) return;

        if(replaceSrc != null)
        {
            //Generate new
            for (int i = 0; i < objs.Length; i++)
            {
                Transform t = objs[i].transform;
                GameObject newObject = PrefabUtility.InstantiatePrefab(replaceSrc) as GameObject;
                if(newObject == null)
                {
                    newObject = Instantiate(replaceSrc) as GameObject;
                }
                Transform newT = newObject.transform;
                newT.parent = t.parent;
                newT.position = t.position;
                newT.rotation = t.rotation;
                newT.localScale = t.localScale;
            }
            //Remove old
            for (int i = 0; i < objs.Length; i++)
            {
                DestroyImmediate(objs[i].gameObject);
            }
        }
    }

    // void OnInspectorUpdate() 
	// {
	// 	this.Repaint();
	// }

}