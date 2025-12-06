using FS_CombatCore;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_ShooterSystem
{

    public class ShooterAIManager : MonoBehaviour
    {
        [SerializeField] bool shouldFlank = true;
        [SerializeField] float timeToStartFlanking = 10f;

        float timeInCover = 0f;

        FighterCore player;
        List<CombatAIController> enemiesInRange = new List<CombatAIController>();

        private void Start()
        {
            player = CombatAIManager.i.Player;
        }


        private void Update()
        {
            if (!shouldFlank) return;

            bool isPlayerInCover = player.animator.GetBool(AnimatorParameters.coverMode);

            // Find enemies in range
            enemiesInRange.Clear();
            foreach (var combatAI in CombatAIManager.i.RangedAIList)
            {
                if (combatAI.Fighter.Target == player && !combatAI.Fighter.IsDead)
                    enemiesInRange.Add(combatAI);
            }

            if (enemiesInRange.Count == 0) return;

            if (isPlayerInCover)
            {
                timeInCover += Time.deltaTime;
            }
            else
            {
                timeInCover = 0f;
            }

            // when player goes to cover, start incrementing timeInCover and reset it when player comes out of cover

            if (timeInCover > timeToStartFlanking)
            {
                if (!enemiesInRange.Any(e => e.IsInState(AIStates.Flank))) 
                {
                    var enemyToFlank = SelectEnemyToFlank(enemiesInRange);
                    enemyToFlank?.ChangeState(AIStates.Flank);
                }
            }
        }

        CombatAIController SelectEnemyToFlank(List<CombatAIController> enemies)
        {
            return enemies.Where(e => e.stateDict.ContainsKey(AIStates.Flank) && !player.IsBusy && e.DistanceToTarget < e.GetMovementRange()).OrderBy(e => e.DistanceToTarget).FirstOrDefault();
        }
    }
}
