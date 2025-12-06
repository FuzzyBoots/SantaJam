#if UNITY_EDITOR
using FS_CombatCore;
using FS_ShooterSystem;
using FS_ThirdPerson;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FS_Core
{
    public partial class FSSystemsSetup
    {
        static FSSystemInfo ShooterSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            selected: false,
            systemName: "Shooter System",
            displayName: "Shooter",
            prefabName: "Shooter Controller",
            welcomeEditorShowKey: "ShooterSystem_WelcomeWindow_Opened",
            mobileControllerPrefabName: "Shooter Mobile Controller",
            systemProjectSettings: new SystemProjectSettingsData
            (
                layers: new List<string> { "Cover", "Enemy" },
                extraSetupAction: () =>
                {
                    SetCoverLayerToCoverPrefab();
                }
            ),

            extraSetupActionPlayer: (GameObject playerObject, GameObject playerParentObjectPrefab, GameObject createdPlayerParentObject) =>
            {
                SetShooterSettings(playerParentObjectPrefab, createdPlayerParentObject);
                SetShooterUI(createdPlayerParentObject);
                SetupVisionSensorShooter(playerObject, playerParentObjectPrefab);
            }
        );

        static FSSystemInfo ShooterSystemAISetup = new FSSystemInfo
        (
            characterType: CharacterType.AI,
            selected: false,
            systemName: "Shooter System",
            displayName: "Shooter AI",
            prefabName: "Shooter AI Controller",
            welcomeEditorShowKey: "ShooterSystem_WelcomeWindow_Opened",

            extraSetupActionAI: (GameObject aiObject, GameObject prefabObject) =>
            {
                SetupVisionSensorShooter(aiObject, prefabObject);
                SetShooterTargetLayer(aiObject, "Player");
            }
        );


        static string ShooterSystemWelcomeEditorKey => ShooterSystemSetup.welcomeEditorShowKey;


        [InitializeOnLoadMethod]
        public static void LoadShooterSystem()
        {
            if (!string.IsNullOrEmpty(ShooterSystemWelcomeEditorKey) && !EditorPrefs.GetBool(ShooterSystemWelcomeEditorKey, false))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                EditorPrefs.SetBool(ShooterSystemWelcomeEditorKey, true);
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }

        static void SetShooterSettings(GameObject playerParentObjectPrefab, GameObject createdPlayerParentObject)
        {
            FSSystemsSetupEditorWindow.CopyComponents(playerParentObjectPrefab, createdPlayerParentObject);
            SetShooterTargetLayer(createdPlayerParentObject.GetComponentInChildren<PlayerController>().gameObject, "Enemy");
        }

        static void SetShooterUI(GameObject createdPlayerParentObject)
        {
            var shooterUI = Resources.Load<GameObject>("Shooter UI Canvas");
            if (shooterUI != null)
            {
                var targetUI = Instantiate(shooterUI.gameObject, createdPlayerParentObject.transform);
                targetUI.name = "Shooter UI Canvas";
                var shooterCrosshairController = targetUI.GetComponentInChildren<ShooterCrosshairController>();
                shooterCrosshairController.shooterController = createdPlayerParentObject.GetComponentInChildren<ShooterController>();
            }
        }

        static void SetupVisionSensorShooter(GameObject characterObject, GameObject prefabObject)
        {
            var visionSensor = characterObject.GetComponentInChildren<VisionSensor>();

            if (visionSensor != null) return;

            var targetObject = new GameObject("Vision Sensor");
            targetObject.transform.SetParent(characterObject.transform);

            FSSystemsSetupEditorWindow.CopyComponents(prefabObject.GetComponentInChildren<VisionSensor>().gameObject, targetObject);
            visionSensor = targetObject.GetComponent<VisionSensor>();

            if ((targetObject.gameObject.layer != LayerMask.NameToLayer("VisionSensor")))
                targetObject.gameObject.layer = LayerMask.NameToLayer("VisionSensor");
        }

        static void SetShooterTargetLayer(GameObject characterObject, string layerName)
        {
            var fighterCore = characterObject.GetComponent<FighterCore>();
            fighterCore.targetLayer = 1 << LayerMask.NameToLayer(layerName);
        }

        static void SetCoverLayerToCoverPrefab()
        {
            var coverPrefab = Resources.Load<GameObject>("Cover Barrier");
            var coverCollider = Resources.Load<GameObject>("Cover Collider");
            foreach (Transform coverCube in coverPrefab.transform)
            {
                if (coverCube.gameObject.layer != LayerMask.NameToLayer("Cover"))
                {
                    coverCube.gameObject.layer = LayerMask.NameToLayer("Cover");
                }
            } 
            if(coverCollider != null && coverCollider.gameObject.layer != LayerMask.NameToLayer("Cover"))
            {
                coverCollider.gameObject.layer = LayerMask.NameToLayer("Cover");
            }
        }
    }
}
#endif