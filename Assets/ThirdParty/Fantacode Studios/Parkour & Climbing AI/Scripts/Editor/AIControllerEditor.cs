using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FS_ParkourAI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AIController))]
    public class AIControllerEditor : Editor
    {
        public SerializedProperty followType;
        public SerializedProperty player;
        public SerializedProperty targetCharacter;
        public SerializedProperty followWayPoints;
        public SerializedProperty followThroughClimbPoints;
        public SerializedProperty dynamicPredictiveJump;
        public SerializedProperty enableSpaceChecking;
        public SerializedProperty useMultiDirectionalAnimation;
        public SerializedProperty animationRunSpeed;

        private void OnEnable()
        {
            followType = serializedObject.FindProperty("followType");
            player = serializedObject.FindProperty("player");
            targetCharacter = serializedObject.FindProperty("target");
            followWayPoints = serializedObject.FindProperty("followWayPoints");
            followThroughClimbPoints = serializedObject.FindProperty("followThroughClimbPoints");
            dynamicPredictiveJump = serializedObject.FindProperty("dynamicPredictiveJump");
            enableSpaceChecking = serializedObject.FindProperty("enableSpaceChecking");
            useMultiDirectionalAnimation = serializedObject.FindProperty("useMultiDirectionalAnimation");
            animationRunSpeed = serializedObject.FindProperty("animationRunSpeed");
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            var aiController = (AIController)target;

            serializedObject.Update();


            EditorGUILayout.PropertyField(followType);
            switch (aiController.followType)
            {
                case FollowType.FollowPlayer:
                    EditorGUILayout.PropertyField(player);
                    EditorGUILayout.PropertyField(followThroughClimbPoints);
                    EditorGUILayout.PropertyField(dynamicPredictiveJump);
                    break;
                case FollowType.FollowTarget:
                    EditorGUILayout.PropertyField(targetCharacter);
                    break;
                case FollowType.FollowWayPoints:
                    EditorGUILayout.PropertyField(followWayPoints);
                    break;
                case FollowType.RandomWanderer:
                    break;
                case FollowType.RunFromPlayer:
                    EditorGUILayout.PropertyField(player);
                    break;
                case FollowType.None:
                    break;
                default:
                    break;
            }
            EditorGUILayout.PropertyField(enableSpaceChecking);
            EditorGUILayout.PropertyField(useMultiDirectionalAnimation);
            EditorGUILayout.PropertyField(animationRunSpeed);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
