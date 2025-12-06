using FS_CombatCore;
using FS_Core;
using UnityEngine;
using UnityEngine.AI;

namespace FS_Shooter
{
    public class RegroupState : State<CombatAIController>, IAIState
    {
        private float stateStartTime;
        private float stateDuration;
        private CombatAIController combatAI;

        #region Inspector‐Exposed Fields (Regroup‐Specific)

        [Header("Regroup Settings")]
        [Tooltip("Minimum seconds to wait before transitioning back to Chase")]
        [SerializeField] private float minRegroupTime = 2f;

        [Tooltip("Maximum seconds to wait before transitioning back to Chase")]
        [SerializeField] private float maxRegroupTime = 4f;

        [Tooltip("Movement speed while regrouping")]
        [SerializeField] private float regroupSpeed = 2.5f;

        [Tooltip("Radius around the AI’s current position to pick a random regroup point")]
        [SerializeField] private float regroupRadius = 5f;

        #endregion

        // The actual chosen regroup target
        private Vector3 regroupTarget;

        public AIStates StateKey => AIStates.Regroup;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;

            // Choose random regroup time
            stateStartTime = Time.time;
            stateDuration = Random.Range(minRegroupTime, maxRegroupTime);

            // Set agent speed to the serialized regroupSpeed
            combatAI.NavAgent.speed = regroupSpeed;

            PickRegroupPoint();
            combatAI.NavAgent.SetDestination(regroupTarget);
        }

        public override void Execute()
        {
            bool timeUp = (Time.time - stateStartTime) >= stateDuration;
            bool arrived =
                combatAI.NavAgent.remainingDistance <=
                combatAI.NavAgent.stoppingDistance + 0.1f;

            if (timeUp || arrived)
            {
                combatAI.ChangeState(AIStates.Chase);
            }
        }

        public override void Exit()
        {
            // Nothing to clean up for RegroupState
        }

        #region Private Helpers (Regroup-Only)

        /// <summary>
        /// Picks a random point within a circle of radius regroupRadius around this AI.
        /// </summary>
        private void PickRegroupPoint()
        {
            Vector2 rnd = Random.insideUnitCircle * regroupRadius;
            if(combatAI.NavAgent.Raycast(combatAI.transform.position + new Vector3(rnd.x, 0f, rnd.y), out NavMeshHit hit))
            regroupTarget = hit.position;
            else
                regroupTarget = (combatAI.transform.position + new Vector3(rnd.x, 0f, rnd.y));
        }

        #endregion
    }
}
