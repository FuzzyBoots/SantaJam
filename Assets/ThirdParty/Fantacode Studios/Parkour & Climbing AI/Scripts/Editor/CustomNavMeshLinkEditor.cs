#if UNITY_EDITOR
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace FS_ParkourAI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CustomNavmeshLink))]

    class CustomNavMeshLinkEditor : Editor
    {
        SerializedProperty m_AgentTypeID;
        SerializedProperty m_Area;
        SerializedProperty m_CostModifier;
        SerializedProperty m_AutoUpdatePosition;
        SerializedProperty m_Bidirectional;
        SerializedProperty m_EndPoint;
        SerializedProperty m_StartPoint;
        SerializedProperty m_Width;


        public static int s_SelectedID = 0;
        public static int s_SelectedPoint = -1;

        static Color s_HandleColor = new Color(255f, 167f, 39f, 210f) / 255;
        static Color s_HandleColorDisabled = new Color(255f * 0.75f, 167f * 0.75f, 39f * 0.75f, 100f) / 255;


        bool pickStartLedge;
        bool pickEndLedge;

        static Vector3 ledgeCheckBoxHalfExtends = new Vector3(0.8f, 1.6f, 0.8f);

        void OnEnable()
        {
            try
            {
                m_AgentTypeID = serializedObject.FindProperty("m_AgentTypeID");
                m_Area = serializedObject.FindProperty("m_Area");
                m_CostModifier = serializedObject.FindProperty("m_CostModifier");
                m_AutoUpdatePosition = serializedObject.FindProperty("m_AutoUpdatePosition");
                m_Bidirectional = serializedObject.FindProperty("m_Bidirectional");
                m_EndPoint = serializedObject.FindProperty("m_EndPoint");
                m_StartPoint = serializedObject.FindProperty("m_StartPoint");
                m_Width = serializedObject.FindProperty("m_Width");
            }
            catch { }


#if !UNITY_2022_2_OR_NEWER
        NavMeshVisualizationSettings.showNavigation++;
#endif
        }

#if !UNITY_2022_2_OR_NEWER
    void OnDisable()
    {
        NavMeshVisualizationSettings.showNavigation--; 
    }
#endif

        static Matrix4x4 UnscaledLocalToWorldMatrix(Transform t)
        {
            return Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
        }

        static void AlignTransformToEndPoints(CustomNavmeshLink navLink)
        {
            var mat = UnscaledLocalToWorldMatrix(navLink.transform);

            var worldStartPt = mat.MultiplyPoint(navLink.startPoint);
            var worldEndPt = mat.MultiplyPoint(navLink.endPoint);

            var forward = worldEndPt - worldStartPt;
            var up = navLink.transform.up;

            // Flatten
            forward -= Vector3.Dot(up, forward) * up;

            var transform = navLink.transform;
            transform.rotation = Quaternion.LookRotation(forward, up);
            transform.position = (worldEndPt + worldStartPt) * 0.5f;
            transform.localScale = Vector3.one;

            navLink.startPoint = transform.InverseTransformPoint(worldStartPt);
            navLink.endPoint = transform.InverseTransformPoint(worldEndPt);
        }

        public override void OnInspectorGUI()
        {

            serializedObject.Update();

            NavMeshComponentsGUIUtility.AgentTypePopup("Agent Type", m_AgentTypeID);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_StartPoint);
            EditorGUILayout.PropertyField(m_EndPoint);

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button("Swap"))
            {
                foreach (CustomNavmeshLink navLink in targets)
                {
                    var tmp = navLink.startPoint;
                    navLink.startPoint = navLink.endPoint;
                    navLink.endPoint = tmp;
                }
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Align Transform"))
            {
                foreach (CustomNavmeshLink navLink in targets)
                {
                    Undo.RecordObject(navLink.transform, "Align Transform to End Points");
                    Undo.RecordObject(navLink, "Align Transform to End Points");
                    AlignTransformToEndPoints(navLink);
                }
                SceneView.RepaintAll();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Width);
            EditorGUILayout.PropertyField(m_CostModifier);
            EditorGUILayout.PropertyField(m_AutoUpdatePosition);
            EditorGUILayout.PropertyField(m_Bidirectional);

            NavMeshComponentsGUIUtility.AreaPopup("Area Type", m_Area);



            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
        }

        static Vector3 CalcLinkRight(CustomNavmeshLink navLink)
        {
            var dir = navLink.endPoint - navLink.startPoint;
            return (new Vector3(-dir.z, 0.0f, dir.x)).normalized;
        }

        static void DrawLink(CustomNavmeshLink navLink)
        {
            var right = CalcLinkRight(navLink);
            var rad = navLink.width * 0.5f;

            Gizmos.DrawLine(navLink.startPoint - right * rad, navLink.startPoint + right * rad);
            Gizmos.DrawLine(navLink.endPoint - right * rad, navLink.endPoint + right * rad);
            Gizmos.DrawLine(navLink.startPoint - right * rad, navLink.endPoint - right * rad);
            Gizmos.DrawLine(navLink.startPoint + right * rad, navLink.endPoint + right * rad);
        }

#if !UNITY_2022_2_OR_NEWER
    [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
#else
        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Active | GizmoType.Pickable)]
#endif
        static void RenderBoxGizmo(CustomNavmeshLink navLink, GizmoType gizmoType)
        {
            if (!EditorApplication.isPlaying && navLink.isActiveAndEnabled)
                navLink.UpdateLink();

            var color = s_HandleColor;
            if (!navLink.enabled)
                color = s_HandleColorDisabled;

            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            Gizmos.matrix = UnscaledLocalToWorldMatrix(navLink.transform);

            Gizmos.color = color;
            DrawLink(navLink);

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;

            Gizmos.DrawIcon(navLink.transform.position, "NavMeshLink Icon", true);


            if (navLink != null && navLink.GetInstanceID() == s_SelectedID && navLink.area == NavMesh.GetAreaFromName("Climbable"))
            {
                var ledge = LayerMask.GetMask("Ledge");

                if (navLink.startLedge != null)
                {
                    var meshFilter = navLink.startLedge.GetComponent<MeshFilter>();
                    Mesh mesh = meshFilter.sharedMesh;
                    if (mesh != null)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireMesh(mesh, navLink.startLedge.transform.position, navLink.startLedge.transform.rotation, navLink.startLedge.transform.localScale);
                    }

                }

                if (!navLink.manualLedge)
                {
                    Collider[] collider = new Collider[1];
                    Physics.OverlapBoxNonAlloc(navLink.startPoint + Vector3.up * 1.4f, ledgeCheckBoxHalfExtends, collider, Quaternion.identity, ledge);
                    navLink.startLedge = collider[0];
                    Gizmos.color = navLink.startLedge == null ? Color.white : Color.blue;
                    Gizmos.DrawWireCube(navLink.startPoint + Vector3.up * 1.4f, ledgeCheckBoxHalfExtends * 2);
                }

                if (navLink.endLedge != null)
                {
                    var meshFilter = navLink.endLedge.GetComponent<MeshFilter>();
                    Mesh mesh = meshFilter.sharedMesh;
                    if (mesh != null)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireMesh(mesh, navLink.endLedge.transform.position, navLink.endLedge.transform.rotation, navLink.endLedge.transform.localScale);
                    }
                }

                if (!navLink.manualLedge)
                {
                    Collider[] collider = new Collider[1];
                    Physics.OverlapBoxNonAlloc(navLink.endPoint + Vector3.up * 1.4f, ledgeCheckBoxHalfExtends, collider, Quaternion.identity, ledge);
                    navLink.endLedge = collider[0];
                    Gizmos.color = navLink.endLedge == null ? Color.white : Color.red;
                    Gizmos.DrawWireCube(navLink.endPoint + Vector3.up * 1.4f, ledgeCheckBoxHalfExtends * 2);
                }
            }
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void RenderBoxGizmoNotSelected(CustomNavmeshLink navLink, GizmoType gizmoType)
        {
#if !UNITY_2022_2_OR_NEWER
        if (NavMeshVisualizationSettings.showNavigation > 0)
#endif
            {
                var color = s_HandleColor;
                if (!navLink.enabled)
                    color = s_HandleColorDisabled;

                var oldColor = Gizmos.color;
                var oldMatrix = Gizmos.matrix;

                Gizmos.matrix = UnscaledLocalToWorldMatrix(navLink.transform);

                Gizmos.color = color;
                DrawLink(navLink);

                Gizmos.matrix = oldMatrix;
                Gizmos.color = oldColor;
            }

            Gizmos.DrawIcon(navLink.transform.position, "NavMeshLink Icon", true);



        }

        public void OnSceneGUI()
        {
            var navLink = (CustomNavmeshLink)target;
            if (!navLink.enabled)
                return;
            var mat = UnscaledLocalToWorldMatrix(navLink.transform);

            var startPt = mat.MultiplyPoint(navLink.startPoint);
            var endPt = mat.MultiplyPoint(navLink.endPoint);
            var midPt = Vector3.Lerp(startPt, endPt, 0.35f);
            var startSize = HandleUtility.GetHandleSize(startPt);
            var endSize = HandleUtility.GetHandleSize(endPt);
            var midSize = HandleUtility.GetHandleSize(midPt);

            var zup = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
            var right = mat.MultiplyVector(CalcLinkRight(navLink));

            var oldColor = Handles.color;
            Handles.color = s_HandleColor;

            Vector3 pos;

            var navLinkSelected = false;

            if (navLink.GetInstanceID() == s_SelectedID && (s_SelectedPoint == 1 || s_SelectedPoint == 0))
            {
                Handles.color = Color.green;
                navLinkSelected = true;
            }
            HandleLink(navLink, true, startPt, startSize, zup, mat, 0, navLinkSelected);

            HandleLink(navLink, false, endPt, endSize, zup, mat, 1, navLinkSelected);


            EditorGUI.BeginChangeCheck();
            pos = Handles.Slider(midPt + right * navLink.width * 0.5f, right, midSize * 0.03f, Handles.DotHandleCap, 0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(navLink, "Adjust link width");
                navLink.width = Mathf.Max(0.0f, 2.0f * Vector3.Dot(right, (pos - midPt)));
            }

            EditorGUI.BeginChangeCheck();
            pos = Handles.Slider(midPt - right * navLink.width * 0.5f, -right, midSize * 0.03f, Handles.DotHandleCap, 0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(navLink, "Adjust link width");
                navLink.width = Mathf.Max(0.0f, 2.0f * Vector3.Dot(-right, (pos - midPt)));
            }

            Handles.color = oldColor;
        }

        void HandleLink(CustomNavmeshLink navLink, bool startPoint, Vector3 point, float size, Quaternion zup, Matrix4x4 mat, int sID, bool navLinkSelected)
        {
            var pos = startPoint ? navLink.startPoint : navLink.endPoint;
            if (navLinkSelected)
            {
                var agent = NavMesh.GetSettingsByID(navLink.agentTypeID);
                var samplePos = NavMesh.SamplePosition(pos, out NavMeshHit hit, agent.agentHeight, NavMesh.AllAreas);
                var diff = pos - hit.position;
                diff.y = 0;

                if (samplePos && diff.magnitude <= agent.agentRadius)
                {
                    Handles.color = Color.green;
                    navLink.validPoint = true;
                }
                else
                {
                    Handles.color = Color.red;
                    navLink.validPoint = false;
                }
            }

            if (navLink.GetInstanceID() == s_SelectedID && s_SelectedPoint == sID)
            {



                EditorGUI.BeginChangeCheck();

                Handles.CubeHandleCap(0, point, zup, 0.1f * size, Event.current.type);
                pos = Handles.PositionHandle(point, navLink.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(navLink, "Move link point");
                    if (startPoint)
                        navLink.startPoint = mat.inverse.MultiplyPoint(pos);
                    else
                        navLink.endPoint = mat.inverse.MultiplyPoint(pos);
                }
            }
            else
            {
                if (Handles.Button(point, zup, 0.1f * size, 0.1f * size, Handles.CubeHandleCap))
                {
                    s_SelectedPoint = sID;
                    s_SelectedID = navLink.GetInstanceID();
                    CreateNavMeshLinkInspector.navmeshLink = navLink;
                }
            }


        }
    }
}
#endif
