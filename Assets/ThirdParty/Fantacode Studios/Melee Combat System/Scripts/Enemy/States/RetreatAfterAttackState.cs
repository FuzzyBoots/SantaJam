using FS_CombatCore;
using FS_Core;
using UnityEngine;

namespace FS_CombatSystem
{
    public class RetreatAfterAttackState : State<CombatAIController>, IAIState
    {
        [SerializeField] float backwardWalkSpeed = 1.5f;
        [SerializeField] float distanceToRetreat = 3f;

        CombatAIController combatAI;
        Vector3 targetPos;

        public AIStates StateKey => AIStates.RetreatAfterAttack;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;
            targetPos = combatAI.Fighter.Target.transform.position;
        }

        public override void Execute()
        {
            if (combatAI.Fighter.CurrentWeapon == null) return;
            if (Vector3.Distance(combatAI.transform.position, targetPos) >= distanceToRetreat)
            {
                combatAI.ChangeState(AIStates.CombatMovement);
                return;
            }

            var vecToTarget = combatAI.Fighter.Target.transform.position - combatAI.transform.position;
            combatAI.NavAgent.Move(-vecToTarget.normalized * backwardWalkSpeed * Time.deltaTime);

            vecToTarget.y = 0f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vecToTarget), 500 * Time.deltaTime);
        }
    }
}