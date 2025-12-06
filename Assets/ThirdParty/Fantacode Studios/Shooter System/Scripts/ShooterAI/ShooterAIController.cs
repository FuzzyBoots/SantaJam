using FS_CombatCore;
using FS_ShooterSystem;
using FS_ThirdPerson;
using FS_Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace FS_Shooter
{
    public class ShooterAIController : MonoBehaviour
    {
        public enum ShooterStates
        {
            Waypoint,
            Chase,
            Shoot,
            Cover,
            Regroup
        }

        #region Inspector‐Exposed Fields (TRULY SHARED)

        public ShooterFighter shooter { get; set; }
        private FighterCore fighterCore;
        public Animator animator { get; private set; }
        public NavMeshAgent navMeshAgent { get; private set; }
        [SerializeField] public PlayerController playerController;

        [Header("Global Settings")]
        [Tooltip("Used by multiple states for line‐of‐sight checks")]
        [SerializeField] public LayerMask obstacleMask;

        [Tooltip("Distance at which AI enters Shoot or Chase or Waypoint logic")]
        [SerializeField] public float preferredMaxRange = 15f;
        [SerializeField] public float preferredMinRange = 4f;
        [SerializeField] public float preferredRange = 8f;

        [SerializeField] public float animationSpeed = 3f;
        #endregion

        // The one StateMachine<ShooterAIController> instance
        [HideInInspector] public StateMachine<ShooterAIController> stateMachine;
        public ShooterStates currentState;

        // Dictionary from enum → actual State component
        private Dictionary<ShooterStates, State<ShooterAIController>> stateDict;

        private void Start()
        {
            // Cache components if not assigned
            shooter = GetComponent<ShooterFighter>();
            fighterCore = GetComponent<FighterCore>();
            animator = GetComponent<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            playerController = FindObjectOfType<PlayerController>();

            fighterCore.OnDeath += OnDeath;

            // 1) Build the state machine
            stateMachine = new StateMachine<ShooterAIController>(this);

            // 2) Populate stateDict by grabbing each State component
            //stateDict = new Dictionary<ShooterStates, State<ShooterAIController>>
            //{
            //    [ShooterStates.Chase] = GetComponent<ChaseState>(),
            //    [ShooterStates.Shoot] = GetComponent<ShootState>(),
            //    [ShooterStates.Cover] = GetComponent<CoverState>(),
            //    [ShooterStates.Regroup] = GetComponent<RegroupState>()
            //};

            // 3) Immediately enter WaypointState
            ChangeState(ShooterStates.Waypoint);
        }

        void OnDeath()
        {
            //fighterCore.SetRagdollState(true); 
            fighterCore.StopMovement = true;
        }

        private void Update()
        {
            if (fighterCore.StopMovement) { navMeshAgent.stoppingDistance = 1000f; return; }

            // If currently taking a hit or blocked hit → freeze & force all AIs into Chase
            if (shooter.Fighter.Action == FighterAction.TakingHit ||
                shooter.Fighter.Action == FighterAction.TakingBlockedHit)
            {
                navMeshAgent.speed = 0f;

                // Force every other AI into Chase
                foreach (var ai in FindObjectsOfType<ShooterAIController>())
                {
                    if (ai.currentState == ShooterStates.Waypoint)
                        ai.ChangeState(ShooterStates.Chase);
                }

                // Force self into Chase
                ChangeState(ShooterStates.Chase);
                return;
            }

            // If busy (e.g., reloading / playing some “hit” animation), skip AI logic entirely
            if (shooter.Fighter.IsBusy)
                return;

            // Otherwise, update shared aiming/animation, then let the current state run
            //UpdateAimingTargets();
            UpdateAnimatorMovement();
            stateMachine.Execute();
        }
        public void ChangeState(ShooterStates newState)
        {
            currentState = newState;
            stateMachine.ChangeState(stateDict[newState]);
        }

        #region Shared Helper Methods (remain in the controller)

        /// <summary>
        /// Update the weapon’s TargetAimPoint. 
        /// - If the current state is ShootState, aim at the player.
        /// - Otherwise, aim “straight ahead” (so the weapon still has a valid forward‐direction). 
        /// </summary>
        

        /// <summary>
        /// Sets Animator parameters “moveAmount” and “strafeAmount” based on navMeshAgent.velocity.
        /// Also resets “CoverMode” Boolean each frame.
        /// </summary>
        public void UpdateAnimatorMovement()
        {
            //animator.SetBool(AnimatorParameters.CoverMode, false);
            //navMeshAgent.speed = 3.5f;

            Vector3 v = navMeshAgent.velocity;
            v.y = 0f;

            animator.SetFloat(
                "moveAmount",
                Vector3.Dot(v, transform.forward) / animationSpeed,
                0.2f,
                Time.deltaTime
            );
            animator.SetFloat(
                "strafeAmount",
                Vector3.Dot(v, transform.right) / animationSpeed,
                0.2f,
                Time.deltaTime
            );
        }

        #endregion
    }
}
