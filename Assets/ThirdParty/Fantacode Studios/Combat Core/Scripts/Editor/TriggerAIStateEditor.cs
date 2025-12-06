#if UNITY_EDITOR

using UnityEditor;

namespace FS_CombatCore
{

    [CustomEditor(typeof(TriggerAIState))]
    public class TriggerAIStateEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Cache properties
            SerializedProperty triggerType = serializedObject.FindProperty("triggerType");
            SerializedProperty enemies = serializedObject.FindProperty("enemies");
            SerializedProperty stateToTrigger = serializedObject.FindProperty("stateToTrigger");
            SerializedProperty requiredStates = serializedObject.FindProperty("requiredStatesForTriggering");
            SerializedProperty setTarget = serializedObject.FindProperty("setTarget");
            SerializedProperty target = serializedObject.FindProperty("target");

            // Draw default fields
            EditorGUILayout.PropertyField(triggerType);
            EditorGUILayout.PropertyField(enemies);
            EditorGUILayout.PropertyField(stateToTrigger);
            EditorGUILayout.PropertyField(requiredStates);

            EditorGUILayout.PropertyField(setTarget);

            // Conditionally show 'target' field
            if ((TriggerType)triggerType.enumValueIndex == TriggerType.OnStart && setTarget.boolValue)
            {
                EditorGUILayout.PropertyField(target);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
