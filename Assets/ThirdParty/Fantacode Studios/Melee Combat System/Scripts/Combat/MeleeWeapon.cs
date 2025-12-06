using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace FS_CombatSystem
{
    [CustomIcon(FolderPath.DataIcons + "Weapon Icon.png")]
    [Icon(FolderPath.DataIcons + "Weapon Icon.png")]
    [CreateAssetMenu(menuName = "Melee Combat System/Create Weapon")]
    [MovedFrom("WeaponData")]
    public class MeleeWeapon : CombatWeapon
    {
        [Tooltip("List of attack datas, each containing details about an attack and its conditions.")]
        [SerializeField] List<AttackContainer> attacks;

        [Tooltip("List of attack data that is considered a heavy attack, each containing details about an attack and its conditions.")]
        [SerializeField] List<AttackContainer> heavyAttacks;

        [Tooltip("List of attack data that is considered a special attacks, each containing details about an attack and its conditions.")]
        [SerializeField] List<AttackContainer> specialAttacks;

        [Tooltip("Indicates if the weapon can perform counterattacks.")]
        [SerializeField] bool canCounter = true;

        [Tooltip("If true, the fighter will play a taunt action if the counter input is pressed while the enemy is not attacking. This can be used to prevent the misuse of the counter input.")]
        [SerializeField] bool playActionIfCounterMisused = false;
        [Tooltip("Animation clip of the action to play if the counter is pressed while the enemy is not attacking.")]
        [SerializeField] AnimationClip counterMisusedAction;

        [Tooltip("The minimum distance required for the weapon to have an effective attack.")]
        [SerializeField] float minAttackDistance = 0;

        [Tooltip("Indicates if root motion should be used for combat movement")]
        [SerializeField] bool useRootmotion;

        public void InIt()
        {
            foreach (var attack in attacks)
                attack.AttackSlots.ForEach(a => a.Container = attack);

            foreach (var attack in heavyAttacks)
                attack.AttackSlots.ForEach(a => a.Container = attack);

            foreach (var attack in specialAttacks)
                attack.AttackSlots.ForEach(a => a.Container = attack);
        }

        public List<AttackContainer> Attacks => attacks;
        public List<AttackContainer> HeavyAttacks => heavyAttacks;
        public List<AttackContainer> SpecialAttacks => specialAttacks;
        public bool CanCounter => canCounter;
        public bool PlayActionIfCounterMisused => playActionIfCounterMisused;
        public AnimationClip CounterMisusedAction => counterMisusedAction;
        public float MinAttackDistance => minAttackDistance;
        public bool UseRootmotion => useRootmotion;
        public override void SetCategory()
        {
            category = Resources.Load<ItemCategory>("Category/Melee Weapon");
        }
    }
}