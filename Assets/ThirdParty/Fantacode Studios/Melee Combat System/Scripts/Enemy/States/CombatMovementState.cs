using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using UnityEngine;
namespace FS_CombatSystem
{
    public enum AICombatStates { Idle, Chase, Circling }

    public class CombatMovementState : State<CombatAIController>, IAIState
    {

        [SerializeField] float adjustDistanceThreshold = 1f;
        [SerializeField] Vector2 attackAttemptTimeRange = new Vector3(2, 6);
        [SerializeField] Vector2 idleTimeRange = new Vector2(2, 5);
        [SerializeField] Vector2 circlingTimeRange = new Vector2(3, 6);
        [SerializeField] float rotateTowardsTargetSpeed = 150f;

        float circlingDistThreshold = 1.5f;

        float timer = 0f;

        int circlingDir = 1;
        bool slowChase = true;

        AICombatStates state;

        float attackAttemptTime = 0f;

        CombatAIController combatAI;

        public AIStates StateKey => AIStates.CombatMovement;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;

            combatAI.NavAgent.stoppingDistance = combatAI.Fighter.PreferredFightingRange;
            combatAI.CombatMovementTimer = 0f;

            combatAI.Animator.SetBool(AnimatorParameters.combatMode, true);

            state = AICombatStates.Idle;

            attackAttemptTime = Random.Range(attackAttemptTimeRange.x, attackAttemptTimeRange.y);
        }

        public override void Execute()
        {
            if (combatAI.Fighter.CurrentWeapon == null) return;
            if (combatAI.Fighter.Target == null || combatAI.Fighter.Target.Damagable.CurrentHealth <= 0)
            {
                combatAI.Fighter.Target = null;
                combatAI.ChangeState(AIStates.Idle);
                return;
            }

            if (combatAI.TimeSinceLastAttack >= attackAttemptTime && combatAI.DistanceToTarget <= combatAI.Fighter.MaxAttackRange)
            {
                if (MeleeAIManager.i.CanAttack(combatAI.Fighter.Target) && !combatAI.Fighter.Target.IsBeingAttacked)
                {
                    combatAI.ChangeState(AIStates.Attack);
                    return;
                }
            }

            if ((combatAI.DistanceToTarget > combatAI.Fighter.MaxAttackRange || combatAI.NavAgent.Raycast(combatAI.Fighter.Target.transform.position, out _)) && state != AICombatStates.Chase)
                StartChase();

            if (state == AICombatStates.Idle)
            {
                if (timer <= 0)
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        StartIdle();
                    }
                    else
                    {
                        StartCircling();
                    }
                }
                if (combatAI.Fighter.Target != null)
                {
                    var vecToTarget = (combatAI.transform.position - combatAI.Fighter.Target.transform.position);
                    vecToTarget.y = 0f;
                    vecToTarget.Normalize();
                    var targetRot = Quaternion.LookRotation(-vecToTarget);

                    if (Vector3.Angle(combatAI.transform.forward, -vecToTarget) > rotateTowardsTargetSpeed  * Time.deltaTime)
                        combatAI.transform.rotation = Quaternion.RotateTowards(combatAI.transform.rotation, targetRot, rotateTowardsTargetSpeed * Time.deltaTime);
                    else
                        combatAI.transform.rotation = targetRot;
                }

            }
            else if (state == AICombatStates.Chase)
            {
                if (combatAI.DistanceToTarget <= combatAI.Fighter.PreferredFightingRange + 0.03f)
                {
                    StartIdle();
                    return;
                }

                // If the target is too far, then run and chase fast
                if (slowChase)
                {
                    if (combatAI.DistanceToTarget >= combatAI.Fighter.MaxAttackRange + 5)
                    {
                        slowChase = false;
                        combatAI.NavAgent.speed = combatAI.RunSpeed;
                        combatAI.Animator.SetBool(AnimatorParameters.combatMode, false);
                    }
                }

                combatAI.NavAgent.SetDestination(combatAI.Fighter.Target.transform.position);
            }
            else if (state == AICombatStates.Circling)
            {
                if (timer <= 0 || combatAI.DistanceToTarget < circlingDistThreshold)
                {
                    StartIdle();
                    return;
                }

                var vecToTarget = combatAI.transform.position - combatAI.Fighter.Target.transform.position;
                var rotatedPos = Quaternion.Euler(0, combatAI.CombatModeSpeed * circlingDir * Time.deltaTime, 0) * vecToTarget;

                combatAI.NavAgent.Move((rotatedPos - vecToTarget).normalized * combatAI.CombatModeSpeed * Time.deltaTime);
                var rotatedPosXZ = rotatedPos;
                rotatedPosXZ.y = 0;
                combatAI.transform.rotation = Quaternion.LookRotation(-rotatedPosXZ);
            }

            if (timer > 0)
                timer -= Time.deltaTime;

            combatAI.CombatMovementTimer += Time.deltaTime;
        }

        void StartChase()
        {
            state = AICombatStates.Chase;
            slowChase = true;
            combatAI.NavAgent.speed = combatAI.CombatModeSpeed;
        }

        void StartIdle()
        {
            combatAI.Animator.SetBool(AnimatorParameters.combatMode, true);

            state = AICombatStates.Idle;
            timer = Random.Range(idleTimeRange.x, idleTimeRange.y);
        }

        void StartCircling()
        {
            combatAI.Animator.SetBool(AnimatorParameters.combatMode, true);

            state = AICombatStates.Circling;
            timer = Random.Range(circlingTimeRange.x, circlingTimeRange.y);

            circlingDir = Random.Range(0, 2) == 0 ? 1 : -1;
        }

        public override void Exit()
        {
            combatAI.CombatMovementTimer = 0f;

            if (combatAI.NavAgent != null && combatAI.NavAgent.isActiveAndEnabled)
                combatAI.NavAgent.ResetPath();
        }

        //private void OnGUI()
        //{
        //    var style = new GUIStyle();
        //    style.fontSize = 24;

        //    GUILayout.Label(state.ToString(), style);
        //}
    }
}