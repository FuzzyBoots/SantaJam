using FS_Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ThirdPerson
{

    public class EnemyDamagable : MonoBehaviour
    {
        //    #region Damage
        //    public float CurrentHealth { get; set; } = 40;
        //    public float DamageMultiplier { get; set; } = 1;
        //    public Action<Vector3, float> OnHit { get; set; }
        //    public Damagable Parent => this;
        //    public bool CanTakeHit { get; set; }

        //    [field: SerializeField] public float MaxHealth { get; set; } = 40;
        //    public Action OnDamageUpdated { get; set; }

        //    public void TakeDamage(Vector3 dir, float damage)
        //    {
        //        var armorHandler = GetComponent<ArmorHandler>();

        //        if (armorHandler != null)
        //        {
        //            float reducedDamage = Mathf.Clamp(armorHandler.TotalDefence, 0, damage / 2);
        //            // Ensure damage is not negative
        //            damage -= Mathf.Max(reducedDamage, 0);
        //        }

        //        UpadteHealth(-damage);
        //    }

        //    public void UpadteHealth(float hpRestore)
        //    {
        //        CurrentHealth = Mathf.Clamp(CurrentHealth + hpRestore, 0, MaxHealth);
        //        OnDamageUpdated?.Invoke();
        //    }

        //    private void OnEnable()
        //    {
        //        OnHit += TakeDamage;
        //        CurrentHealth = MaxHealth;
        //    }

        //    private void OnDisable()
        //    {
        //        OnHit -= TakeDamage;
        //    }

        //    #endregion
    }
}
