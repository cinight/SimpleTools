using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;
#endif
using UnityEngine;

// Use this tool to save a prefab to Asset folder together 
// with the images that is loaded on UI Image/RawImage components,
// which the images are downloaded that they only exist in memory.

// This is useful if you want to isolate some assets for debugging purposes,
// that you don't want to complicate the project with setting up the downloading of the images 
// just for reproduce an unrelated bug.

public class SaveObjectAndNullPathImage
{
    #if UNITY_EDITOR
        [MenuItem("Test/SaveAsset")]
        public static void SaveAsset()
        {
            var selected = Selection.activeObject as GameObject;
            
            var allImage = selected.GetComponentsInChildren<Image>();
            foreach (var img in allImage)
            {
                if (img.sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(img.sprite);
                    if (path == "")
                    {
                        AssetDatabase.CreateAsset(img.sprite, "Assets/_CMWTest/SaveAsset/" + img.sprite.name + ".asset");
                    }
                }
            }
            
            var allRawImage = selected.GetComponentsInChildren<RawImage>();
            foreach (var img in allRawImage)
            {
                if (img.texture != null)
                {
                    string path = AssetDatabase.GetAssetPath(img.texture);
                    if (path == "")
                    {
                        AssetDatabase.CreateAsset(img.texture, "Assets/_CMWTest/SaveAsset/" + img.texture.name + ".asset");
                    }
                }
            }
            
            Debug.Log(selected.name);
            bool prefabSuccess;
            string localPath = "Assets/_CMWTest/SaveAsset/" + selected.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(selected, localPath, out prefabSuccess);
            Debug.Log(localPath+ " = " +prefabSuccess);
        }  
    #endif

}



