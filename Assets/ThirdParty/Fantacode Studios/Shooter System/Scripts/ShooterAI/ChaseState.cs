using FS_CombatCore;
using FS_Core;
using UnityEngine;

namespace FS_Shooter
{
    public class ChaseState : State<CombatAIController>, IAIState
    {
        private CombatAIController combatAI;

        #region Inspector‐Exposed Fields (Chase‐Specific)

        [Header("Chase Settings")]
        [Tooltip("Speed at which the AI chases the player")]
        [SerializeField] private float chaseSpeed = 3.5f;

        #endregion

        public AIStates StateKey => AIStates.Chase;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;

            if (combatAI.GetMovementRange() <= 1f)
                combatAI.ChangeState(AIStates.Shoot);

            // Set agent speed to the serialized chaseSpeed
            combatAI.NavAgent.speed = chaseSpeed;
        }

        public override void Execute()
        {
            if (combatAI.Fighter.Target == null)
            {
                combatAI.ChangeState(AIStates.Idle);
                return;
            }

            // 1) Always set nav destination to player’s current position
            combatAI.NavAgent.SetDestination(combatAI.Fighter.Target.transform.position);

            // 2) If we get within preferredRange, switch to ShootState
            if (combatAI.DistanceToTarget < combatAI.Fighter.PreferredFightingRange || combatAI.IsOutsideMovementRange())
                combatAI.ChangeState(AIStates.Shoot);
        }

        public override void Exit()
        {
            // Nothing specific to clean up for Chase
        }
    }
}
