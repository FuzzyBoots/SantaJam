using FS_CombatCore;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ShooterSystem
{
    public class ArkhamShooterController : MonoBehaviour
    {
        [Header("Weapon Slots")]
        [Tooltip("Map keys 1-9 to specific EquippableItems here.")]
        public List<EquippableItem> weaponSlots = new List<EquippableItem>();

        [Header("Quickfire Settings")]
        public float doubleTapTime = 0.3f;
        public float enemyDetectionRadius = 10f;
        public float enemyDetectionAngle = 45f;
        public LayerMask enemyLayer;

        [Header("References")]
        public ShooterFighter shooterFighter;
        public ItemEquipper itemEquipper;

        private float[] lastTapTimes;
        private int quickfireWeaponIndex = -1;
        private bool isQuickfiring = false;

        private void Start()
        {
            if (!shooterFighter) shooterFighter = GetComponent<ShooterFighter>();
            if (!itemEquipper) itemEquipper = GetComponent<ItemEquipper>();

            // Initialize tap timers for 9 slots (1-9)
            lastTapTimes = new float[9];
        }

        private void Update()
        {
            if (isQuickfiring) return; // Block input during quickfire sequence? Or maybe allow queueing? simpler to block for now.

            HandleWeaponInput();
        }

        private void HandleWeaponInput()
        {
            // Simple iteration for keys 1-9
            for (int i = 0; i < 9; i++)
            {
                // KeyCode.Alpha1 corresponds to '1'. Alpha1 is 49.
                KeyCode key = KeyCode.Alpha1 + i;
                
                if (Input.GetKeyDown(key))
                {
                    HandleWeaponKeyPress(i);
                }
            }
        }

        private void HandleWeaponKeyPress(int slotIndex)
        {
            if (slotIndex >= weaponSlots.Count || weaponSlots[slotIndex] == null) return;

            float timeNow = Time.time;
            if (timeNow - lastTapTimes[slotIndex] < doubleTapTime)
            {
                // Double tap detected
                AttemptQuickfire(slotIndex);
            }
            else
            {
                // Single tap (potentially)
                // We don't want to delay the "Select" action, so we select immediately on first tap.
                // If they tap again quickly, it upgrades to Quickfire.
                EquipWeapon(slotIndex);
            }
            lastTapTimes[slotIndex] = timeNow;
        }

        private void EquipWeapon(int slotIndex)
        {
            var item = weaponSlots[slotIndex];
            if (itemEquipper.EquippedItem != item)
            {
                itemEquipper.EquipItem(item);
            }
        }

        private void AttemptQuickfire(int slotIndex)
        {
            if (CheckForEnemyInFront())
            {
                StartCoroutine(QuickfireSequence(slotIndex));
            }
            else
            {
                // No enemy, just ensure equipped (already done by first tap)
                EquipWeapon(slotIndex);
            }
        }

        private bool CheckForEnemyInFront()
        {
            // Spherecast or OverlapSphere and check angle
            Vector3 origin = transform.position + Vector3.up; // Headish height
            Collider[] hits = Physics.OverlapSphere(origin, enemyDetectionRadius, enemyLayer);

            foreach (var hit in hits)
            {
                if (hit.transform == transform) continue; // Skip self

                Vector3 dirToEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToEnemy);

                if (angle < enemyDetectionAngle)
                {
                    // Check generic line of sight just in case (optional but good)
                    if (Physics.Raycast(origin, dirToEnemy, out RaycastHit rayHit, enemyDetectionRadius))
                    {
                         if(rayHit.collider == hit) return true; // Found reachable enemy
                    }
                }
            }
            return false;
        }

        private IEnumerator QuickfireSequence(int slotIndex)
        {
            isQuickfiring = true;
            EquippableItem weaponToFire = weaponSlots[slotIndex];

            // 1. Unholster (Equip) if not valid
            if (itemEquipper.EquippedItem != weaponToFire)
            {
                itemEquipper.EquipItem(weaponToFire);
                // Wait for equip to finish... ItemEquipper doesn't give a handy "done" task here, 
                // but we can assume some delay or hook into events.
                // Simpler hack: wait for animation time or fixed delay.
                // Checking `itemEquipper.IsEquippingItem` might be safer.
                yield return new WaitForSeconds(0.1f); 
                yield return new WaitUntil(() => !itemEquipper.IsEquippingItem);
            }

            // 2. Fire
            // We need to tell ShooterFighter to aim at the target?
            // ShooterFighter.UpdateAimingTargets() normally handles this if we have a target.
            // We might need to force a temporary target lock or just force aim direction.
            
            // For Arkham style, it usually auto-aims at the "best" target.
            // Let's find that target again and set it.
            Transform target = FindBestTarget();
            if (target != null)
            {
                shooterFighter.Fighter.Target = target.GetComponent<FighterCore>();
                // If target isn't FighterCore, we might need a dummy target or just SetAimPoint.
                
                // Let's assume for now we just set Aim Point
                shooterFighter.SetAimPoint(target.position + Vector3.up);
            }

            shooterFighter.StartAiming();
            yield return new WaitForSeconds(0.2f); // Aim time
            
            shooterFighter.Shoot();
            yield return new WaitForSeconds(0.1f); // Fire trigger time

            // Wait for shot animation roughly?
             yield return new WaitForSeconds(0.5f);

            shooterFighter.StopAiming();

            // 3. Holster (Unequip)
            itemEquipper.UnEquipItem();

            isQuickfiring = false;
        }

        private Transform FindBestTarget()
        {
            // Re-run detection to get exact transform
             Vector3 origin = transform.position + Vector3.up; 
            Collider[] hits = Physics.OverlapSphere(origin, enemyDetectionRadius, enemyLayer);
            
            Transform bestTarget = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform) continue;

                Vector3 dirToEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToEnemy);

                if (angle < enemyDetectionAngle)
                {
                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        bestTarget = hit.transform;
                    }
                }
            }
            return bestTarget;
        }
    }
}
