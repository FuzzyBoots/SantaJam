#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FS_Core
{

    /// <summary>
    /// Makes a field read-only in the Unity Inspector.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }


    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif