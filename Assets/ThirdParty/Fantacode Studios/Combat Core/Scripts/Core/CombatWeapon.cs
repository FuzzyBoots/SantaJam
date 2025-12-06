using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_CombatCore
{
    public class CombatWeapon : EquippableItem
    {
        [Tooltip("Data for reactions associated with the weapon.")]
        [SerializeField] ReactionsData reactionData;

        [Tooltip("Indicates if the weapon can block attacks.")]
        [SerializeField] bool canBlock;

        [Tooltip("The animation clip used for blocking with the attacks.")]
        [SerializeField] AnimationClip blocking;

        [Tooltip("The percentage of damage taken when getting hit while blocking.")]
        [Range(0, 100)]
        [SerializeField] float blockedDamage = 25f;

        [Tooltip("Data for reactions when blocking with the weapon.")]
        [SerializeField] ReactionsData blockReactionData;

        [Tooltip("Indicates if the locomotion movement speed should be overridden.")]
        [SerializeField] bool overrideMoveSpeed;

        [Tooltip("Movement speed while the character is in combat mode(Locked into a target).")]
        [SerializeField] float combatMoveSpeed = 2f;

        [Tooltip("Indicates if the fighter can dodge.")]
        [SerializeField] bool overrideDodge;
        [SerializeField] DodgeData dodgeData;

        [Tooltip("Indicates if the fighter can roll.")]
        [SerializeField] bool overrideRoll;
        [SerializeField] DodgeData rollData;

        public ReactionsData ReactionData => reactionData;
        public bool CanBlock => canBlock;
        public AnimationClip Blocking => blocking;
        public float BlockedDamage => blockedDamage;
        public ReactionsData BlockReactionData => blockReactionData;
        public bool OverrideMoveSpeed => overrideMoveSpeed;
        public float CombatMoveSpeed => combatMoveSpeed;
        public bool OverrideDodge => overrideDodge;
        public DodgeData DodgeData => dodgeData;
        public bool OverrideRoll => overrideRoll;
        public DodgeData RollData => rollData;
    }
}
