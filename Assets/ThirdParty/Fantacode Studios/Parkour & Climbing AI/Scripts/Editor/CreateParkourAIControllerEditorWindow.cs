#if UNITY_EDITOR
using FS_Core;
using FS_ThirdPerson;
using UnityEditor;
using UnityEngine;

namespace FS_ParkourAI
{

    public class CreateParkourAIControllerEditorWindow : EditorWindow
    {
        public static CreateParkourAIControllerEditorWindow window;

        public GameObject model;
        public static GameObject followTarget;
        bool isHumanoid;
        bool validAvathar;
        bool hasAnimator;
        bool validModel;


        [MenuItem("Tools/Parkour AI/Create Parkour AI Character", false, 15)]
        public static void ShowWindow()
        {
            FSSystemsSetupEditorWindow.ShowWindow();
            FSSystemsSetup.ParkourAISystemSetup.selected = true;
            FSSystemsSetupEditorWindow.ChangeCharacterType(CharacterType.AI);
        }
        static void GetWIndow()
        {
            if (window == null)
            {
                window = GetWindow<CreateParkourAIControllerEditorWindow>("Parkour AI");
                SetWindowHeight(75);
                followTarget = FindAnyObjectByType<LocomotionController>()?.gameObject;
            }
        }


        private void OnGUI()
        {
            GetWIndow();
            GUILayout.Space(10);
            SetWarningAndErrors();
            model = (GameObject)UndoField(model, EditorGUILayout.ObjectField("Model", model, typeof(GameObject), true));
            followTarget = (GameObject)UndoField(followTarget, EditorGUILayout.ObjectField("Follow Target", followTarget, typeof(GameObject), true));
            GUILayout.Space(3f);
            if (GUILayout.Button("Create Parkour AI"))
                CreateCharacter();
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
                SetWindowHeight(115);
            }
            else
                SetWindowHeight(75);

        }
        static void SetWindowHeight(float height)
        {
            window.minSize = new Vector2(400, height);
            window.maxSize = new Vector2(400, height);
        }

        void CreateCharacter()
        {
            if (validModel)
            {

                var playerPrefab = (GameObject)Resources.Load("Parkour AI");
                var parkourAI = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

                var model = Instantiate(this.model, Vector3.zero, Quaternion.identity);
                model.transform.SetParent(parkourAI.transform);
                var animator = parkourAI.GetComponent<Animator>();
                if(animator.runtimeAnimatorController == null)
                    animator.runtimeAnimatorController = ParkourAIWelcomeWindow.SetAnimatorToParkourAIPrefab();
                animator.avatar = this.model.GetComponent<Animator>().avatar;
                parkourAI.name = playerPrefab.name;
                model.name = this.model.name;
                this.model = null;

                parkourAI.GetComponent<AIController>().player = followTarget;

                Undo.RegisterCreatedObjectUndo(parkourAI, "new character controller added");
                Undo.RegisterCreatedObjectUndo(model, "new character added");
                Selection.activeObject = parkourAI;
                SceneView sceneView = SceneView.lastActiveSceneView;
                sceneView.Focus();
                sceneView.LookAt(parkourAI.transform.position);



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
    }
}
#endif