using FS_Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_ShooterSystem
{
    public class ShooterIKSetupEditor : EditorWindow
    {
        static ShooterIKSetupEditor window;
        public static ShooterFighter Fighter { get; private set; }
        static Transform SupportHandTarget => Fighter.CurrentShooterWeaponObject.supportHandIkTarget;
        static Transform HoldHandTarget => Fighter.CurrentShooterWeaponObject.holdHandIkTarget;

        static Color green = new Color(0.2f, 0.8f, 0.3f);
        static Color blue = new Color(.2f, 0.3f, 0.5f);
        static Color red = new Color(0.9f, 0.1f, 0.1f);
        static bool saveAsDefault = true;

        public static void SetFighter(ShooterFighter shooterFighter)
        {
            if (!ReferenceEquals(Fighter, shooterFighter))
            {
                if (shooterFighter != null)
                {
                    Fighter = shooterFighter;
                    if (Fighter != null)
                    {
                        EditorGUIUtility.PingObject(Fighter.gameObject);
                    }
                    InitTargets();
                }
            }
        }

        [MenuItem("Tools/Shooter System/IK Setup")]
        public static void ShowWindow()
        {
            window = GetWindow<ShooterIKSetupEditor>("Ik");
            window.minSize = new Vector2(360, 404); // better default min size
            window.maxSize = new Vector2(360, 404); // better default min size
            InitTargets();
        }

        private void OnGUI()
        {
            if (Fighter == null)
            {
                if (Selection.activeGameObject != null)
                    SetFighter(Selection.activeGameObject.GetComponent<ShooterFighter>());

                if (Fighter == null)
                {
                    EditorGUILayout.HelpBox("Please select a GameObject with ShooterFighter component.", MessageType.Warning);
                    return;
                }
            }
            DrawSetupControls();
            if (Fighter.IkSetupMode)
            {
                if (Fighter.CurrentWeapon != null)
                {
                    DrawIdleAimIkButtons();

                    GUILayout.Space(10);
                    DrawIKSaveSection();
                    GUILayout.Space(10);
                    ShowEquippedWeaponDetails();
                }
            }
        }

        private void DrawSetupControls()
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Setup Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            Color originalColor = GUI.backgroundColor;

            // Main setup toggle
            GUI.backgroundColor = Fighter.IkSetupMode ? red : green;
            if (GUILayout.Button(Fighter.IkSetupMode ? "Stop Setup" : "Start Setup", GUILayout.Height(30)))
            {
                Fighter.IkSetupMode = !Fighter.IkSetupMode;

                if (Fighter.IsAiming)
                    Fighter.StopAiming();
            }
            GUI.backgroundColor = originalColor;

            if (Fighter.IkSetupMode && Fighter.CurrentWeapon == null)
            {
                if (Application.isPlaying)
                    EditorGUILayout.HelpBox("No weapon equipped. Please equip a weapon first.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("Enter Play Mode and equip a weapon to set up IK.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawIdleAimIkButtons()
        {
            if (Fighter.IkSetupMode)
            {
                float buttonWidth = (position.width / 2f) - 10f;
                Color originalColor = GUI.backgroundColor;
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8);
                // IK Setup
                GUI.backgroundColor = Fighter.IsAiming ? green : Color.white;
                if (GUILayout.Button("Aim", GUILayout.Width(buttonWidth), GUILayout.Height(28)))
                {
                    Selection.activeObject = null;
                    Fighter.StartAiming();
                }


                // Weapon Setup
                GUI.backgroundColor = !Fighter.IsAiming ? green : Color.white;
                if (GUILayout.Button("Idle", GUILayout.Width(buttonWidth), GUILayout.Height(28)))
                {
                    if (Fighter.IsAiming)
                        Fighter.StopAiming();
                }

                GUI.backgroundColor = originalColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIKSaveSection()
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");

            saveAsDefault = EditorGUILayout.Toggle("Save as Default", saveAsDefault);


            GUI.backgroundColor = blue;
            EditorGUILayout.BeginHorizontal("box");

            if (GUILayout.Button("Save Aim IK", GUILayout.Height(25)))
                SaveIKData(false);
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Clear", GUILayout.Height(25)))
                ClearData(true);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = blue;
            if (GUILayout.Button("Save Idle IK", GUILayout.Height(25)))
                SaveIKData(true);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Clear", GUILayout.Height(25)))
                ClearData(false);

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void ShowEquippedWeaponDetails()
        {
            GUILayout.Space(5);

            if (Fighter.CurrentWeapon != null)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("Current Weapon", Fighter.CurrentWeapon.name);
                EditorGUILayout.LabelField("Holder Bone", Fighter.CurrentWeapon.holderBone.ToString());
                EditorGUILayout.LabelField("Idle IK (Support Hand)", Fighter.CurrentWeapon.UseHandIkForIdle.ToString());

                EditorGUILayout.EndVertical();
            }
        }

        void SaveIKData(bool saveIdleData = true)
        {
            Undo.RecordObject(Fighter.CurrentWeapon, "Ik data modified");
            if (saveIdleData)
            {
                if (!saveAsDefault)
                {
                    var existingArmIKReference = Fighter.CurrentWeapon.iKReferences.Find(
                    r => r.characterReferences != null &&
                    r.characterReferences.Count > 0 &&
                    r.characterReferences.FirstOrDefault(c => c.charcaterId == Fighter.characterID.id || c.avatar == Fighter.Animator.avatar) != null);

                    if (existingArmIKReference != null)
                    {
                        existingArmIKReference.idleIKData.supportHandPosition = SupportHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localPosition;
                        existingArmIKReference.idleIKData.supportHandRotation = SupportHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localEulerAngles;
                        existingArmIKReference.idleIKData.holdHandPosition = HoldHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localPosition;
                        existingArmIKReference.idleIKData.holdHandRotation = HoldHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles;
                    }
                    else
                    {
                        var newIkReference = new ArmIKReference()
                        {
                            characterReferences = new List<CharacterReference>()
                            {
                                new CharacterReference
                                {
                                    charcaterId = Fighter.characterID.id,
                                    avatar = Fighter.Animator.avatar
                                }
                            },
                            idleIKData = new ArmIkData()
                            {
                                supportHandPosition = SupportHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localPosition,
                                supportHandRotation = SupportHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localEulerAngles,
                                holdHandPosition = HoldHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localPosition,
                                holdHandRotation = HoldHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles
                            }
                        };

                        Fighter.CurrentWeapon.iKReferences.Add(newIkReference);
                    }
                }
                else
                {
                    Fighter.CurrentWeapon.idleIKData.supportHandPosition = SupportHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localPosition;
                    Fighter.CurrentWeapon.idleIKData.supportHandRotation = SupportHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localEulerAngles;
                    Fighter.CurrentWeapon.idleIKData.holdHandPosition = HoldHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localPosition;
                    Fighter.CurrentWeapon.idleIKData.holdHandRotation = HoldHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles;
                }
                Debug.Log($"Idle IK data saved for {Fighter.CurrentWeapon.name}.");
            }
            else
            {
                if (!saveAsDefault)
                {
                    var existingArmIKReference = Fighter.CurrentWeapon.iKReferences.Find(
                    r => r.characterReferences != null &&
                    r.characterReferences.Count > 0 &&
                    r.characterReferences.FirstOrDefault(c => c.charcaterId == Fighter.characterID.id || c.avatar == Fighter.Animator.avatar) != null);

                    if (existingArmIKReference != null)
                    {
                        existingArmIKReference.aimIKData.supportHandPosition = SupportHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localPosition;
                        existingArmIKReference.aimIKData.supportHandRotation = SupportHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localEulerAngles;
                        existingArmIKReference.aimIKData.holdHandPosition = HoldHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localPosition;
                        existingArmIKReference.aimIKData.holdHandRotation = HoldHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles;
                    }
                    else
                    {
                        var newIkReference = new ArmIKReference()
                        {
                            characterReferences = new List<CharacterReference>()
                            {
                                new CharacterReference
                                {
                                    charcaterId = Fighter.characterID.id,
                                    avatar = Fighter.Animator.avatar
                                }
                            },
                            aimIKData = new ArmIkData()
                            {
                                supportHandPosition = SupportHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localPosition,
                                supportHandRotation = SupportHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localEulerAngles,
                                holdHandPosition = HoldHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localPosition,
                                holdHandRotation = HoldHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles
                            }
                        };

                        Fighter.CurrentWeapon.iKReferences.Add(newIkReference);
                    }
                }
                else
                {
                    Fighter.CurrentWeapon.aimIKData.supportHandPosition = SupportHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localPosition;
                    Fighter.CurrentWeapon.aimIKData.supportHandRotation = SupportHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.supportHandIkSetupHelperTarget.localEulerAngles;
                    Fighter.CurrentWeapon.aimIKData.holdHandPosition = HoldHandTarget.localPosition - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localPosition;
                    Fighter.CurrentWeapon.aimIKData.holdHandRotation = HoldHandTarget.localEulerAngles - Fighter.CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles;
                }
                Debug.Log($"Aim IK data saved for {Fighter.CurrentWeapon.name}.");
            }
            Fighter.UpdateExistingIkReference();
        }

        void ClearData(bool isAimData)
        {
            Undo.RecordObject(Fighter.CurrentWeapon, "Ik data modified");
            var existingArmIKReference = Fighter.CurrentWeapon.iKReferences.Find(
                     r => r.characterReferences != null &&
                     r.characterReferences.Count > 0 &&
                     r.characterReferences.FirstOrDefault(c => c.charcaterId == Fighter.characterID.id || c.avatar == Fighter.Animator.avatar) != null);

            if (existingArmIKReference != null)
            {
                if(isAimData)
                    existingArmIKReference.aimIKData = new ArmIkData();
                else
                    existingArmIKReference.idleIKData = new ArmIkData();

                var dataType = isAimData ? "Aim" : "Idle";
                Debug.Log($"{Fighter.CurrentWeapon} {dataType} IK data cleared.");
            }
            else
            {
                bool confirm = EditorUtility.DisplayDialog(
                               "No Data Found",
                               "No Aim IK data was found for this character on the current weapon.\n\n" +
                               "Do you want to clear the default data? Only clear it if the default data is not used by other weapons.",
                               "OK",
                               "Cancel"
                           );

                if (confirm)
                {
                    if (isAimData)
                        Fighter.CurrentWeapon.aimIKData = new ArmIkData();
                    else
                        Fighter.CurrentWeapon.idleIKData = new ArmIkData();

                    var dataType = isAimData ? "Aim" : "Idle";
                    Debug.Log($"{Fighter.CurrentWeapon} default {dataType} IK data has been cleared.");
                }
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            InitTargets();

            Selection.selectionChanged += () =>
            {
                if (Selection.activeGameObject != null)
                    SetFighter(Selection.activeGameObject.GetComponent<ShooterFighter>());
                if (window != null)
                {
                    window.Repaint();
                }
            };
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;

            if (Fighter != null)
            {
                Fighter.IkSetupMode = false;
                Fighter.StopAiming();
                Fighter = null;
            }
        }

        static void InitTargets()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        #region Gizmos
        static void OnSceneGUI(SceneView sceneView)
        {
            if (Application.isPlaying && Fighter != null && Fighter.CurrentWeapon != null && !Fighter.CurrentWeapon.isDualItem && Fighter.CurrentShooterWeaponObject.aimController != null)
            {
                HandleIKTargetSelection(SupportHandTarget, new Color(0f, 1f, 1f, 0.5f));
                HandleIKTargetSelection(HoldHandTarget, new Color(0f, 1f, 1f, 0.5f));

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    HandleMouseClick(Event.current.mousePosition, SupportHandTarget);
                    HandleMouseClick(Event.current.mousePosition, HoldHandTarget);
                }
            }
        }

        static void HandleIKTargetSelection(Transform target, Color color)
        {
            if (target == null) return;
            Handles.color = color;

            float radius = 0.015f;
            bool isSelected = Selection.activeGameObject == target.gameObject;

            Handles.DrawSolidDisc(target.position, Vector3.up, radius * 0.8f);
            Handles.DrawSolidDisc(target.position, Vector3.forward, radius * 0.8f);
            Handles.DrawSolidDisc(target.position, Vector3.right, radius * 0.8f);

            if (isSelected)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.8f);
                Handles.DrawWireDisc(target.position, Vector3.up, radius * 1.2f);
                Handles.DrawWireDisc(target.position, Vector3.forward, radius * 1.2f);
                Handles.DrawWireDisc(target.position, Vector3.right, radius * 1.2f);
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(target.position, radius));
        }

        static void HandleMouseClick(Vector2 mousePosition, Transform target)
        {
            float minDistance = float.MaxValue;
            Transform closestTarget = null;

            Vector2 screenPos = HandleUtility.WorldToGUIPoint(target.position);
            float distance = Vector2.Distance(mousePosition, screenPos);

            if (distance < 10f && distance < minDistance)
            {
                minDistance = distance;
                closestTarget = target;
            }
            if (closestTarget != null)
            {
                Selection.activeGameObject = closestTarget.gameObject;
                SceneView.RepaintAll();
                Event.current.Use();
            }
        }

        private void OnSelectionChange()
        {
            SceneView.RepaintAll();
        }

        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (Fighter == null) return;
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            if (obj == Fighter.gameObject)
            {
                Rect rect = new Rect(selectionRect.xMax - 60, selectionRect.y, 60, selectionRect.height);
                GUIStyle style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = Color.green;
                GUI.Label(rect, "IK Setup", style);
            }
        }
        #endregion
    }
}
