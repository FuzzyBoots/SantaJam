#if UNITY_EDITOR
using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using UnityEditor;
using UnityEngine;
namespace FS_CombatSystem
{
    public class CreateCombatCharacterEditorWindow : EditorWindow
    {
        public static CreateCombatCharacterEditorWindow window;

        public GameObject model;
        public bool isEnemy;
        bool isHumanoid;
        bool validAvathar;
        bool hasAnimator;
        bool validModel;



#if UNITY_EDITOR

        [MenuItem("Tools/Melee Combat System/Create Enemy", false, 2)]
        public static void InitEnemySetupWindow()
        {
            FSSystemsSetupEditorWindow.ShowWindow();
            FSSystemsSetup.MeleeCombatSystemAISetup.selected = true;
            FSSystemsSetupEditorWindow.ChangeCharacterType(CharacterType.AI);
        }

        [MenuItem("Tools/Melee Combat System/Create Player", false, 2)]
        public static void InitPlayerSetupWindow()
        {
            FSSystemsSetupEditorWindow.ShowWindow();
            FSSystemsSetup.MeleeCombatSystemSetup.selected = true;
            FSSystemsSetupEditorWindow.ChangeCharacterType(CharacterType.Player);
        }


        private void OnGUI()
        {
            GetWindow();
            GUILayout.Space(10);
            SetWarningAndErrors();
            model = (GameObject)UndoField(model, EditorGUILayout.ObjectField("Character Model", model, typeof(GameObject), true));
            //isEnemy = (bool)UndoField(isEnemy, EditorGUILayout.Toggle("Is Enemy", isEnemy));
            GUILayout.Space(2f);
            if (GUILayout.Button("Create Enemy"))
            {
                if (isEnemy)
                    CreateEnemy();
            }
        }


        void CreatePlayer()
        {
            if (validModel)
            {
                var playerPrefab = (GameObject)Resources.Load("Combat Controller");
                var combatController = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

                var model = Instantiate(this.model, Vector3.zero, Quaternion.identity);

                var playerController = combatController.GetComponentInChildren<PlayerController>();
                var playerGameObject = playerController.gameObject;
                var animator = playerGameObject.GetComponent<Animator>();
                var modelAnimator = model.GetComponent<Animator>();
                

                model.transform.SetParent(playerGameObject.transform);
                combatController.GetComponentInChildren<CameraController>().firstPersonCamera.defaultSettings.overridedFollowTarget = model.transform;
                combatController.GetComponentInChildren<CameraController>().thirdPersonCamera.defaultSettings.overridedFollowTarget = model.transform;
                animator.avatar = modelAnimator.avatar;
                combatController.name = playerPrefab.name;
                model.name = this.model.name;

                SetColliders(modelAnimator);

                if (animator.isHuman)
                {
                    var footTriggerPrefab = (GameObject)Resources.Load("FootTrigger");
                    var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot).transform;
                    var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform;
                    var rightCollider = PrefabUtility.InstantiatePrefab(footTriggerPrefab, rightFoot) as GameObject;
                    var leftCollider = PrefabUtility.InstantiatePrefab(footTriggerPrefab, leftFoot) as GameObject;
                    rightCollider.transform.localPosition = Vector3.zero;
                    leftCollider.transform.localPosition = Vector3.zero;

                    if ((rightCollider.layer != LayerMask.NameToLayer("FootTrigger")))
                        rightCollider.layer = LayerMask.NameToLayer("FootTrigger");
                    if ((leftCollider.layer != LayerMask.NameToLayer("FootTrigger")))
                        leftCollider.layer = LayerMask.NameToLayer("FootTrigger");
                }


                if ((playerGameObject.layer != LayerMask.NameToLayer("Player")))
                    playerGameObject.layer = LayerMask.NameToLayer("Player");

                //player.GetComponent<FootIK>().root = model.transform;

                Undo.RegisterCreatedObjectUndo(combatController, "new character controller added");
                Undo.RegisterCreatedObjectUndo(model, "new character added");
                Selection.activeObject = combatController;
                SceneView sceneView = SceneView.lastActiveSceneView;
                sceneView.Focus();
                sceneView.LookAt(combatController.transform.position);
            }
        }

        void CreateEnemy()
        {
            if (validModel)
            {
                var enemyPrefab = (GameObject)Resources.Load("Enemy Controller");
                var enemyController = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);

                var model = Instantiate(this.model, Vector3.zero, Quaternion.identity);

                var animator = enemyController.GetComponent<Animator>();
                var visionSensor = enemyController.GetComponentInChildren<VisionSensor>();
                var modelAnimator = model.GetComponent<Animator>();

                model.transform.SetParent(enemyController.transform);
                animator.avatar = modelAnimator.avatar;
                enemyController.name = this.model.name + " (Enemy)";
                model.name = this.model.name;

                SetColliders(modelAnimator);
                SetItemHandler(animator);

                if ((enemyController.layer != LayerMask.NameToLayer("Enemy")))
                    enemyController.layer = LayerMask.NameToLayer("Enemy");

                if ((visionSensor.gameObject.layer != LayerMask.NameToLayer("VisionSensor")))
                    visionSensor.gameObject.layer = LayerMask.NameToLayer("VisionSensor");

                Undo.RegisterCreatedObjectUndo(enemyController, "new character controller added");
                Undo.RegisterCreatedObjectUndo(model, "new character added");
                Selection.activeObject = enemyController;
                SceneView sceneView = SceneView.lastActiveSceneView;
                sceneView.Focus();
                sceneView.LookAt(enemyController.transform.position);
            }
        }

        public void SetColliders(Animator modelAnimator)
        {
            if (!modelAnimator.isHuman) return;

            AddColliderToBone(modelAnimator, HumanBodyBones.RightHand);
            AddColliderToBone(modelAnimator, HumanBodyBones.LeftHand);
            AddColliderToBone(modelAnimator, HumanBodyBones.RightFoot);
            AddColliderToBone(modelAnimator, HumanBodyBones.LeftFoot);

            AddColliderToBone(modelAnimator, HumanBodyBones.LeftLowerArm, "LeftElbowCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.RightLowerArm, "RightElbowCollider"); ;
            AddColliderToBone(modelAnimator, HumanBodyBones.LeftLowerLeg, "LeftKneeCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.RightLowerLeg, "RightKneeCollider");
            AddColliderToBone(modelAnimator, HumanBodyBones.Head, "HeadCollider");
        }

        private void SetItemHandler(Animator animator)
        {
            var rh = new GameObject("Right Hand Item Handler");
            rh.transform.parent = animator.GetBoneTransform(HumanBodyBones.RightHand);
            rh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            rh.AddComponent<EquippableItemHolder>();
            var lh = new GameObject("Left Hand Item Handler");
            lh.transform.parent = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            lh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            lh.AddComponent<EquippableItemHolder>();
        }

        void SetWarningAndErrors()
        {
            validModel = false;
            if (model != null)
            {
                var animator = model.GetComponent<Animator>();
                if (animator != null)
                {
                    hasAnimator = true;
                    isHumanoid = animator.isHuman;
                    validAvathar = animator.avatar != null && animator.avatar.isValid;
                }
                else
                    hasAnimator = isHumanoid = validAvathar = false;
                if (!hasAnimator)
                    EditorGUILayout.HelpBox("Animator Component is Missing", MessageType.Error);
                else if (!isHumanoid)
                    EditorGUILayout.HelpBox("Set your model animtion type to Humanoid", MessageType.Error);
                else if (!validAvathar)
                    EditorGUILayout.HelpBox(model.name + " is a invalid Humanoid", MessageType.Info);
                else
                {
                    EditorGUILayout.HelpBox("Make sure your FBX model is Humanoid", MessageType.Info);
                    validModel = true;
                }
                SetWindowHeight(95);
            }
            else
                SetWindowHeight(55);
        }
        static void SetWindowHeight(float height)
        {
            window.minSize = new Vector2(400, height);
            window.maxSize = new Vector2(400, height);
        }
        static void GetWindow()
        {
            if (window == null)
            {
                window = GetWindow<CreateCombatCharacterEditorWindow>();
                window.titleContent = new GUIContent("Combat");
                SetWindowHeight(55);
            }
        }
        object UndoField(object oldValue, object newValue)
        {
            if (newValue != null && oldValue != null && newValue.ToString() != oldValue.ToString())
            {
                Undo.RegisterCompleteObjectUndo(this, "Update Field");
            }
            return newValue;
        }

        void AddColliderToBone(Animator animator, HumanBodyBones bone, string objName = "")
        {
            var handler = animator.GetBoneTransform(bone).transform;
            var colliderName = string.IsNullOrEmpty(objName) ? bone.ToString() + "Collider" : objName;
            var weaponHandler = (GameObject)Resources.Load("CombatCollider");
            var obj = PrefabUtility.InstantiatePrefab(weaponHandler, handler) as GameObject;
            obj.name = colliderName;
            obj.transform.localPosition = Vector3.zero;
        }
#endif
    }
}
#endif
