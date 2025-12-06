using FS_CombatCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_CombatSystem
{
    public enum EnemySelectionType { TimeWaited, Distance, DistanceAndTimeWaited }

    public class MeleeAIManager : MonoBehaviour
    {
        [SerializeField] bool isFreeFlowCombat = true;
        [SerializeField] Vector2 timeRangeBetweenAttacks = new Vector2(1, 4);
        [SerializeField] EnemySelectionType criteriaToSelectEnemyToAttack = EnemySelectionType.DistanceAndTimeWaited;

        List<CombatAIController> enemiesInRange = new List<CombatAIController>();
        float notAttackingTimer = 2;

        public static MeleeAIManager i { get; private set; }
        private void Awake()
        {
            i = this;
        }

        public bool CanAttack(FighterCore target)
        {
            if (target == CombatAIManager.i.Player && isFreeFlowCombat) return false;

            return true;
        }

        float timer = 0f;
        private void Update()
        {
            if (!isFreeFlowCombat) return;

            // Find enemies in range
            enemiesInRange.Clear();
            foreach (var enemy in CombatAIManager.i.MeleeAIList)
            {
                if (enemy.Fighter.Target == CombatAIManager.i.Player && !enemy.Fighter.IsDead)
                    enemiesInRange.Add(enemy);
            }

            if (enemiesInRange.Count == 0) return;

            if (notAttackingTimer > 0)
                notAttackingTimer -= Time.deltaTime;

            if (!enemiesInRange.Any(e => e.IsInState(AIStates.Attack)))
            {
                if (notAttackingTimer <= 0)
                {
                    // Attack the player
                    var attackingEnemy = SelectEnemyForAttack(enemiesInRange);

                    if (attackingEnemy != null && !attackingEnemy.iAICharacter.IsBusy)
                    {
                        attackingEnemy.ChangeState(AIStates.Attack);
                        notAttackingTimer = Random.Range(timeRangeBetweenAttacks.x, timeRangeBetweenAttacks.y);
                    }
                }
            }

            if (timer >= 0.1f)
            {
                timer = 0f;

                //var closestEnemy = GetEnemyToTarget(player.GetTargetingDir());
                //if (closestEnemy != null && closestEnemy != player.TargetEnemy)
                //{
                //    player.TargetEnemy = closestEnemy;
                //}
            }

            timer += Time.deltaTime;
        }

        CombatAIController SelectEnemyForAttack(List<CombatAIController> enemiesInRange)
        {
            foreach (var e in enemiesInRange)
            {
                bool check = e.Fighter.Target != null && e.IsInState(AIStates.CombatMovement) && e.DistanceToTarget <= e.Fighter.MaxAttackRange
                && e.LineOfSightCheck(e.Fighter.Target) && !e.Fighter.Target.IsInSyncedAnimation && !e.Fighter.IsKnockedDown;
            }

            var possibleEnemies = enemiesInRange.Where(e => e.Fighter.Target != null && e.IsInState(AIStates.CombatMovement) && e.DistanceToTarget <= e.Fighter.MaxAttackRange
                && e.LineOfSightCheck(e.Fighter.Target) && !e.Fighter.Target.IsInSyncedAnimation && !e.Fighter.IsKnockedDown).ToList();

            if (criteriaToSelectEnemyToAttack == EnemySelectionType.TimeWaited)
                return possibleEnemies.OrderByDescending(e => e.CombatMovementTimer).FirstOrDefault();
            else if (criteriaToSelectEnemyToAttack == EnemySelectionType.Distance)
                possibleEnemies.OrderBy(e => e.DistanceToTarget).FirstOrDefault();
            else if (criteriaToSelectEnemyToAttack == EnemySelectionType.DistanceAndTimeWaited)
                return possibleEnemies.Select(e => new
                {
                    Enemy = e,
                    Weight = (e.CombatMovementTimer * 5) / (e.DistanceToTarget * 10)
                }).OrderByDescending(e => e.Weight).FirstOrDefault()?.Enemy;

            return null;
        }

        public CombatAIController GetAttackingEnemy(List<CombatAIController> enemiesInRange)
        {
            return enemiesInRange.FirstOrDefault(e => e.IsInState(AIStates.Attack));
        }

        //private void OnGUI()
        //{
        //    var style = new GUIStyle();
        //    style.fontSize = 24;

        //    GUILayout.Space(30);
        //    GUILayout.Label("Not attacked timer " + notAttackingTimer, style);
        //}
    }

}
