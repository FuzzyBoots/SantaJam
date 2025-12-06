using UnityEngine;
using UnityEditor;
using FS_ThirdPerson;

namespace FS_CombatSystem
{
    [CustomEditor(typeof(MeleeWeapon))]
    public class MeleeWeaponEditor : EquippableItemEditor
    {
        private MeleeWeapon weaponData;

        private SerializedProperty attacks;
        private SerializedProperty heavyAttacks;
        private SerializedProperty specialAttacks;
        private SerializedProperty reactionData;
        private SerializedProperty canBlock;
        private SerializedProperty blocking;
        private SerializedProperty blockedDamage;
        private SerializedProperty blockReactionData;
        private SerializedProperty canCounter;
        private SerializedProperty playActionIfCounterMisused;
        private SerializedProperty counterMisusedAction;
        private SerializedProperty overrideMoveSpeed;
        private SerializedProperty combatMoveSpeed;
        private SerializedProperty useRootmotion;
        private SerializedProperty overrideDodge;
        private SerializedProperty dodgeData;
        private SerializedProperty overrideRoll;
        private SerializedProperty rollData;
        private SerializedProperty minAttackDistance;

        private bool showAttacks, showBlock, showCounter, showMovement, showDodge, showRoll;

        public override void OnEnable()
        {
            weaponData = (MeleeWeapon)target;

            attacks = serializedObject.FindProperty("attacks");
            heavyAttacks = serializedObject.FindProperty("heavyAttacks");
            specialAttacks = serializedObject.FindProperty("specialAttacks");
            reactionData = serializedObject.FindProperty("reactionData");

            canBlock = serializedObject.FindProperty("canBlock");
            blocking = serializedObject.FindProperty("blocking");
            blockedDamage = serializedObject.FindProperty("blockedDamage");
            blockReactionData = serializedObject.FindProperty("blockReactionData");

            canCounter = serializedObject.FindProperty("canCounter");
            playActionIfCounterMisused = serializedObject.FindProperty("playActionIfCounterMisused");
            counterMisusedAction = serializedObject.FindProperty("counterMisusedAction");

            overrideMoveSpeed = serializedObject.FindProperty("overrideMoveSpeed");
            combatMoveSpeed = serializedObject.FindProperty("combatMoveSpeed");
            useRootmotion = serializedObject.FindProperty("useRootmotion");

            overrideDodge = serializedObject.FindProperty("overrideDodge");
            dodgeData = serializedObject.FindProperty("dodgeData");
            overrideRoll = serializedObject.FindProperty("overrideRoll");
            rollData = serializedObject.FindProperty("rollData");

            minAttackDistance = serializedObject.FindProperty("minAttackDistance");

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            DrawFoldout(ref showAttacks, "Attack Settings", () =>
            {
                EditorGUILayout.PropertyField(attacks, true);
                EditorGUILayout.PropertyField(heavyAttacks, true);
                EditorGUILayout.PropertyField(specialAttacks, true);
                EditorGUILayout.PropertyField(reactionData);
            });

            DrawFoldout(ref showBlock, "Blocking Settings", () =>
            {
                EditorGUILayout.PropertyField(canBlock);
                if (canBlock.boolValue)
                {
                    EditorGUILayout.PropertyField(blocking);
                    EditorGUILayout.PropertyField(blockedDamage);
                    EditorGUILayout.PropertyField(blockReactionData);
                }
            });

            DrawFoldout(ref showCounter, "Counter Settings", () =>
            {
                EditorGUILayout.PropertyField(canCounter);
                if (canCounter.boolValue)
                {
                    EditorGUILayout.PropertyField(playActionIfCounterMisused);
                    if (playActionIfCounterMisused.boolValue)
                    {
                        EditorGUILayout.PropertyField(counterMisusedAction);
                    }
                }
            });

            DrawFoldout(ref showMovement, "Movement Settings", () =>
            {
                EditorGUILayout.PropertyField(overrideMoveSpeed);
                if (overrideMoveSpeed.boolValue)
                {
                    EditorGUILayout.PropertyField(combatMoveSpeed);
                }
                EditorGUILayout.PropertyField(useRootmotion);
                EditorGUILayout.PropertyField(minAttackDistance);
            });

            DrawFoldout(ref showDodge, "Dodge Settings", () =>
            {
                EditorGUILayout.PropertyField(overrideDodge);
                if (overrideDodge.boolValue)
                {
                    EditorGUILayout.PropertyField(dodgeData);
                }
            });

            DrawFoldout(ref showRoll, "Roll Settings", () =>
            {
                EditorGUILayout.PropertyField(overrideRoll);
                if (overrideRoll.boolValue)
                {
                    EditorGUILayout.PropertyField(rollData);
                }
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
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical(); // End HelpBox

        }

        public override void ChangeAnimationClip(object type = null)
        {
            base.ChangeAnimationClip(type);
            if (type != null)
                currentClipType = (Animations)type;
            switch (currentClipType)
            {
                case Animations.Equip:
                    clip = weaponData.equipClip.clip;
                    break;
                case Animations.UnEquip:
                    clip = weaponData.unEquipClip.clip;
                    break;
                case Animations.Block:
                    clip = (AnimationClip)blocking.objectReferenceValue;
                    break;
                default:
                    break;
            }
        }
        public enum Animations { Equip, UnEquip, Block }
        static Animations currentClipType = Animations.Equip;
        public override void HandleAnimationEnumPopup()
        {
            base.HandleAnimationEnumPopup();
            EditorGUI.BeginChangeCheck();
            currentClipType = (Animations)EditorGUILayout.EnumPopup(currentClipType, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                ChangeAnimationClip(currentClipType);
                UpdatePreview();
            }
        }
    }
}