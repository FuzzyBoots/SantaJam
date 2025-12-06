using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace FS_Core
{
    public enum HitType { Any, Melee, Ranged };

    public class Damagable : MonoBehaviour, ISavable
    {
        AnimGraph animGraph;

        [field: SerializeField] public virtual float MaxHealth { get; set; } = 100;
        [field: SerializeField] public virtual float MaxBreath { get; set; } = 100;
        [SerializeField] HitType canBeDamagedBy;

        public virtual float CurrentHealth { get; set; }
        public virtual float CurrentBreath { get; set; }
        public event Action OnHealthUpdated;
        public event Action OnBreathUpdating;
        public event Action OnDead;
        public UnityEvent OnDeadEvent;
        public virtual Damagable Parent => this;
        public virtual bool CanTakeHit { get; set; } = true;
        public bool IsDead => CurrentHealth <= 0;

        ItemAttacher itemAttacher;
        private void Awake()
        {
            CurrentHealth = MaxHealth;
            CurrentBreath = MaxBreath;
            animGraph = GetComponent<AnimGraph>();
            itemAttacher = GetComponent<ItemAttacher>();
        }

        public virtual void TakeDamage(float damage, HitType damageType)
        {
            if (canBeDamagedBy != HitType.Any && canBeDamagedBy != damageType) return;

            UpdateHealth(-damage);

            if (CurrentHealth <= 0)
            {
                OnDead?.Invoke();
                OnDeadEvent?.Invoke();
            }
        }


        public virtual void UpdateHealth(float hpRestore)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + hpRestore, 0, MaxHealth);
            OnHealthUpdated?.Invoke();
        }

        public virtual void UpdateBreath(float breathRestore)
        {
            CurrentBreath = Mathf.Clamp(CurrentBreath + breathRestore, 0, MaxBreath);
            OnBreathUpdating?.Invoke();
        }

        public void Dead(List<AnimGraphClipInfo> deathAnimations, Action OnDead)
        {
            StartCoroutine(PlayDeathAnimation(deathAnimations, OnDead));
        }

        IEnumerator PlayDeathAnimation(List<AnimGraphClipInfo> deathAnimations, Action OnDead)
        {
            if (deathAnimations.Count > 0 && animGraph != null)
                yield return animGraph.CrossfadeAsync(deathAnimations[UnityEngine.Random.Range(0, deathAnimations.Count)], transitionBack: false);
            OnDead?.Invoke();
        }

        public float GetDefense()
        {
            return itemAttacher != null? itemAttacher.GetTotalDefense() : 0;
        }

        public object CaptureState()
        {
            var saveData = new DamagableSaveData()
            {
                currentHealth = CurrentHealth
            };

            return saveData;
        }

        public void RestoreState(object state)
        {
            var saveData = state as DamagableSaveData;
            CurrentHealth = saveData.currentHealth;

            OnHealthUpdated?.Invoke();
        }

        public Type GetSavaDataType()
        {
            return typeof(DamagableSaveData);
        }
    }

    [Serializable]
    public class DamagableSaveData
    {
        public float currentHealth;
    }
}