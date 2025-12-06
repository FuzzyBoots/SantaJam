using FS_CombatCore;
using FS_Core;
using System.Collections;
using UnityEngine;

namespace FS_CombatSystem
{

    public class AttackState : State<CombatAIController>, IAIState
    {
        bool isAttacking;

        CombatAIController combatAI;
        MeleeFighter meleeFighter;

        public AIStates StateKey => AIStates.Attack;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;
            if (meleeFighter == null)
                meleeFighter = combatAI.GetComponent<MeleeFighter>();
        }

        public override void Execute()
        {
            if (isAttacking || combatAI.Fighter.CurrentWeapon == null) return;

            if (combatAI.Fighter.IsBlocking)
                combatAI.Fighter.IsBlocking = false;

            StartCoroutine(Attack());
        }

        IEnumerator Attack()
        {
            isAttacking = true;

            meleeFighter.TryToAttack(combatAI.Fighter.Target);
            int comboCount = meleeFighter.CurrAttacksList.Count;
            for (int i = 1; i < comboCount; i++)
            {
                while (combatAI.Fighter.Action == FighterAction.Attacking && !meleeFighter.IsInCounterWindow()) yield return null;
                if (combatAI.Fighter.Action != FighterAction.Attacking)
                    break;
                
                meleeFighter.TryToAttack(combatAI.Fighter.Target);
                yield return null;
            }
            
            yield return new WaitUntil(() => meleeFighter.AttackState == AttackStates.Idle);
            
            isAttacking = false;

            if (combatAI.IsInState(AIStates.Attack))
                combatAI.ChangeState(AIStates.CombatMovement);
        }

        public override void Exit()
        {
            //enemy.NavAgent.ResetPath();
        }
    }
}