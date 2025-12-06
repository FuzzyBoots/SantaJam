using FS_CombatCore;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_CombatCore
{

    public class CombatAIManager : MonoBehaviour
    {
        

        FighterCore player;
        public FighterCore Player {
            get => player;
            set { player = value; }
        }

        public static CombatAIManager i { get; private set; }
        private void Awake()
        {
            i = this;

            var playerController = GetComponentInChildren<PlayerController>();
            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();

            player = playerController.GetComponent<FighterCore>();
            CombatAILookup.Init();
        }

        public List<CombatAIController> MeleeAIList { get; private set; } = new List<CombatAIController>();
        public List<CombatAIController> RangedAIList { get; private set; } = new List<CombatAIController>();

        public void RegisterCombatAI(CombatAIController combatAI)
        {
            CombatAILookup.Register(combatAI);

            if (combatAI.AICombatType == AICombatType.Melee)
                MeleeAIList.Add(combatAI);
            else
                RangedAIList.Add(combatAI);
        }

        public void RemoveCombatAI(CombatAIController combatAI)
        {
            CombatAILookup.Remove(combatAI.Fighter);

            if (combatAI.AICombatType == AICombatType.Melee)
                MeleeAIList.Remove(combatAI);
            else
                RangedAIList.Add(combatAI);
        }
    }
}
