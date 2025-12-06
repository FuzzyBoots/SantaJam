#if UNITY_EDITOR
using FS_CombatCore;
using FS_ThirdPerson;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FS_Core
{
    public partial class FSSystemsSetup
    {
        public static FSSystemInfo MeleeCombatSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            selected: false,
            systemName: "Melee Combat System",
            displayName: "Melee Combat",
            prefabName: "Combat Controller",
            welcomeEditorShowKey: "MeleeCombatSystem_WelcomeWindow_Opened_1",

            systemProjectSettings: new SystemProjectSettingsData
            (
                layers: new List<string> { "Enemy", "VisionSensor", "Cover", "PlayerAlly" },
                tags: new List<string>() { "Hitbox" }
            ),
            mobileControllerPrefabName: "Combat Mobile Controller",

            extraSetupActionPlayer: (GameObject playerObject, GameObject playerParentObjectPrefab,GameObject createdPlayerParentObject) => 
            { 
                SetColliders(playerObject.GetComponent<Animator>());
                SetupVisionSensorMelee(playerObject, playerParentObjectPrefab);
                SetCombatSettings(playerParentObjectPrefab, createdPlayerParentObject);
            }
        );

        public static FSSystemInfo MeleeCombatSystemAISetup = new FSSystemInfo
        (
            characterType: CharacterType.AI,
            selected: false,
            systemName: "Melee Combat System",
            displayName: "Melee Combat AI",
            prefabName: "Combat AI Controller",
            welcomeEditorShowKey: "MeleeCombatSystem_WelcomeWindow_Opened_1",

            extraSetupActionAI: (GameObject aiObject, GameObject prefabObject) =>
            {
                SetupVisionSensorMelee(aiObject, prefabObject);
                SetColliders(aiObject.GetComponent<Animator>());
                SetMeleeTargetLayer(aiObject, "Player");
            }
        );

        static string MeleeCombatSystemWelcomeEditorKey => MeleeCombatSystemSetup.welcomeEditorShowKey;


        [InitializeOnLoadMethod]
        public static void LoadMeleeCombatSystem()
        {
            if (!string.IsNullOrEmpty(MeleeCombatSystemWelcomeEditorKey) && !EditorPrefs.GetBool(MeleeCombatSystemWelcomeEditorKey, false))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                EditorPrefs.SetBool(MeleeCombatSystemWelcomeEditorKey, true);
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }

        static void SetCombatSettings(GameObject playerParentObjectPrefab, GameObject createdPlayerParentObject)
        {
            FSSystemsSetupEditorWindow.CopyComponents(playerParentObjectPrefab, createdPlayerParentObject);

            SetMeleeTargetLayer(createdPlayerParentObject.GetComponentInChildren<PlayerController>().gameObject, "Enemy");
        }

        static void SetColliders(Animator modelAnimator)
        {
            if (!modelAnimator.isHuman) return;

            AddColliderToBone(modelAnimator, HumanBodyBones.RightHand);
            AddColliderToBone(modelAnimator, HumanBodyBones.LeftHand);
            AddColliderToBone(modelAnimator, HumanBodyBones.RightFoot);
            AddColliderToBone(modelAnimator, HumanBodyBones.LeftFoot);

            AddColliderToBone(modelAnimator, HumanBodyBones.LeftLowerArm, "LeftElbowCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.RightLowerArm, "RightElbowCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.LeftLowerLeg, "LeftKneeCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.RightLowerLeg, "RightKneeCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.Head, "HeadCollider");
        }

        static void AddColliderToBone(Animator animator, HumanBodyBones bone, string objName = "")
        {
            var handler = animator.GetBoneTransform(bone);
            var colliderName = string.IsNullOrEmpty(objName) ? bone.ToString() + "Collider" : objName;
            var weaponHandler = (GameObject)Resources.Load("CombatCollider");
            var obj = PrefabUtility.InstantiatePrefab(weaponHandler, handler) as GameObject;
            obj.name = colliderName;
            obj.transform.localPosition = Vector3.zero;

        }

        static void SetupVisionSensorMelee(GameObject aiObject, GameObject prefabObject)
        {
            var visionSensor = aiObject.GetComponentInChildren<VisionSensor>();

            if (visionSensor != null) return;

            var targetObject = new GameObject("Vision Sensor");
            targetObject.transform.SetParent(aiObject.transform);

            FSSystemsSetupEditorWindow.CopyComponents(prefabObject.GetComponentInChildren<VisionSensor>().gameObject, targetObject);
            visionSensor = targetObject.GetComponent<VisionSensor>();

            if ((targetObject.gameObject.layer != LayerMask.NameToLayer("VisionSensor")))
                targetObject.gameObject.layer = LayerMask.NameToLayer("VisionSensor");
        }

        static void SetMeleeTargetLayer(GameObject characterObject, string layerName)
        {
            var fighterCore = characterObject.GetComponent<FighterCore>();
            fighterCore.targetLayer = 1 << LayerMask.NameToLayer(layerName);
        }
    }
}
#endif