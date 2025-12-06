using FS_CombatCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_CombatSystem
{
    public class SetMeleeCombatLayersForDemo : MonoBehaviour
    {
        [SerializeField] FighterCore player;
        [SerializeField] List<FighterCore> enemies;
        [SerializeField] List<FighterCore> playerAllies;

        private void Start()
        {
            player.gameObject.layer = LayerMask.NameToLayer("Player");
            player.targetLayer = LayerMask.GetMask("Enemy");
            foreach (var ally in playerAllies)
            {
                ally.gameObject.layer = LayerMask.NameToLayer("PlayerAlly");
                ally.targetLayer = LayerMask.GetMask("Enemy");
            }

            foreach (var enemy in enemies)
            {
                enemy.gameObject.layer = LayerMask.NameToLayer("Enemy");
                enemy.targetLayer = LayerMask.GetMask("Player", "PlayerAlly");
            }
        }
    }
}
