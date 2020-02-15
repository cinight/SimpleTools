//By Ming Wai Chan
//
//What is this:
//It lists out all Animators, AnimationStates, and AnimationClips in the scene, or in a specific gameObject
//In play mode, you can trigger these animations to play and no need to write scripts to trigger them manually.
//
//How to use:
//1. Put this script in an Editor folder
//2. Top Menu->Window->AnimationPreviewTool
//3. Go into play mode
//4. Click update list on the tool
//5. Click on checkbox to trigger to play an animation
//
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationPreviewTool : EditorWindow
{
	#if UNITY_EDITOR

	Vector2 scrollPosition;
	public GameObject pa;
	private myAni[] allanimators;
	public bool allobject = true;
	public bool include_disabled = false;

	[MenuItem("Window/AnimationPreviewTool")]
	public static void ShowWindow ()
	{
		// Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow (typeof(AnimationPreviewTool));
	}

	void OnGUI () 
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition,GUILayout.Width(0),GUILayout.Height(0));

		AnimationPreviewTool myTarget = this;

		// Show the basic public variables
		GUILayout.Label ("This tool shows all animator and animations in Scene, hit Play and click on check box to preview them : )", EditorStyles.wordWrappedMiniLabel);
		EditorGUILayout.Space ();
		allobject = EditorGUILayout.ToggleLeft("All GameObjects on scene?",allobject);
		if (!allobject)
		{
			GUILayout.Label ("Assign the GameObject that you want to check", EditorStyles.boldLabel);
			pa = (GameObject)EditorGUILayout.ObjectField (pa, typeof(GameObject), true);
		}
		include_disabled = EditorGUILayout.ToggleLeft("Include disabled objects?",include_disabled);


		if (GUILayout.Button ("Update Animation List", EditorStyles.miniButton))
		{
			myTarget.initializelist ();
		}

		EditorGUILayout.Space();
		EditorGUILayout.Separator ();

		// Show the Animators and Animation Lists
		if (myTarget.allanimators != null && myTarget.allanimators.Length>0) 
		{

			for (int i = 0; i < myTarget.allanimators.Length; i++) 
			{
				if (myTarget.allanimators [i].ac != null) 
				{
					string objstatus="";
					if (!myTarget.allanimators [i].animator.gameObject.activeSelf)
					{
						objstatus = " (disabled)";
					}
					if (GUILayout.Button (myTarget.allanimators [i].animator.name+objstatus, EditorStyles.miniButton))
					{
						EditorApplication.ExecuteMenuItem ("Window/General/Hierarchy");
						Selection.activeGameObject = myTarget.allanimators [i].animator.gameObject;
					}

					float Twidth = EditorGUIUtility.currentViewWidth;

					//Horizontal Group
					GUIStyle myStyle_g = new GUIStyle();
					myStyle_g.onHover.textColor = GUI.color-Color.cyan;
					myStyle_g.hover.textColor = GUI.color-Color.cyan;
					//Horizontal Group

					GUILayoutOption myStyle1 = GUILayout.Width(15);

					GUIStyle myStyle2 = new GUIStyle(EditorStyles.label);
					myStyle2.fontStyle = FontStyle.Bold;
					myStyle2.fixedWidth = (Twidth-1)*0.4f;

					GUIStyle myStyle3 = new GUIStyle(EditorStyles.label);
					myStyle3.fontStyle = FontStyle.Bold;
					myStyle3.alignment = TextAnchor.MiddleRight;

					//GUILayoutOption options0 = GUILayout.Width(15);

					GUILayout.BeginHorizontal(myStyle_g);
					GUILayout.Label("",myStyle1);
					GUILayout.Label("State",myStyle2);
					GUILayout.Label("Clip",myStyle2);
					GUILayout.Label("Sec",myStyle3);
					GUILayout.EndHorizontal();

					if (myTarget.allanimators [i].mylistbool != null) 
					{
						for (int j = 0; j < myTarget.allanimators [i].mylistbool.Length; j++) 
						{
							string title = myTarget.allanimators [i].mylist [j];
							float seconds = myTarget.allanimators [i].mylistduration [j];
							int dur_sec = Mathf.FloorToInt (seconds);
							float dur_msec = seconds-dur_sec;
							dur_msec = Mathf.Round(dur_msec*10f);

							GUILayout.BeginHorizontal(myStyle_g);

							myStyle2.fontStyle = FontStyle.Normal;
							myStyle3.fontStyle = FontStyle.Normal;

							myTarget.allanimators [i].mylistbool [j] = EditorGUILayout.ToggleLeft("", myTarget.allanimators [i].mylistbool [j],myStyle1);

							//Bold the current playing statename
							if (myTarget.allanimators [i].isCurrentPlayingState (title))
							{
								myStyle2.fontStyle = FontStyle.BoldAndItalic;

								if (myTarget.allanimators [i].myclips [j] != null)
								title = title + " Frame:" + ShowCurrentFrame(myTarget.allanimators [i].animator, myTarget.allanimators [i].myclips [j]);
							}

							GUILayout.Label (title, myStyle2);

							string clipname = "";
							if (myTarget.allanimators [i].myclips [j] != null)
							{
								clipname = myTarget.allanimators [i].myclips [j].name;
							}
							GUILayout.Label(clipname,myStyle2);
							GUILayout.Label(dur_sec+"."+dur_msec,myStyle3);

							GUILayout.EndHorizontal();
						}
						myTarget.allanimators [i].playtheList (0);
						EditorGUILayout.Space ();
					}
				}
			}Repaint ();
		}
	
		//***************
		GUILayout.Space (70);
		GUILayout.EndScrollView();
		//***************
	
	}

	private void initializelist()
	{
		Animator[] templist;

		if (allobject)
		{
			if (include_disabled)
			{
				templist = GetAllObjectsInScene ().ToArray ();
			}
			else
			{
				templist = UnityEngine.Object.FindObjectsOfType<Animator> ();
			}
		}
		else
		{
			templist = pa.GetComponentsInChildren<Animator> (include_disabled);
		}

		//Assign List
		allanimators = new myAni[templist.Length];
		int i = 0;
		foreach (Animator ani in templist) 
		{
			allanimators [i] = new myAni (ani);
			i++;
		}
	}

	List<Animator> GetAllObjectsInScene()
	{
		List<Animator> objectsInScene = new List<Animator>();

		foreach (Animator go in Resources.FindObjectsOfTypeAll(typeof(Animator)) as Animator[])
		{
			if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
				continue;

			if (!EditorUtility.IsPersistent(go.transform.root.gameObject))
				continue;

			objectsInScene.Add(go);
		}

		return objectsInScene;
	}
		
	private string ShowCurrentFrame(Animator anicon, AnimationClip aniclip)
	{
		float length = aniclip.length;
		float fps = aniclip.frameRate;
		float progress = anicon.GetCurrentAnimatorStateInfo (0).normalizedTime;

		progress = progress - Mathf.Floor (progress);

		float totalframe = length * fps;
		float currentframe = totalframe * progress;


		///new
		return currentframe.ToString("0.00") + "/" + totalframe;
	}
		

	#endif
}
//=================================================================================================================
public class myAni
{
	#if UNITY_EDITOR
	public Animator animator;
	public string[] mylist;
	public float[] mylistduration;
	public bool[] mylistbool;
	public AnimationClip[] myclips;
	public UnityEditor.Animations.AnimatorController ac;
	public int lengthOfList = 0;

	// Constructor, assign all properties
	public myAni(Animator ani)
	{
		animator = ani;
		Debug.Log (animator.name + " initializing ");
		if (animator.runtimeAnimatorController == null)
		{
			ac = null;
			return;
		}
		ac = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

		Debug.Log("No. of Layers in **"+ac.name+"** is "+ac.layers.Length);
		for (int h = 0; h < ac.layers.Length; h++) 
		{
			lengthOfList += ac.layers [h].stateMachine.states.Length;
		}

		if (ani != null && ac != null) 
		{
			mylist = new string[lengthOfList];
			mylistduration = new float[lengthOfList];
			myclips = new AnimationClip[lengthOfList];
			int i = 0;

			for (int h = 0; h < ac.layers.Length; h++)
			{
				foreach (UnityEditor.Animations.ChildAnimatorState st in ac.layers[h].stateMachine.states)
				{
					mylist [i] = st.state.name;

					//duration
					for (int k = 0; k < ac.layers[h].stateMachine.states.Length; k++)
					{
						UnityEditor.Animations.AnimatorState state = ac.layers [h].stateMachine.states [k].state;
						if (state.name == mylist [i])
						{
							AnimationClip clip = state.motion as AnimationClip;
							if (clip != null)
							{
								mylistduration [i] = clip.length;
								myclips [i] = clip;
							}
						}
					}
					//
					i++;
				}
			}
			mylistbool = new bool[lengthOfList];
			for (int j = 0; j < lengthOfList; j++) 
			{
				mylistbool [j] = false;
			}
		}
	}

	// Play an animation
	public void playtheList(float myAniSample)
	{
		for(int i=0; i<lengthOfList;i++)
		{
			if (mylistbool[i]) 
			{
				if (EditorApplication.isPlaying)
				{
					animator.Play (mylist [i], 0, 0);
				}
				else
				{
					//myclips [i].SampleAnimation (animator.gameObject, myAniSample);
				}
				
				Debug.Log ("play animation : " + mylist [i]);
				mylistbool [i] = false;
			}
		}
	}

	// Return the current playing animation of this animator
	public bool isCurrentPlayingState(string statename)
	{
		return animator.GetCurrentAnimatorStateInfo (0).IsName (statename);
	}

	#endif
}