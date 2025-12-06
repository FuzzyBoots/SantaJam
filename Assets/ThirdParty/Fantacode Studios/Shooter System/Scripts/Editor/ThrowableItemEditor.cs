using Codice.Client.Common;
using FS_CombatCore;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FS_ShooterSystem
{

    [CustomEditor(typeof(ThrowableItem))]
    public class ThrowableItemEditor : EquippableItemEditor
    {
        SerializedProperty ammo;
        SerializedProperty aimAnimation;
        SerializedProperty throwAnimation;
        SerializedProperty zoomWhileAim;
        SerializedProperty aimCameraSettings;
        SerializedProperty maxThrowDistance;
        SerializedProperty throwForce;
        SerializedProperty arcHeight;
        SerializedProperty distanceMultiplier;

        // Add these field declarations at the top of the class with other serialized properties
        SerializedProperty reactionData;
        SerializedProperty overrideDodge;
        SerializedProperty dodgeData; 
        SerializedProperty overrideRoll;
        SerializedProperty rollData;

        bool showThrowSettings = false;
        bool showAnimations = false;
        bool showAimSettings = false;
        // Add these bool fields for foldout states
        bool reactionSettings = false;
        bool showDodge = false;
        bool showRoll = false;

        public override void OnEnable()
        {
            ammo = serializedObject.FindProperty("ammo");

            aimAnimation = serializedObject.FindProperty("aimAnimation");
            throwAnimation = serializedObject.FindProperty("throwAnimation");

            zoomWhileAim = serializedObject.FindProperty("zoomWhileAim");
            aimCameraSettings = serializedObject.FindProperty("aimCameraSettings");

            maxThrowDistance = serializedObject.FindProperty("maxThrowDistance");
            throwForce = serializedObject.FindProperty("throwForce");
            arcHeight = serializedObject.FindProperty("arcHeight");
            distanceMultiplier = serializedObject.FindProperty("distanceMultiplier");

            // Add these property initializations to the existing OnEnable method
            reactionData = serializedObject.FindProperty("reactionData");
            overrideDodge = serializedObject.FindProperty("overrideDodge");
            dodgeData = serializedObject.FindProperty("dodgeData");
            overrideRoll = serializedObject.FindProperty("overrideRoll");
            rollData = serializedObject.FindProperty("rollData");

            // Keep existing base.OnEnable() call
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(ammo);
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            serializedObject.Update();

            DrawFoldout(ref showAnimations, "Animations", () => {
                EditorGUILayout.PropertyField(aimAnimation);
                EditorGUILayout.PropertyField(throwAnimation);
            });

            DrawFoldout(ref showThrowSettings, "Throw Settings", () =>
            {
                EditorGUILayout.PropertyField(maxThrowDistance);
                EditorGUILayout.PropertyField(throwForce);
                EditorGUILayout.PropertyField(arcHeight);
                EditorGUILayout.PropertyField(distanceMultiplier);
            });

            DrawFoldout(ref showAimSettings, "Aim Settings", () =>
            {
                EditorGUILayout.PropertyField(zoomWhileAim);
                if (zoomWhileAim.boolValue)
                    EditorGUILayout.PropertyField(aimCameraSettings);
            });
            // Reaction
            DrawFoldout(ref reactionSettings, "Reaction Settings", () =>
            {
                EditorGUILayout.PropertyField(reactionData);
            });
            // Dodge
            DrawFoldout(ref showDodge, "Dodge Settings", () =>
            {
                EditorGUILayout.PropertyField(overrideDodge);
                if (overrideDodge.boolValue)
                    EditorGUILayout.PropertyField(dodgeData);
            });

            // Roll
            DrawFoldout(ref showRoll, "Roll Settings", () =>
            {
                EditorGUILayout.PropertyField(overrideRoll);
                if (overrideRoll.boolValue)
                    EditorGUILayout.PropertyField(rollData);
            });


            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFoldout(ref bool toggle, string label, System.Action drawer)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox); // Start HelpBox
            EditorGUI.indentLevel++;
            toggle = EditorGUILayout.Foldout(toggle, label, true);
            if (toggle)
            {
                EditorGUI.indentLevel++;
                drawer();
                EditorGUI.indentLevel--;
                //EditorGUILayout.Space(5);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical(); // End HelpBox

        }
    }
}