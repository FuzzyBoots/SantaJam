using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FS_CoverSystem
{
#if UNITY_EDITOR
    [CanEditMultipleObjects]
#endif
    public class CoverHandler : MonoBehaviour
    {
        public float colliderHeightInCover = 1.2f;
        public Vector3 colliderCenterInCover = new Vector3(0, 0.6f, 0);

        CharacterController characterController;
        Animator animator;
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            controllerOriginalHeight = characterController.height;
            controllerOriginalCenter = characterController.center;
        }

        public bool InCover { get; private set; }

        private void Update()
        {
            if (InCover) { 
            AdjustControllerForCover();
            }
        }

        public void GoToCover()
        {
            InCover = true;

            animator.SetBool(AnimatorParameters.coverMode, true);
            animator.CrossFade(AnimationNames.LocomotionToCover, .2f);
            //AdjustControllerForCover();
        }

        public void GoOutOfCover()
        {
            InCover = false;
            characterController.SimpleMove(Vector3.zero);
            animator.SetBool(AnimatorParameters.coverMode, false);
        }

        Vector3 controllerOriginalCenter;
        float controllerOriginalHeight;
        void AdjustControllerForCover()
        {
            if (characterController == null) return;

            characterController.height = Mathf.Lerp(controllerOriginalHeight, colliderHeightInCover, animator.GetFloat("CoverType"));
            characterController.center = Vector3.Lerp(controllerOriginalCenter, colliderCenterInCover, animator.GetFloat("CoverType"));
        }

        void ResetControllerAdjustments()
        {
            if (characterController == null) return;

            characterController.height = controllerOriginalHeight;
            characterController.center = controllerOriginalCenter;
        }
    }
}
