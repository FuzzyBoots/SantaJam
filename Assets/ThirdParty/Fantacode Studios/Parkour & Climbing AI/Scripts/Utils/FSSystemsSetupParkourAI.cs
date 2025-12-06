#if UNITY_EDITOR
using FS_ParkourAI;
using FS_ThirdPerson;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_Core
{
    public partial class FSSystemsSetup
    {
        public static FSSystemInfo ParkourAISystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.AI,
            systemName: "Parkour And Climbing AI",
            displayName: "Parkour and Climbing AI",
            prefabName: "Parkour AI Controller",
            welcomeEditorShowKey: "ParkourAISystem_WelcomeWindow_Opened",
            systemProjectSettings: new SystemProjectSettingsData
            (
                tags: new List<string>() { "NavmeshLink", "NavmeshSurface" },
                extraSetupAction: () => { AddTagsAndArea(); }
            ),
            extraSetupActionAI: (GameObject aiObject, GameObject prefabObject) =>
            {
                SetFollowType(aiObject);
            }
        );

        static string ParkourAISystemWelcomeEditorKey => ParkourAISystemSetup.welcomeEditorShowKey;


        [InitializeOnLoadMethod]
        public static void LoadParkourAISystem()
        {
            if (!string.IsNullOrEmpty(ParkourAISystemWelcomeEditorKey) && !EditorPrefs.GetBool(ParkourAISystemWelcomeEditorKey, false))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false); 
                EditorPrefs.SetBool(ParkourAISystemWelcomeEditorKey, true);
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }


        static void AddTagsAndArea()
        {
            var newAreas = new[]
            {
                new
                {
                    name = "Climbable",
                    cost = 3f
                },
            };


            SerializedObject navmesh = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
            SerializedProperty areaProperty = navmesh.FindProperty("areas");
            if (areaProperty != null)
            {
                foreach (var area in newAreas)
                {
                    string areaName = area.name;
                    if (!string.IsNullOrEmpty(areaName))
                    {
                        bool areaExists = false;
                        for (int j = 0; j < areaProperty.arraySize; j++)
                        {
                            SerializedProperty layerProp = areaProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name");
                            if (layerProp.stringValue == areaName)
                            {
                                areaExists = true;
                                break;
                            }
                        }

                        if (!areaExists)
                        {
                            for (int j = 0; j < areaProperty.arraySize; j++)
                            {
                                SerializedProperty areaNameProperty = areaProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name");
                                if (string.IsNullOrEmpty(areaNameProperty.stringValue))
                                {
                                    SerializedProperty costProperty = areaProperty.GetArrayElementAtIndex(j).FindPropertyRelative("cost");
                                    areaNameProperty.stringValue = areaName;
                                    costProperty.floatValue = area.cost;
                                    break;
                                }
                            }
                        }
                    }
                }
                navmesh.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("Failed to find 'areas' property.");
            }
        }

        static void SetFollowType(GameObject aiObject)
        {
            var aiController = aiObject.GetComponent<AIController>();

            var selectedSystemsCount = FSSystemsSetupEditorWindow.setupScript.CurrentFSSystemsForSetup.Count(s => s.Value.selected);
            if(selectedSystemsCount > 1)
            {
                aiController.followType = FollowType.None;
            }

            aiController.target = FindObjectOfType<PlayerController>()?.gameObject;
        }
    }
}
#endif