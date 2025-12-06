using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using UnityEngine;

namespace FS_CombatCore
{
    public class GettingHitState : State<CombatAIController>, IAIState
    {
        [SerializeField] float stunnTime = 1f;

        CombatAIController combatAI;

        public AIStates StateKey => AIStates.GettingHit;

        public override void Enter(CombatAIController owner)
        {
            StopAllCoroutines();

            combatAI = owner;
            combatAI.Fighter.OnHitComplete += HitComplete;

            if (!combatAI.Animator.GetBool(AnimatorParameters.combatMode))
                combatAI.Animator.SetBool(AnimatorParameters.combatMode, true);
        }

        void HitComplete()
        {
            StartCoroutine(GoToCombatState());
            combatAI.Fighter.OnHitComplete -= HitComplete;
        }

        IEnumerator GoToCombatState()
        {
            yield return new WaitForSeconds(stunnTime);

            if (!combatAI.IsInState(AIStates.Dead))
            {
                if(combatAI.AICombatType == AICombatType.Melee)
                    combatAI.ChangeState(AIStates.CombatMovement);
                else if(combatAI.AICombatType == AICombatType.Ranged)
                    combatAI.ChangeState(AIStates.Chase);
            }
        }
    }
}