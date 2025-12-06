#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FS_ParkourAI
{

    public class ParkourAIWelcomeWindow : EditorWindow
    {

        static ParkourAIWelcomeWindow window;
        public static string windowShowedKey = "ParkourAI_Started";

        //[InitializeOnLoadMethod]
        public static void ShowWindow()
        {
            if (PlayerPrefs.GetString(windowShowedKey) != "FC_AI_showed")
            {
                AddTagesAndArea();
                InItWindow();
                SetAnimatorToParkourAIPrefab();
                PlayerPrefs.SetString(windowShowedKey, "FC_AI_showed");
            }
        }
        //[MenuItem("Tools/Parkour AI/Welcome Window", false, 30)]
        public static void InItWindow()
        {
            CreateWindow();
        }


        //[MenuItem("Tools/Parkour AI/Import tags", false, 30)]
        public static void AddTagsAndNavmeshAreas()
        {
            AddTagesAndArea();
            EditorUtility.DisplayDialog("Tags", "Tags successfully imported", "ok");
        }

        static void AddTagesAndArea()
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

            if (!InternalEditorUtility.tags.ToList().Contains("NavmeshSurface"))
                InternalEditorUtility.AddTag("NavmeshSurface");
            if (!InternalEditorUtility.tags.ToList().Contains("NavmeshLink"))
                InternalEditorUtility.AddTag("NavmeshLink");
        }



        static void CreateWindow()
        {
            if (window == null)
            {
                window = GetWindow<ParkourAIWelcomeWindow>("Welcome");
                window.minSize = new Vector2(400, 200);
                window.maxSize = new Vector2(400, 200);
            }
        }

        public static RuntimeAnimatorController SetAnimatorToParkourAIPrefab()
        {
            var parkoueAI = (GameObject)Resources.Load("Parkour AI");
            var parkourController = (GameObject)Resources.Load("Parkour Controller");
            var animator = parkourController.GetComponentInChildren<Animator>().runtimeAnimatorController;
            parkoueAI.GetComponent<Animator>().runtimeAnimatorController = animator;
            return animator;
        }

        private void OnGUI()
        {
            if (window == null)
                CreateWindow();
            GUILayout.Space(5);

            EditorGUILayout.HelpBox("\"Parkour and Climbing AI\" is an add-on for \"Parkour and Climbing Sytsem\", it's an innovative concept featuring dynamic traversal capabilities for non-player characters (NPCs). This allows them to use parkour and climbing actions to intelligently navigate complex environments. This method is suitable for realistic games in which the NPCs have to follow or avoid targets over a variety of terrain.", MessageType.None);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("QuickStart", GUILayout.Height(40), GUILayout.Width(position.width / 3.1f)))
                Application.OpenURL("https://fantacode.gitbook.io/parkour-and-climbing-ai/quickstart");
            if (GUILayout.Button("Documentation", GUILayout.Height(40), GUILayout.Width(position.width / 3.1f)))
                Application.OpenURL("https://fantacode.gitbook.io/parkour-and-climbing-ai/");
            if (GUILayout.Button("Videos", GUILayout.Height(40), GUILayout.Width(position.width / 3.1f)))
                Application.OpenURL("https://youtube.com/playlist?list=PLnbdyws4rcAsydRZvhwi-SO41x8ysESMe&si=ElSNqg3I2agy-Dfn");
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Box("", GUILayout.Height(2), GUILayout.Width(position.width));
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Parkour And Climbing System", GUILayout.Height(40), GUILayout.Width(position.width / 2f)))
                Application.OpenURL("https://assetstore.unity.com/packages/templates/systems/parkour-and-climbing-system-258182");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
#endif