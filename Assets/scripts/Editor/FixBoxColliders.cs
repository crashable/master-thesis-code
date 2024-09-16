using UnityEditor;
using UnityEngine;

    public class FixBoxColliders : EditorWindow
    {
        [MenuItem("Tools/Fix Box Colliders")]
        private static void FixColliders()
        {
            foreach (BoxCollider collider in FindObjectsOfType<BoxCollider>())
            {
                if (collider.size.x < 0 || collider.size.y < 0 || collider.size.z < 0)
                {
                    collider.size = new Vector3(
                        Mathf.Abs(collider.size.x),
                        Mathf.Abs(collider.size.y),
                        Mathf.Abs(collider.size.z)
                    );
                    Debug.Log("Fixed BoxCollider on GameObject: " + collider.gameObject.name);
                }

                // Check and fix negative scale on the GameObject as well
                Transform transform = collider.transform;
                Vector3 localScale = transform.localScale;
                if (localScale.x < 0 || localScale.y < 0 || localScale.z < 0)
                {
                    transform.localScale = new Vector3(
                        Mathf.Abs(localScale.x),
                        Mathf.Abs(localScale.y),
                        Mathf.Abs(localScale.z)
                    );
                    Debug.Log("Fixed negative scale on GameObject: " + collider.gameObject.name);
                }
            }
        }
    }