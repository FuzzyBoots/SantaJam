using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FS_CombatSystem
{
    public class MeleeCombatSettings : MonoBehaviour
    {
        [SerializeField] bool onlyCounterWhileBlocking = false;
        [SerializeField] bool onlyCounterFirstAttackOfCombo = true;
        [SerializeField] bool sameInputForAttackAndCounter = false;
        [SerializeField] float holdTimeForChargedAttacks = 0.2f;
        [SerializeField] float verticalLimitForAttacks = 1f;



        public static MeleeCombatSettings i { get; private set; }
        private void Awake()
        {
            i = this;
        }

        public bool OnlyCounterWhileBlocking => onlyCounterWhileBlocking;
        public bool OnlyCounterFirstAttackOfCombo => onlyCounterFirstAttackOfCombo;
        public bool SameInputForAttackAndCounter => sameInputForAttackAndCounter;
        public float HoldTimeForChargedAttacks => holdTimeForChargedAttacks;

        public float VerticalLimitForAttacks => verticalLimitForAttacks;
    }
}
