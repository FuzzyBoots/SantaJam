#if UNITY_EDITOR
using FS_ThirdPerson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;

namespace FS_Core
{
    public partial class FSSystemsSetup : MonoBehaviour
    {
        public static string welcomeWindowOpenKey = "FS_WelcomeWindow_Opened";
        public static FSSystemInfo ThirdPersonControllerSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            selected: true,
            systemName: "Locomotion System",
            displayName: "Locomotion",

            systemProjectSettings: new SystemProjectSettingsData
            (
                layers: new List<string> { "Player", "FootTrigger", "Hotspot", "HitBone" }
            ),

            prefabName: "Locomotion Controller",
            welcomeEditorShowKey: "LocomotionSystem_WelcomeWindow_Opened_2"
        );

        static string LocomotionSystemWelcomeEditorKey => ThirdPersonControllerSystemSetup.welcomeEditorShowKey;

        [InitializeOnLoadMethod]
        public static void LoadLocomotionSystem()
        {
            if (!string.IsNullOrEmpty(LocomotionSystemWelcomeEditorKey) && !EditorPrefs.GetBool(LocomotionSystemWelcomeEditorKey, false))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                EditorPrefs.SetBool(LocomotionSystemWelcomeEditorKey, true);
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }

        private void Awake()
        {
            this.enabled = false;
        }

        // Filtered systems by current selected charcater type from window
        public Dictionary<string, FSSystemInfo> CurrentFSSystemsForSetup = new Dictionary<string, FSSystemInfo>();
        // All installed FS Systems
        public Dictionary<string, FSSystemInfo> AllInstalledSystems = new Dictionary<string, FSSystemInfo>();


        public void FindSystem()
        {
            AllInstalledSystems.Clear();
            CurrentFSSystemsForSetup.Clear();
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var field in fields)
            {
                // Check if the field's type is FSSystem
                if (field.FieldType == typeof(FSSystemInfo))
                {
                    // Get the value of the field and cast it to FSSystem
                    FSSystemInfo system = field.GetValue(this) as FSSystemInfo;

                    if (system != null)
                    {
                        if(system.characterType == FSSystemsSetupEditorWindow.characterType)
                            CurrentFSSystemsForSetup.TryAdd(field.Name, system);
                        AllInstalledSystems.TryAdd(field.Name, system);
                    }
                }
            }
        }
        public void EnableSystems(params string[] systemNames)
        {
            foreach (var s in CurrentFSSystemsForSetup)
            {
                if(systemNames.Contains(s.Value.systemName))
                    s.Value.selected = true;
                else
                    s.Value.selected = false;
            }
        }

        /// <summary>
        /// Loads the prefab from Resources and copies all its components to this GameObject.
        /// </summary>
        public GameObject CopyComponentsAndAnimControllerFromPrefab(string prefabName, AnimatorMergerUtility animatorMergerUtility, GameObject characterObject)
        {
            if (!string.IsNullOrEmpty(prefabName))
            {
                // Load the prefab from Resources
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                bool isPlayer = FSSystemsSetupEditorWindow.characterType == CharacterType.Player;
                if (prefab != null)
                {
                    var characterPrefab = isPlayer ? prefab.GetComponentInChildren<PlayerController>().gameObject: prefab;

                    var animatorController = characterPrefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;

                    animatorMergerUtility.MergeAnimatorControllers(animatorController);

                    FSSystemsSetupEditorWindow.CopyComponents(characterPrefab, characterObject);

                    //if (isPlayer)
                    //{
                    //    var managedScript = characterObject.GetComponents<SystemBase>().ToList();
                    //    managedScript.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                    //    characterObject.GetComponent<PlayerController>().managedScripts = managedScript;
                    //}
                }
                return prefab;
            }
            else
            {
                //Debug.LogWarning("Prefab name is not specified.");
            } 
            return null;
        }

        public void ImportProjectSettings()
        {
            FindSystem();
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            foreach (var systemProjectSettingsData in AllInstalledSystems.Values)
            {
                if (systemProjectSettingsData.systemProjectSettings == null) 
                    continue;
                for (int i = 0; i < systemProjectSettingsData.systemProjectSettings.layers.Count; i++)
                {
                    string layerName = systemProjectSettingsData.systemProjectSettings.layers[i];

                    if (!string.IsNullOrEmpty(layerName))
                    {
                        bool layerExists = false;
                        for (int j = 0; j < layersProp.arraySize; j++)
                        {
                            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(j);
                            if (layerProp.stringValue == layerName)
                            {
                                layerExists = true;
                                break;
                            }
                        }

                        if (!layerExists)
                        {
                            for (int j = 8; j < layersProp.arraySize; j++)
                            {
                                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(j);
                                if (string.IsNullOrEmpty(layerProp.stringValue))
                                {
                                    layerProp.stringValue = layerName;
                                    break;
                                }
                            }
                        }
                    }
                }
                tagManager.ApplyModifiedProperties();

                if (systemProjectSettingsData.systemProjectSettings.tags != null)
                {
                    foreach (var tag in systemProjectSettingsData.systemProjectSettings.tags)
                    {
                        if (!InternalEditorUtility.tags.ToList().Contains(tag))
                            InternalEditorUtility.AddTag(tag);
                    }
                }

                systemProjectSettingsData.systemProjectSettings.extraSetupAction?.Invoke();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var serializedObject = new SerializedObject(asset);
            var property = serializedObject.FindProperty("activeInputHandler");

            if(property.intValue == 1)
            {
                ScriptingDefineSymbolController.ToggleScriptingDefineSymbol(FSSystemsSetupEditorWindow.inputsystem, true);
            }
        }
    }
    public enum CharacterType
    {
        Player,
        AI,
        None
    }
    public class FSSystemInfo
    {
        public CharacterType characterType;
        public bool selected;
        public string systemName;
        public string displayName;
        public string prefabName;
        public string mobileControllerPrefabName;
        public Action<GameObject, GameObject, GameObject> extraSetupActionPlayer;
        public Action<GameObject, GameObject> extraSetupActionAI;
        public SystemProjectSettingsData systemProjectSettings;
        public string welcomeEditorShowKey;
        public FSSystemInfo(CharacterType characterType, string systemName, string displayName = "", string prefabName = "", bool selected = false, string welcomeEditorShowKey = "", SystemProjectSettingsData systemProjectSettings = null, string mobileControllerPrefabName = "", Action<GameObject, GameObject, GameObject> extraSetupActionPlayer = null, Action<GameObject, GameObject> extraSetupActionAI = null)
        {
            this.characterType = characterType;
            this.selected = selected;
            this.systemName = systemName;
            this.displayName = displayName;
            this.prefabName = prefabName;
            this.systemProjectSettings = systemProjectSettings;
            this.mobileControllerPrefabName = mobileControllerPrefabName;

            if (extraSetupActionPlayer != null)
                this.extraSetupActionPlayer = extraSetupActionPlayer;
            if (extraSetupActionAI != null)
                this.extraSetupActionAI = extraSetupActionAI;

            if (!string.IsNullOrEmpty(welcomeEditorShowKey))
                this.welcomeEditorShowKey = welcomeEditorShowKey;
            else
                this.welcomeEditorShowKey = FSSystemsSetup.welcomeWindowOpenKey;
        }

    }

    public class SystemProjectSettingsData 
    {
        public List<string> layers = new List<string>();
        public List<string> tags = new List<string>();

        public Action extraSetupAction;

        public SystemProjectSettingsData(List<string> layers = null, List<string> tags = null, Action extraSetupAction = null)
        {
            if (layers == null)
                this.layers = new List<string>();
            else
                this.layers = layers;
            this.tags = tags;
            this.extraSetupAction = extraSetupAction;
        }
    }
}
#endif