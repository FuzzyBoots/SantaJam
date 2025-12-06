#if UNITY_EDITOR
using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace FS_ParkourAI
{
    [CustomEditor(typeof(NavMeshLinkGenerator))]
    public class CreateNavMeshLinkInspector : Editor
    {
        Vector3 startPoint = Vector3.zero;
        Vector3 dragPoint = Vector3.zero;

        static bool mouseDown = false;
        static bool startLink = false;

        static GameObject linkObject;
        public static CustomNavmeshLink navmeshLink, previousLink;
        public static Action<SceneView> sceneView;


        static int areaType = 2;
        static bool biDirectional = true;
        static bool autoUpdatePosition = true;


        GUIStyle redButton;
        GUIStyle greenButton;
        GUIStyle deleteButton;
        GUIStyle blueButton;

        static bool pickStartLedge;
        static bool pickEndLedge;

        bool LedgePicking => pickStartLedge || pickEndLedge;

        SerializedProperty m_startLedge;
        SerializedProperty m_endLedge;
        SerializedProperty m_manualLedge;

        static int activeTool = 2;


        public override void OnInspectorGUI()
        {
            redButton = GetStyle(HexToColor("#9C0000"), HexToColor("#780000"), Color.white, HexToColor("#959595"), 20);
            greenButton = GetStyle(HexToColor("#006937"), HexToColor("#00592F"), Color.white, HexToColor("#959595"), 20);
            deleteButton = GetStyle(HexToColor("#000000"), HexToColor("#1E1D1D"), Color.white, HexToColor("#959595"), 15);
            blueButton = GetStyle(HexToColor("#0E3AD7"), HexToColor("#0D2888"), Color.white, HexToColor("#959595"), 10);

            var styleButtonStyle = !startLink ? greenButton : redButton;
            var rect = new Rect(20, 20, 230, 50);
            if (GUI.Button(rect, !startLink ? "Start" : "Stop", styleButtonStyle))
            {
                startLink = !startLink;
                if (startLink)
                    HandleOnSceneGUI();
            }

            var rect2 = new Rect(270, 20, 117, 50);
            var deleteButtonStyle = deleteButton;
            if (GUI.Button(rect2, "Delete Link", deleteButtonStyle))
                DeleteLink();
            GUILayout.Space(80);

            if (navmeshLink != null)
            {
                if (!navmeshLink.validPoint)
                    EditorGUILayout.HelpBox("This is not a valid link", MessageType.Warning);
                GUILayout.Space(10);
                CreateEditor(navmeshLink).OnInspectorGUI();

                areaType = navmeshLink.area;
                biDirectional = navmeshLink.bidirectional;
                autoUpdatePosition = navmeshLink.autoUpdate;


                if (navmeshLink.area == NavMesh.GetAreaFromName("Climbable"))
                {
                    redButton.fontSize = 10;
                    greenButton.fontSize = 10;


                    float inspectorWidth = EditorGUIUtility.currentViewWidth;
                    GUILayout.Box("", GUILayout.Width(inspectorWidth - 20), GUILayout.Height(navmeshLink.manualLedge ? 115 : 30));

                    Rect lastRect = GUILayoutUtility.GetLastRect();

                    SerializedObject serializedObject = new SerializedObject(navmeshLink);
                    serializedObject.Update();
                    m_startLedge = serializedObject.FindProperty("startLedge");
                    m_endLedge = serializedObject.FindProperty("endLedge");
                    m_manualLedge = serializedObject.FindProperty("manualLedge");


                    EditorGUI.PropertyField(new Rect(lastRect.x + 10, lastRect.y + 8, inspectorWidth, 15), m_manualLedge);

                    if (navmeshLink.manualLedge)
                    {
                        styleButtonStyle = !pickStartLedge ? greenButton : blueButton;
                        EditorGUI.PropertyField(new Rect(lastRect.x + 10, lastRect.y + 40, inspectorWidth * .7f, 25), m_startLedge);
                        if (GUI.Button(new Rect(lastRect.x + 15 + inspectorWidth * .7f, lastRect.y + 40, inspectorWidth * .3f - 40, 25), "Pick Ledge", styleButtonStyle))
                        {
                            pickStartLedge = !pickStartLedge;
                            if (pickStartLedge)
                                HandleOnSceneGUI();
                            if (pickStartLedge && pickEndLedge)
                                pickEndLedge = false;
                        }

                        GUILayout.Space(10);

                        styleButtonStyle = !pickEndLedge ? greenButton : redButton;
                        EditorGUI.PropertyField(new Rect(lastRect.x + 10, lastRect.y + 75, inspectorWidth * .7f, 25), m_endLedge);

                        if (GUI.Button(new Rect(lastRect.x + 15 + inspectorWidth * .7f, lastRect.y + 75, inspectorWidth * .3f - 40, 25), "Pick Ledge", styleButtonStyle))
                        {
                            pickEndLedge = !pickEndLedge;
                            if (pickEndLedge)
                                HandleOnSceneGUI();
                            if (pickStartLedge && pickEndLedge)
                                pickStartLedge = false;
                        }
                    }

                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    pickStartLedge = pickEndLedge = false;
                }


            }
        }

        void HandleOnSceneGUI()
        {
            Selection.activeObject = linkObject = GameObject.FindGameObjectWithTag("NavmeshLink");
            SceneView.duringSceneGui -= sceneView;
            SceneView.duringSceneGui += OnSceneGUI;
            sceneView = OnSceneGUI;
        }

        private void OnEnable()
        {
            HandleOnSceneGUI();
        }


        void DrawButtons(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(5, sceneView.position.height - 90, 160, 60));
            var blackTex = GetStyle(HexToColor("#000000"), HexToColor("#000000"), Color.white, HexToColor("#000000"));
            GUILayout.Box("", blackTex, GUILayout.Width(160), GUILayout.Height(60));
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(10, sceneView.position.height - 85, 150, 50));
            EditorGUI.BeginChangeCheck();
            var ToolIcons = new GUIContent[2]
              {
                new GUIContent("Link","Offmesh Link Editor"),
                new GUIContent("Surface","Navmesh Surface Editor")
              };

            activeTool = GUILayout.Toolbar(activeTool, ToolIcons, GUILayout.Height(50));
            if (EditorGUI.EndChangeCheck())
            {
                switch (activeTool)
                {
                    case 0:
                        SetLinkObject();
                        break;
                    case 1:
                        OpenSufaceEditor();
                        break;
                    default:
                        break;
                }
                activeTool = -1;
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }
        public void OnSceneGUI(SceneView sceneView)
        {
            if (startLink || LedgePicking)
            {

                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                Tools.current = Tool.None;

                HandleMouseEvents();
            }
            if (navmeshLink != previousLink)
            {
                Repaint();
                previousLink = navmeshLink;
            }

            DrawButtons(sceneView);
        }

        void HandleMouseEvents()
        {
            Event currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0) // Left mouse button down
                    {
                        if (Selection.activeObject != linkObject)
                        {
                            startLink = pickStartLedge = pickEndLedge = false;
                            return;
                        }
                        if (LedgePicking)
                            AddLedge(navmeshLink);
                        else
                        {
                            startPoint = GetCurrentMousePosition();
                            if (startPoint != Vector3.zero)
                            {
                                dragPoint = startPoint;
                                mouseDown = true;
                                CreateLink();
                            }
                        }
                        Selection.activeObject = linkObject;
                        this.Repaint();
                    }
                    break;

                case EventType.MouseDrag:
                    if (currentEvent.button == 0 && mouseDown) // Left mouse button drag
                    {
                        if (!LedgePicking)
                            navmeshLink.endPoint = GetCurrentMousePosition();
                    }
                    break;

                case EventType.MouseUp:
                    if (currentEvent.button == 0) // Left mouse button up
                    {
                        if (!LedgePicking && mouseDown)
                        {
                            navmeshLink.endPoint = GetCurrentMousePosition();
                            mouseDown = false;
                            CustomNavMeshLinkEditor.s_SelectedID = navmeshLink.GetInstanceID();
                            CustomNavMeshLinkEditor.s_SelectedPoint = 1;
                        }
                        this.Repaint();
                    }
                    break;
            }
        }
        Vector3 GetCurrentMousePosition()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                if (NavMesh.SamplePosition(hitInfo.point, out NavMeshHit hit, 0.8f, NavMesh.AllAreas))
                    return hit.position;
                return hitInfo.point;
            }
            return Vector3.zero;
        }

        [InitializeOnLoadMethod]
        static void InitializeLinkObject()
        {
            linkObject = GameObject.FindGameObjectWithTag("NavmeshLink");
            if (linkObject != null)
            {
                var editor = linkObject.GetComponent<NavMeshLinkGenerator>();
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(editor, true);
                //Selection.activeObject = linkObject;
            }
        }



        [MenuItem("Tools/Parkour AI/Create Offmesh Link", priority = 2)]
        static void SetLinkObject()
        {
            NavMeshLinkGenerator editor;
            if (navmeshLink != null)
            {
                areaType = navmeshLink.area;
                biDirectional = navmeshLink.bidirectional;
                autoUpdatePosition = navmeshLink.autoUpdate;
            }
            if (linkObject == null)
            {
                linkObject = GameObject.FindGameObjectWithTag("NavmeshLink");
                if (linkObject == null)
                {
                    linkObject = new GameObject("Navmesh Link");
                    linkObject.AddComponent<NavMeshLinkGenerator>();
                    Undo.RegisterCreatedObjectUndo(linkObject, "New link created");
                    linkObject.tag = "NavmeshLink";
                }
            }
            editor = linkObject.GetComponent<NavMeshLinkGenerator>();
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(editor, true);
            Selection.activeObject = linkObject;
        }

        void CreateLink()
        {
            SetLinkObject();

            navmeshLink = (CustomNavmeshLink)Undo.AddComponent(linkObject, typeof(CustomNavmeshLink));
            navmeshLink.startPoint = startPoint;
            navmeshLink.endPoint = startPoint;
            navmeshLink.autoUpdate = true;


            navmeshLink.area = areaType;
            navmeshLink.bidirectional = biDirectional;
            navmeshLink.autoUpdate = autoUpdatePosition;
            Repaint();
        }
        void DeleteLink()
        {
            if (navmeshLink != null)
            {
                Undo.DestroyObjectImmediate(navmeshLink);
                Repaint();
            }
        }

        #region Pick ledge
        void AddLedge(CustomNavmeshLink link)
        {
            var ledge = GetLedge();

            if (ledge != null)
            {
                if (pickStartLedge)
                {
                    Undo.RecordObject(navmeshLink, "start ledge added");
                    link.startLedge = ledge;
                    pickStartLedge = false;
                }
                else if (pickEndLedge)
                {
                    Undo.RecordObject(navmeshLink, "end ledge added");
                    link.endLedge = ledge;
                    pickEndLedge = false;
                }
            }
        }
        Collider GetLedge()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;
            LayerMask ledge = LayerMask.GetMask("Ledge");
            if (Physics.Raycast(ray, out hitInfo, 20f, ledge))
                return hitInfo.collider;
            return null;
        }
        #endregion

        #region texture
        public static Color HexToColor(string hex)
        {
            Color color = new Color();
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }
        GUIStyle GetStyle(Color nor, Color hov, Color texNor, Color texHov, int fontSize = 10)
        {
            GUIStyle style = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = fontSize,

                normal = new GUIStyleState()
                {
                    background = CreateTexture(nor, 1, 1),
                    textColor = texNor
                },
                hover = new GUIStyleState()
                {
                    background = CreateTexture(hov, 1, 1),
                    textColor = texHov
                },
                active = new GUIStyleState()
                {
                    background = CreateTexture(nor, 1, 1),
                    textColor = texNor
                }
            };

            return style;
        }
        private Texture2D CreateTexture(Color color, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);

            // Fill the texture with the specified color
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
        #endregion

        #region surface editor
        static GameObject navmeshSurfaceGameObject;
        static NavMeshSurface navMeshSurface;
        [MenuItem("Tools/Parkour AI/Setup Navmesh Surface", priority = 1)]
        static void OpenSufaceEditor()
        {
            if (navmeshSurfaceGameObject == null)
            {
                navmeshSurfaceGameObject = GameObject.FindGameObjectWithTag("NavmeshSurface");
                if (navmeshSurfaceGameObject == null)
                {
                    var prefab = (GameObject)Resources.Load("NavMesh Surface");
                    navmeshSurfaceGameObject = Instantiate(prefab);
                    Undo.RegisterCreatedObjectUndo(navmeshSurfaceGameObject, "NavMeshSurface created");
                    navmeshSurfaceGameObject.name = "Navmesh Surface";
                    navmeshSurfaceGameObject.tag = "NavmeshSurface";
                }
                navMeshSurface = navmeshSurfaceGameObject.GetComponent<CustomNavMeshSurface>();
                if (navMeshSurface == null)
                    navMeshSurface = (CustomNavMeshSurface)Undo.AddComponent(navmeshSurfaceGameObject, typeof(CustomNavMeshSurface));
            }
            Selection.activeObject = navmeshSurfaceGameObject;
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(navMeshSurface, true);
        }
        #endregion
    }
}
#endif