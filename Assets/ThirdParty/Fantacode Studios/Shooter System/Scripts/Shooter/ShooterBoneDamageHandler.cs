using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FS_ShooterSystem
{

    public class ShooterBoneDamageHandler : MonoBehaviour
    {
        public float damageMultiplier = 1;
        Damagable parentDamagable;

        private void Awake()
        {
            var fighterCore = GetComponentInParent<FighterCore>();
            parentDamagable = fighterCore.GetComponent<Damagable>();

            gameObject.layer = LayerMask.NameToLayer("HitBone");
        }

        public Damagable ParentDamagable => parentDamagable;
    }
}