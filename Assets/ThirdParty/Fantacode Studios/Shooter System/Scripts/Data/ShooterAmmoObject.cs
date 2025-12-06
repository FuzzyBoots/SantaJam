using FS_CombatCore;
using FS_Core;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FS_ShooterSystem
{
    public class ShooterAmmoObject : MonoBehaviour
    {
        public ShooterAmmo Ammo { get; set; }
        [Tooltip("The trail effect for the bullet.")]
        public GameObject trailObject;
        [Tooltip("The transform where the ammo will attach upon hitting a target (e.g., arrows sticking into surfaces).")]
        public Transform hitTransform;

        private Vector3 velocity;
        private Vector3 previousPosition;
        private float elapsedTime;
        private Action<ShooterAmmoObject> returnToPoolCallback;

        public bool ReadyToPerform { get; set; } = false;
        public bool IsHit { get; set; } = false;

        [HideInInspector] public AudioSource fireAudioSource;
        [HideInInspector] public AudioSource hitAudioSource;

        [Tooltip("Assign a Rigidbody if the ammo requires physics-based collision (e.g., throwable items).")]
        public Rigidbody rigidBody;

        [Tooltip("Assign a Collider if the ammo should interact with other objects on collision (e.g., throwable items).")]
        public Collider collider;

        public ShooterFighter ParentShooterFighter { get; set; }

        float timer;

        private void Start()
        {
            SetAudioSource();
            if (rigidBody == null)
                rigidBody = GetComponent<Rigidbody>();
            if (collider == null)
                collider = GetComponent<Collider>();
            HandlePhysics(false);
        }

        public void HandlePhysics(bool enable = true)
        {
            if (rigidBody != null)
                rigidBody.isKinematic = !enable;
            if (collider != null)
                collider.enabled = enable;
        }

        public void SetAudioSource()
        {
            if (fireAudioSource == null)
            {
                var go = transform.parent != null ? transform.parent.gameObject : gameObject;

                fireAudioSource = go.AddComponent<AudioSource>();
                fireAudioSource.playOnAwake = false;
                hitAudioSource = go.AddComponent<AudioSource>();
                hitAudioSource.playOnAwake = false;
            }
        }
        /// <summary>
        /// Initializes the ammo object for firing by setting its position, direction, speed, 
        /// and callback for returning to the object pool. Also resets internal state and visuals.
        /// </summary>
        /// <param name="startPosition">The starting position of the ammo.</param>
        /// <param name="direction">The normalized direction in which the ammo will travel.</param>
        /// <param name="speed">The speed at which the ammo should move.</param>
        /// <param name="returnCallback">Callback action to return the ammo to the object pool after use.</param>
        /// <param name="hitPos">Optional. Used for direct hit calculations; defaults to Vector3.zero.</param>
        public void Initialize(Vector3 startPosition, Vector3 direction, float speed, Action<ShooterAmmoObject> returnCallback, Vector3 hitPos = new Vector3(), RaycastHit hit = new RaycastHit())
        {
            HandlePhysics(false);
            //transform.forward = direction;
            transform.position = startPosition;
            velocity = direction * speed;
            elapsedTime = 0f;
            timer = 0f;
            gameObject.SetActive(true);
            ClearTrailEffect();
            returnToPoolCallback = returnCallback;
            hitParentTransform = null;
            IsHit = false;
            if (!Ammo.usesTimedExplosion)
            {
                var distToHitPoint = Vector3.Distance(startPosition, hit.transform != null ? hit.point : hitPos);
                if ((Ammo.enableDirectHit || distToHitPoint <= 5f) && hit.transform != null)
                {
                    hit.point = hitPos;
                    HandleHit(hit, previousPosition);
                    if (Ammo.destroyImmediateAfterHit)
                        ReturnToPool();
                    else
                    {
                        transform.position = hitTransform != null ? hit.point - ((hitTransform.position - transform.position).normalized * Vector3.Distance(transform.position, hitTransform.position)) : hit.point;
                        // Store relative position and rotation
                        hitParentTransform = hit.transform;
                        localHitPosition = hitParentTransform.InverseTransformPoint(transform.position);
                        localHitRotation = Quaternion.Inverse(hitParentTransform.rotation) * transform.rotation;
                        transform.forward = velocity.normalized;
                        trailObject.SetActive(false);
                    }
                    IsHit = true;
                }
                else if (hitTransform != null)
                {
                    // Use the distance to the hit object to place the position in front of the character
                    transform.position = transform.position + direction.normalized * 1;
                    previousPosition = hitTransform.position;
                }
                else
                {
                    transform.position = transform.position + direction.normalized * 1;
                    // Default to the starting position if no hit occurred
                    previousPosition = transform.position;
                }
            }
        }
        void ClearTrailEffect()
        {
            if (trailObject != null)
            {
                trailObject.SetActive(true);
                var trail = trailObject.GetComponent<TrailRenderer>();
                if (trail != null)
                    trailObject.GetComponent<TrailRenderer>().Clear();
            }
        }

        private Transform hitParentTransform;
        private Vector3 localHitPosition;
        private Quaternion localHitRotation;

        private void Update()
        {
            if (!ReadyToPerform || Ammo == null) return;
            // explosion type
            if (Ammo.usesTimedExplosion)
            {
                if (timer <= Ammo.timer)
                    timer += Time.deltaTime;
                else
                {
                    SpawnExplotionEffect();
                    ParentShooterFighter?.AmmoExploded(this);
                    PlayHitSound(Ammo.explotionAudio);
                    HandleDamageAndExplosion(transform.position);
                    ReturnToPool();
                }
            }
            // Bullet type
            else
            {
                elapsedTime += Time.deltaTime;

                if (!IsHit)
                {
                    
                    Vector3 racastPosition = (hitTransform != null ? hitTransform.position : transform.position) + velocity * Time.deltaTime;
                    Vector3 newPosition = transform.position + velocity * Time.deltaTime;
                    var velDir = (racastPosition - previousPosition).normalized;


                    var hitFound = Physics.Raycast(previousPosition, velDir, out RaycastHit hit, (racastPosition - previousPosition).magnitude, ~ShooterSettings.instance.hitIgnoreMask, QueryTriggerInteraction.Ignore);
                    if (hitFound)
                    {
                        if (hit.transform.gameObject != ParentShooterFighter.gameObject)
                        {
                            HandleHit(hit, previousPosition);
                            if (Ammo.destroyImmediateAfterHit)
                                ReturnToPool();
                            else
                            {
                                transform.position = hitTransform != null ? hit.point - (velDir * Vector3.Distance(transform.position, hitTransform.position)) : hit.point;
                                // Store relative position and rotation
                                hitParentTransform = hit.transform;
                                localHitPosition = hitParentTransform.InverseTransformPoint(transform.position);
                                localHitRotation = Quaternion.Inverse(hitParentTransform.rotation) * transform.rotation;
                                transform.forward = velDir;
                                trailObject.SetActive(false);
                                IsHit = true;
                            }
                        }
                        else
                        {
                            transform.position = newPosition;
                            transform.forward = Vector3.MoveTowards(transform.forward,velDir, Time.deltaTime * .5f);
                        }
                    }
                    else
                    {
                        transform.position = newPosition;
                        transform.forward = Vector3.MoveTowards(transform.forward, velDir, Time.deltaTime * .5f);
                    }

                    previousPosition = hitTransform != null ? hitTransform.position : transform.position;
                    // Apply gravity and calculate the new position
                    velocity.y -= Ammo.gravity * Time.deltaTime;
                }

                
                
                if (IsHit && hitParentTransform != null)
                {
                    // Update position and rotation relative to the hit object
                    transform.position = hitParentTransform.TransformPoint(localHitPosition);
                    transform.rotation = hitParentTransform.rotation * localHitRotation;
                }
                // Deactivate the bullet if it exceeds its lifetime
                if (elapsedTime > Ammo.maxLifetime)
                {
                    ReturnToPool();
                }
            }
        }


        void HandleHit(RaycastHit hit, Vector3 origin)
        {

            if (Ammo.isExplosive)
            {
                SpawnExplotionEffect();
                ParentShooterFighter?.AmmoExploded(this);
            }
            else
            {
                var hitEffect = Ammo.GetHitEffect(hit.collider.gameObject.layer);
                if (hitEffect != null)
                {
                    SpawnHitEffect(hit, hitEffect);
                    PlayHitSound(hitEffect.hitAudioClip);
                }
            }

            if (Ammo.isExplosive)
                HandleDamageAndExplosion(hit.point, hit);
            else
                ApplyDirectDamage(hit.point, hit);
        }



        void HandleDamageAndExplosion(Vector3 ammoPos, RaycastHit? hit = null)
        {
            var ignoreLayerMask = Ammo.overrideHitIgnoreMask ? Ammo.hitIgnoreMask : ShooterSettings.instance.explosionIgnoreMask;

            // Detect all colliders within the explosion radius, ignoring the specified mask.
            // Filter out colliders that are obstructed by using the IsObstructed method.
            Collider[] colliders = Physics.OverlapSphere(ammoPos, Ammo.explosiveRadius, ~ignoreLayerMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore)
                .Where(c => !IsObstructed(ammoPos, GetColliderHitPosition(c), c))
                .ToArray();

            var damagableColliders = colliders.Where(c => c.GetComponent<Damagable>() != null).ToList();

            foreach (var collider in damagableColliders)
            {
                if (!Ammo.usesTimedExplosion && hit != null && hit.Value.collider != null && collider == hit.Value.collider)
                    ApplyDirectDamage(ammoPos, hit.Value);
                else
                    ApplyFieldDamage(collider, ammoPos);
            }

            foreach (var collider in colliders)
            {
                ApplyExplosionForce(collider, ammoPos);
            }
        }

        void ApplyDirectDamage(Vector3 hitPos, RaycastHit hit)
        {
            var fighter = hit.collider.GetComponent<FighterCore>();
            if (fighter == null)
                fighter = hit.collider.GetComponentInParent<FighterCore>();

            Damagable damageHandler = null;
            var boneDamageHandler = hit.collider.GetComponent<ShooterBoneDamageHandler>();
            if (boneDamageHandler != null)
                damageHandler = boneDamageHandler.ParentDamagable;
            else
                damageHandler = hit.collider.GetComponent<Damagable>();


            var currWeapon = ParentShooterFighter.CurrentWeapon;
            if (damageHandler != null && damageHandler.CanTakeHit)
            {
                float damage = DamageCalculator.CalculateDamage(currWeapon.GetAttributeValue<float>("Damage"), Ammo.GetAttributeValue<float>("Damage"), damageHandler);
                if (boneDamageHandler != null)
                    damage *= boneDamageHandler.damageMultiplier;

                if (fighter != null && ParentShooterFighter.Fighter.IsTarget(fighter.gameObject))
                {

                    var bloodEffect = fighter.overrideBloodEffects ? fighter.bloodEffectPrefab : Ammo.bloodEffect;
                    SpawnHitEffect(hit, bloodEffect);

                    fighter.TakeHit(new CombatHitData()
                    {
                        Damage = damage,
                        HitType = HitType.Ranged,
                        Damagable = damageHandler,
                        PlayReaction = currWeapon.playReactionOnTarget,
                        ReactionTag = currWeapon.targetReactionTag,
                        WaitTimeTillNextReaction = 0.8f
                    }, attacker: ParentShooterFighter.Fighter);
                }
                else
                {
                    if (damageHandler != null)
                        damageHandler.TakeDamage(damage, HitType.Ranged);
                }

                ParentShooterFighter.OnHitDamagable?.Invoke();
            }

            // Apply bullet hit force if the collider has a Rigidbody
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForceAtPosition(velocity * Time.deltaTime, hitPos, ForceMode.Impulse);
            }
        }

        void ApplyFieldDamage(Collider collider, Vector3 explosionPosition)
        {
            var fighter = collider.GetComponent<FighterCore>();
            if (fighter == null)
                fighter = collider.GetComponentInParent<FighterCore>();

            Vector3 hitDir = (collider.transform.position - explosionPosition).normalized;
            float distance = Vector3.Distance(explosionPosition, collider.transform.position);
            float damage = Mathf.Lerp(Ammo.GetAttributeValue<float>("Damage"), Ammo.minDamage, (distance / Ammo.explosiveRadius));

            if (fighter != null)
            {
                // Avoid hurting allies
                if (ParentShooterFighter != null && !ParentShooterFighter.Fighter.IsTarget(fighter.gameObject) && !ShooterSettings.instance.explosionAffectAllLayers)
                    return;

                fighter.TakeHit(new CombatHitData() { Damage = damage, HitType = HitType.Ranged, PlayReaction = false },
                    attacker: ParentShooterFighter?.Fighter);
                if (fighter?.Damagable.CurrentHealth <= 0 && fighter.defaultAnimations.useRagdollForExplosionDeath)
                    fighter.SetRagdollState(true);
            }
            else
            {
                Damagable damageHandler = collider.GetComponent<Damagable>();
                if (damageHandler != null)
                    damageHandler.TakeDamage(damage, HitType.Ranged);
            }
        }

        void ApplyExplosionForce(Collider collider, Vector3 explosionPosition)
        {
            Vector3 hitDir = (collider.transform.position - explosionPosition).normalized;
            float distance = Vector3.Distance(explosionPosition, collider.transform.position);
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float forceAmount = Ammo.explosionForce / Mathf.Clamp(distance, 1, Ammo.explosiveRadius);
                rb.AddForceAtPosition(hitDir * forceAmount, explosionPosition, ForceMode.Impulse);
            }
        }

        Vector3 GetColliderHitPosition(Collider collider)
        {
            Animator animator = collider.GetComponent<Animator>();
            return (animator != null && animator.isHuman)
                ? animator.GetBoneTransform(HumanBodyBones.UpperChest).position
                : collider.transform.position;
        }

        bool IsObstructed(Vector3 explotionPos, Vector3 hitAffectingPos, Collider target)
        {
            Vector3 direction = (hitAffectingPos - explotionPos).normalized;
            float distance = Vector3.Distance(explotionPos, hitAffectingPos);
            if (Physics.Raycast(explotionPos + Vector3.up * Mathf.Epsilon, direction, out RaycastHit raycastHit, ~target.gameObject.layer & ~ShooterSettings.instance.hitBoneMask, ~0, QueryTriggerInteraction.Ignore))
            {
                if (raycastHit.collider.gameObject != target.gameObject)
                {
                    return true;
                }
            }
            return false;
        }



        void PlayHitSound(AudioClip audioClip)
        {
            if (audioClip != null)
            {
                hitAudioSource.Stop();
                hitAudioSource.clip = audioClip;
                hitAudioSource.Play();
            }
        }

        private void SpawnHitEffect(RaycastHit hit, HitEffectData hitEffectData)
        {
            if (hitEffectData.hitMarker != null)
            {
                // Create the hit marker at the impact point
                GameObject effect = Instantiate(hitEffectData.hitMarker, hit.point, Quaternion.LookRotation(hit.normal));
                effect.transform.parent = hit.collider.transform; // Parent to the hit object for better organization
                // Destroy the effect after the specified lifetime
                Destroy(effect, ShooterSettings.instance.hitEffectLifetime);
            }
            if (hitEffectData.hitEffect != null)
            {
                // Create the hit effect at the impact point
                GameObject effect = Instantiate(hitEffectData.hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                // Destroy the effect after the specified lifetime
                Destroy(effect, ShooterSettings.instance.hitEffectLifetime);
            }
        }

        private void SpawnHitEffect(RaycastHit hit, GameObject hitEffect)
        {
            if (hitEffect == null) return;

            GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, ShooterSettings.instance.hitEffectLifetime);
        }

        void SpawnExplotionEffect()
        {
            if (Ammo.explotionPrefab != null)
            {
                var vfx = Instantiate(Ammo.explotionPrefab, this.transform.position, Quaternion.identity);
                Destroy(vfx, Ammo.explotionLifeTime);
                PlayHitSound(Ammo.explotionAudio);
            }
        }

        public void ReturnToPool()
        {
            gameObject.SetActive(false);
            returnToPoolCallback?.Invoke(this);
        }
    }
}