using FS_ThirdPerson;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FS_Core
{
    public class AIManager : MonoBehaviour, IAICharacter
    {
        NavMeshAgent navMeshAgent;
        Animator animator;
        //AIController aiController;
        ItemEquipper itemEquipper;
        EquippableItem currentEquippedItem;
        Damagable damagable;

        public float Gravity => -20f;

        public Animator Animator { get => animator == null ? GetComponent<Animator>() : animator; set => animator = value; }
        public bool UseRootMotion { get; set; } = false;

        public Vector3 MoveDir => navMeshAgent.desiredVelocity;

        public bool IsGrounded => false;
        public bool IsBusy { get; set; }

        public bool PreventParkourAction => false;

        public NavMeshAgent NavMeshAgent
        {
            get
            {
                return navMeshAgent == null ? GetComponent<NavMeshAgent>() : navMeshAgent;
            }
            set
            {
                navMeshAgent = value;
            }
        }

        void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.autoTraverseOffMeshLink = false;

            if (animator == null)
                animator = GetComponent<Animator>();
            //aiController = GetComponent<AIController>();
            itemEquipper = GetComponent<ItemEquipper>();
            damagable = GetComponent<Damagable>();
        }

        private void Start()
        {
            damagable.OnDead += () => { navMeshAgent.updatePosition = false; };
        }
        public bool WaitToStartSystem { get; set; } = false;

        public void OnStartAction(bool unEquip = true, bool stopMovement = true, bool itemBecomeUnUsable = false)
        {
            navMeshAgent.updatePosition = !stopMovement;
            navMeshAgent.updateRotation = !stopMovement;
            IsBusy = true;
            if (unEquip && itemEquipper != null)
            {
                if (itemEquipper.EquippedItem != null && itemEquipper.EquippedItem.unEquipDuringActions) 
                {
                    currentEquippedItem = itemEquipper.EquippedItem;
                    itemEquipper.PreventItemSwitching = false;
                    itemEquipper.UnEquipItem(false);
                    itemEquipper.PreventItemSwitching = true;
                }
                else if (itemEquipper.EquippedItem != null)
                    itemEquipper.StopIdleAnimation();
            }
            else if (itemBecomeUnUsable && itemEquipper != null)
            {
                itemEquipper.StopIdleAnimation();
            }
        }


        public void OnEndAction()
        {
            navMeshAgent.Warp(transform.position);
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = true;
            if (navMeshAgent.enabled)
                navMeshAgent.isStopped = false;
            //aiController.isFalling = false;
            animator.SetBool("IsGrounded", true);
            if (itemEquipper != null)
            {
                if (itemEquipper.IsCurrentItemUnusable)
                    itemEquipper.ResumeIdleAnimation();
                else if (currentEquippedItem != null)
                {
                    itemEquipper.PreventItemSwitching = false;
                    itemEquipper.EquipItem(currentEquippedItem, false);
                    currentEquippedItem = null;
                }
            }
            StartCoroutine(AsyncUtil.RunAfterFrames(1, () => IsBusy = false));
        }
    }
}
