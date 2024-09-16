using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FindMissingScripts : EditorWindow
    {
        [MenuItem("Tools/Find Missing Scripts in Scene")]
        public static void FindInScene()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            List<GameObject> offenders = new List<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        Debug.LogWarning("Missing script found on: " + obj.name + ", ID: " + obj.GetInstanceID());
                        offenders.Add(obj);
                    }
                }
            }
        
            if (offenders.Count > 0)
            {
                Debug.LogWarning(offenders.Count + " GameObjects with missing scripts found.");
            }
            else
            {
                Debug.Log("No missing scripts found in the scene.");
            }
        }

        // To search through all prefabs in the project assets (Optional)
        [MenuItem("Tools/Find Missing Scripts in Assets")]
        public static void FindInAssets()
        {
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            List<GameObject> offenders = new List<GameObject>();
            foreach (string assetPath in allAssets)
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (obj != null)
                {
                    Component[] components = obj.GetComponentsInChildren<Component>(true);
                    foreach (Component component in components)
                    {
                        if (component == null)
                        {
                            Debug.LogWarning("Missing script found in asset: " + assetPath);
                            offenders.Add(obj);
                            break; // Only need to report once per asset
                        }
                    }
                }
            }

            if (offenders.Count > 0)
            {
                Debug.LogWarning(offenders.Count + " assets with missing scripts found.");
            }
            else
            {
                Debug.Log("No missing scripts found in assets.");
            }
        }
    }

