using FS_Core;
using FS_Util;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class EquippableItemEditor : ItemDataEditor
    {
        public enum Animations { Equip, UnEquip }
        private EquippableItem equippableItem;

        private SerializedProperty name;
        private SerializedProperty description;
        private SerializedProperty icon;
        private SerializedProperty spawnModel;

        public SerializedProperty equipmentSlotIndex;
        public SerializedProperty attachOnBody;
        public SerializedProperty usePhysicsForAttachedItem;
        public SerializedProperty overridePhysicsPrefab;
        public SerializedProperty physicsPrefab;
        public SerializedProperty attachedItemPrefab;
        public SerializedProperty isSkinnedMesh;
        public SerializedProperty hasHolder;
        public SerializedProperty holderPrefab;
        public SerializedProperty attachBone;

        public SerializedProperty defaultAttachReference;
        public SerializedProperty attachReferences;

        public SerializedProperty holderLocalPosition;
        public SerializedProperty holderLocalRotation;
        public SerializedProperty holderAttachReferences;
        public SerializedProperty itemLocalPosition;
        public SerializedProperty itemLocalRotation;
        public SerializedProperty itemAttachReferences;

        private SerializedProperty modelPrefab;
        private SerializedProperty holderBone;
        private SerializedProperty localPositionR;
        private SerializedProperty localRotationR;
        private SerializedProperty localPositionL;
        private SerializedProperty localRotationL;
        private SerializedProperty itemEquipReferences;
        private SerializedProperty unEquipDuringActions;
        private SerializedProperty isDualItem;
        private SerializedProperty overrideController;
        private SerializedProperty itemEquippedIdleClip;
        private SerializedProperty itemEquippedIdleClipMask;
        private SerializedProperty itemEnableTime;
        private SerializedProperty equipAudio;
        private SerializedProperty equipClip;
        private SerializedProperty itemDisableTime;
        private SerializedProperty unEquipAudio;
        private SerializedProperty unEquipClip;
        static bool equipFoldout;
        static bool unEquipFoldout;
        static bool equipmentConfigurationSettings;

        EquipmentSlotsDatabase equipmentSlots;

        public virtual bool ShowDualItemProperty => false;

        public override void OnEnable()
        {
            base.OnEnable();

            equipmentSlots = Resources.LoadAll<EquipmentSlotsDatabase>("").FirstOrDefault();
            equippableItem = (EquippableItem)target;
            targetData = equippableItem;

            name = serializedObject.FindProperty("name");
            description = serializedObject.FindProperty("description");
            icon = serializedObject.FindProperty("icon");
            spawnModel = serializedObject.FindProperty("spawnModel");

            equipmentSlotIndex = serializedObject.FindProperty("equipmentSlotIndex");
            attachOnBody = serializedObject.FindProperty("attachOnBody");
            usePhysicsForAttachedItem = serializedObject.FindProperty("usePhysicsForAttachedItem");
            overridePhysicsPrefab = serializedObject.FindProperty("overridePhysicsPrefab");
            physicsPrefab = serializedObject.FindProperty("physicsPrefab");
            attachedItemPrefab = serializedObject.FindProperty("attachedItemPrefab");
            isSkinnedMesh = serializedObject.FindProperty("isSkinnedMesh");
            hasHolder = serializedObject.FindProperty("hasHolder");
            holderPrefab = serializedObject.FindProperty("holderPrefab");
            attachBone = serializedObject.FindProperty("attachBone");
            holderLocalPosition = serializedObject.FindProperty("holderLocalPosition");
            holderLocalRotation = serializedObject.FindProperty("holderLocalRotation");
            holderAttachReferences = serializedObject.FindProperty("holderAttachReferences");
            itemLocalPosition = serializedObject.FindProperty("itemLocalPosition");
            itemLocalRotation = serializedObject.FindProperty("itemLocalRotation");
            itemAttachReferences = serializedObject.FindProperty("itemAttachReferences");


            modelPrefab = serializedObject.FindProperty("modelPrefab");
            holderBone = serializedObject.FindProperty("holderBone");
            localPositionR = serializedObject.FindProperty("localPositionR");
            localRotationR = serializedObject.FindProperty("localRotationR");
            localPositionL = serializedObject.FindProperty("localPositionL");
            localRotationL = serializedObject.FindProperty("localRotationL");
            itemEquipReferences = serializedObject.FindProperty("itemEquipReferences");
            unEquipDuringActions = serializedObject.FindProperty("unEquipDuringActions");
            isDualItem = serializedObject.FindProperty("isDualItem");
            overrideController = serializedObject.FindProperty("overrideController");
            itemEquippedIdleClip = serializedObject.FindProperty("itemEquippedIdleClip");
            itemEquippedIdleClipMask = serializedObject.FindProperty("itemEquippedIdleClipMask");
            itemEnableTime = serializedObject.FindProperty("itemEnableTime");
            equipAudio = serializedObject.FindProperty("equipAudio");
            equipClip = serializedObject.FindProperty("equipClip");
            itemDisableTime = serializedObject.FindProperty("itemDisableTime");
            unEquipAudio = serializedObject.FindProperty("unEquipAudio");
            unEquipClip = serializedObject.FindProperty("unEquipClip");

            OnStart(equippableItem.equipClip.clip);
        }

        private void DrawEquipmentSlots()
        {
            if (equippableItem.equipmentSlotIndex == -1)
                equippableItem.equipmentSlotIndex = equippableItem.category.equipmentSlotIndex;

            var slots = equipmentSlots.EquipmentSlotList;
            string[] slotNames = slots.ToArray();

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup("Equipment Slot", equippableItem.equipmentSlotIndex, slotNames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(equippableItem, "Changed Equipment Slot");
                equippableItem.equipmentSlotIndex = newSelectedIndex;
                EditorUtility.SetDirty(equippableItem);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            // Equipment Configuration
            DrawFoldout(ref equipmentConfigurationSettings, "Equipment", () =>
            {
                if (equipmentSlots != null)
                {
                    DrawEquipmentSlots();
                }

                EditorGUILayout.PropertyField(holderBone);
                EditorGUILayout.PropertyField(unEquipDuringActions, new GUIContent("Unequip During Actions", "If true, item will be unequipped when performing other actions"));

                //if (ShowDualItemProperty)
                //{
                //    EditorGUILayout.PropertyField(isDualItem);
                //}
                EditorGUILayout.PropertyField(spawnModel, new GUIContent("Spawn On Equip"));

                if (spawnModel.boolValue)
                {
                    if (holderBone.enumValueIndex == (int)HumanBodyBones.RightHand)
                    {
                        DrawTransformSection("Default", localPositionR, localRotationR);
                    }
                    else if (holderBone.enumValueIndex == (int)HumanBodyBones.LeftHand)
                    {
                        DrawTransformSection("Default", localPositionL, localRotationL);
                    }
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        //EditorGUILayout.LabelField("Per Character Reference", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(itemEquipReferences, new GUIContent("Per Character References"));
                    }
                }

                GUILayout.Space(5);

                // Animation Configuration
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(overrideController);
                    EditorGUILayout.PropertyField(itemEquippedIdleClip);
                    EditorGUILayout.PropertyField(itemEquippedIdleClipMask);

                    // Equip Settings
                    EditorGUI.indentLevel++;
                    equipFoldout = EditorGUILayout.Foldout(equipFoldout, "Equip Settings", true);
                    if (equipFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(equipClip);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            ChangeAnimationClip(Animations.Equip);
                        }
                        if (equippableItem.equipClip.clip != null)
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(itemEnableTime);
                            if (EditorGUI.EndChangeCheck())
                            {
                                previewTime = itemEnableTime.floatValue * clip.length;
                                UpdatePreview();
                            }
                        }
                        EditorGUILayout.PropertyField(equipAudio);
                        EditorGUI.indentLevel--;
                    }

                    // Unequip Settings
                    unEquipFoldout = EditorGUILayout.Foldout(unEquipFoldout, "Unequip Settings", true);
                    if (unEquipFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(unEquipClip);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            ChangeAnimationClip(Animations.UnEquip);
                        }
                        if (equippableItem.unEquipClip.clip != null)
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(itemDisableTime);
                            if (EditorGUI.EndChangeCheck())
                            {
                                previewTime = itemDisableTime.floatValue * clip.length;
                                UpdatePreview();
                            }
                        }
                        EditorGUILayout.PropertyField(unEquipAudio);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;

            });

            serializedObject.ApplyModifiedProperties();
        }
    }
}