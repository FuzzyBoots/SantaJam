using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace FS_CombatCore
{
    [CustomPropertyDrawer(typeof(Reaction), true)]
    public class ReactionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            bool onlyShowClip = IsFaintReactionData(property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float padding = 4;
            float yOffset = position.y + padding;

            var animationClip = property.FindPropertyRelative("animationClipInfo");

            Rect DrawNextRect() => new Rect(position.x, yOffset, position.width, lineHeight);

            EditorGUI.PropertyField(DrawNextRect(), animationClip);
            yOffset += EditorGUI.GetPropertyHeight(animationClip, true) + padding;

            var rotateToAttacker = property.FindPropertyRelative("rotateToAttacker");
            EditorGUI.PropertyField(DrawNextRect(), rotateToAttacker);
            yOffset += lineHeight + padding;

            if (rotateToAttacker.boolValue)
            {
                var rotationOffset = property.FindPropertyRelative("rotationOffset");
                EditorGUI.PropertyField(DrawNextRect(), rotationOffset, new GUIContent("Rotation Offset (angles)"));
                yOffset += lineHeight + padding;
            }

            if (!onlyShowClip)
            {
                var willBeKnockedDown = property.FindPropertyRelative("willBeKnockedDown");
                var knockDownDirection = property.FindPropertyRelative("knockDownDirection");
                var lyingDownTimeRange = property.FindPropertyRelative("lyingDownTimeRange");
                var overrideLyingDownAnimation = property.FindPropertyRelative("overrideLyingDownAnimation");
                var lyingDownAnimation = property.FindPropertyRelative("lyingDownAnimation");
                var getUpAnimation = property.FindPropertyRelative("getUpAnimation");
                var animationMask = property.FindPropertyRelative("animationMask");
                var isAdditiveAnimation = property.FindPropertyRelative("isAdditiveAnimation");

                EditorGUI.PropertyField(DrawNextRect(), willBeKnockedDown);
                yOffset += lineHeight + padding;

                if (willBeKnockedDown.boolValue)
                {
                    EditorGUI.PropertyField(DrawNextRect(), knockDownDirection);
                    yOffset += lineHeight + padding;

                    EditorGUI.PropertyField(DrawNextRect(), lyingDownTimeRange);
                    yOffset += lineHeight + padding;

                    EditorGUI.PropertyField(DrawNextRect(), overrideLyingDownAnimation);
                    yOffset += lineHeight + padding;

                    if (overrideLyingDownAnimation.boolValue)
                    {
                        EditorGUI.PropertyField(DrawNextRect(), lyingDownAnimation);
                        yOffset += lineHeight + padding;

                        EditorGUI.PropertyField(DrawNextRect(), getUpAnimation);
                        yOffset += lineHeight + padding;
                    }
                }

                EditorGUI.PropertyField(DrawNextRect(), animationMask);
                yOffset += lineHeight + padding;
                EditorGUI.PropertyField(DrawNextRect(), isAdditiveAnimation);
                yOffset += lineHeight + padding;
            }
            else
            {
                var triggerRagdoll = property.FindPropertyRelative("triggerRagdoll");
                var ragdollTriggerTime = property.FindPropertyRelative("ragdollTriggerTime");

                EditorGUI.PropertyField(DrawNextRect(), triggerRagdoll, new GUIContent("Trigger Ragdoll"));
                yOffset += lineHeight + padding;

                if (triggerRagdoll != null && triggerRagdoll.boolValue)
                {
                    EditorGUI.Slider(DrawNextRect(), ragdollTriggerTime, 0f, 1f, new GUIContent("Ragdoll Trigger Time"));
                    yOffset += lineHeight + padding;
                }
            }
            var preventFallingFromLedge = property.FindPropertyRelative("preventFallingFromLedge");
            EditorGUI.PropertyField(DrawNextRect(), preventFallingFromLedge);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool onlyShowClip = IsFaintReactionData(property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float padding = 5;
            float height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationClipInfo"), true) + padding;

            SerializedProperty rotateToAttacker = property.FindPropertyRelative("rotateToAttacker");
            height += (lineHeight + padding);
            if (rotateToAttacker.boolValue)
                height += (lineHeight + padding);

            if (onlyShowClip)
            {
                SerializedProperty triggerRagdoll = property.FindPropertyRelative("triggerRagdoll");
                height += lineHeight + padding;
                if (triggerRagdoll != null && triggerRagdoll.boolValue)
                {
                    height += lineHeight + padding;
                }
                return height;
            }

            SerializedProperty willBeKnockedDown = property.FindPropertyRelative("willBeKnockedDown");
            SerializedProperty overrideLyingAnim = property.FindPropertyRelative("overrideLyingDownAnimation");

            height += (lineHeight + padding) * 4;

            if (willBeKnockedDown.boolValue)
            {
                height += lineHeight * 3 + padding * 3;
                if (overrideLyingAnim.boolValue)
                {
                    height += lineHeight * 2 + padding * 2;
                }
            }

            return height;
        }

        // Helper to check if the root object is a FaintReactionData
        private bool IsFaintReactionData(SerializedProperty property)
        {
            var root = property.serializedObject.targetObject;
            return root != null && root.GetType().Name == nameof(DeathReactionsData);
        }
    }
}
