
#if gameCreator2
using GameCreator.Runtime.Characters;
#endif
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using FS_ThirdPerson;

namespace FS_ParkourAI
{
    public class GC2AIManager : MonoBehaviour
#if gameCreator2
        , IAICharacter
#endif
    {
#if gameCreator2
        Character character;

        NavMeshAgent navMeshAgent;
        Animator animator;
        Animator gc2Animator;
        ParkourAI parkourAI;
        AIController aiController;

        public float Gravity => -20f;

        public Animator Animator 
        {
            get
            {
                return animator == null ? GetComponentInParent<Character>().GetComponent<Animator>() : animator;
            }
            set
            {
                animator = value;
            }
        }
        public bool UseRootMotion { get; set; } = false;
        public IEnumerator HandleVerticalJump() { yield break; }

        public Vector3 MoveDir => navMeshAgent.desiredVelocity;

        public bool IsGrounded => false;

        public bool PreventParkourAction => false;

        public bool WaitToStartSystem { get; set; } = false;

        public NavMeshAgent NavMeshAgent
        {
            get
            {
                return navMeshAgent == null ? character.GetComponent<NavMeshAgent>() : navMeshAgent;
            }
            set
            {
                navMeshAgent = value;
            }
        }

        public bool IsBusy { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        private void Awake()
        {
            character = GetComponentInParent<Character>();
            navMeshAgent = character.GetComponent<NavMeshAgent>();
            animator = GetComponentInParent<Animator>();
            gc2Animator = character.Animim.Animator;
            parkourAI = GetComponent<ParkourAI>();
            aiController = GetComponent<AIController>();
            this.transform.localPosition = Vector3.zero;

            if (character.gameObject.layer == LayerMask.NameToLayer("Default"))
                character.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        IEnumerator LerpParameter(Animator animator, string parameter, float wait)
        {
            yield return new WaitForSeconds(wait);
            if (parkourAI.ControlledByParkour)
            {
                animator.SetFloat(parameter, 0);

            }
        }

        IEnumerator OnEndParkour()
        {
            navMeshAgent.Warp(character.transform.position);
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = true;
            navMeshAgent.isStopped = false;
            aiController.isFalling = false;
            character.Animim.OnStartup(character);

            character.transform.parent = null;
            transform.parent = animator.transform;
            transform.localPosition = Vector3.zero;

            var c = character.GetComponent<Collider>();
            c.enabled = true;
            animator.SetBool("IsGrounded", true);

            character.enabled = true;
            
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
            {
                animator.SetFloat("Movement", gc2Animator.GetFloat("Movement"));
                animator.SetFloat("Speed-X", gc2Animator.GetFloat("Speed-X"));
                animator.SetFloat("Speed-Y", gc2Animator.GetFloat("Speed-Y"));
                animator.SetFloat("Speed-Z", gc2Animator.GetFloat("Speed-Z"));
                animator.SetFloat("Pivot", gc2Animator.GetFloat("Pivot"));
                animator.SetFloat("Stand", gc2Animator.GetFloat("Stand"));
                yield return null;
            }
            gc2Animator.enabled = true;
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
            {
                var normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
                gc2Animator.Play("Locomotion", 0, normalizedTime);
                gc2Animator.Update(0);
            }
            animator.enabled = false;
            character.States.ChangeWeight(5, 1, 0.25f);
        }

        public void OnStartAction(bool unEquip = true, bool stopMovement = true, bool itemBecomeUnUsable = false)
        {
            if (!character.enabled)
                return;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            var col = character.GetComponent<Collider>();
            col.enabled = false;
            character.Motion.GravityDownwards = 0f;

            this.transform.parent = null;
            character.transform.parent = this.transform;
            character.enabled = false;
            StartCoroutine(LerpParameter(gc2Animator, "Grounded", 0.05f));
            character.Motion.StandLevel.Current = 1;
            animator.enabled = true;
            animator.SetBool("IsGrounded", false);


            animator.SetFloat("Movement", gc2Animator.GetFloat("Movement"));
            animator.SetFloat("Speed-X", gc2Animator.GetFloat("Speed-X"));
            animator.SetFloat("Speed-Y", gc2Animator.GetFloat("Speed-Y"));
            animator.SetFloat("Speed-Z", gc2Animator.GetFloat("Speed-Z"));
            animator.SetFloat("Pivot", gc2Animator.GetFloat("Pivot"));
            animator.SetFloat("Stand", gc2Animator.GetFloat("Stand"));

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
            {
                var normalizedTime = gc2Animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
                animator.Play("Locomotion", 0, normalizedTime);
                animator.Update(0);
            }
            gc2Animator.enabled = false;

            character.States.ChangeWeight(5, 0, 0f);
        }

        public void OnEndAction()
        {
            if (character.enabled)
                return;
            StartCoroutine(OnEndParkour());
        }
#endif
    }

#if UNITY_EDITOR && gameCreator2
    [CustomEditor(typeof(GC2AIManager))]
    public class GC2AIManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Integrate"))
            {
                var manager = target as GC2AIManager;

                var animatorObject = manager.transform.parent.gameObject;
                if(animatorObject.GetComponent<Animator>() != null)
                    GameObject.DestroyImmediate(animatorObject.GetComponent<Animator>());


                var gc2Animator = animatorObject.GetComponentInChildren<Animator>();
                var parkourAnimator = (Animator)Undo.AddComponent(animatorObject, typeof(Animator));
                EditorUtility.CopySerialized(gc2Animator, parkourAnimator);
                parkourAnimator.enabled = false;
                if(animatorObject.GetComponent<AIRootmotionController>() == null)
                    Undo.AddComponent(animatorObject, typeof(AIRootmotionController));
                var character = manager.GetComponentInParent<Character>();

                manager.transform.localPosition = Vector3.zero;
            }
        }
    }
#endif
}
