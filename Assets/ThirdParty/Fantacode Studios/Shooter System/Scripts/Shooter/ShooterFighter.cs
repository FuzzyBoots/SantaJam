using FS_CombatCore;
using FS_Core;
using FS_CoverSystem;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

namespace FS_ShooterSystem
{
    public class ShooterFighter : MonoBehaviour
    {
        public ShooterWeapon CurrentWeapon
        {
            get
            {
                return itemEquipper?.EquippedItem is ShooterWeapon shooterWeaponData ? shooterWeaponData : null;
            }
        }
        public ThrowableItem CurrentThrowableItem
        {
            get
            {
                return (itemEquipper?.EquippedItem is ThrowableItem currentThrowableItem)
                    ? currentThrowableItem
                    : null;
            }
        }

        public FighterCore Fighter { get; private set; }



        private float maxHorizontalAngle = 70;
        private float maxVerticalAngle = 70;
        private float rotationSpeed = 10;

        DirectionAxis rightHandForward = DirectionAxis.PositiveY; // Direction in which the hand should point when aiming
        DirectionAxis rightHandUp = DirectionAxis.PositiveZ; // Direction in which the hand should be oriented upwards when aiming
        DirectionAxis leftHandForward = DirectionAxis.PositiveY; // Direction in which the hand should point when aiming
        DirectionAxis leftHandUp = DirectionAxis.PositiveZ; // Direction in which the hand should be oriented upwards when aiming

        [Space(10)]
        [Tooltip("If true, the fighter has unlimited ammo for the equipped weapons.")]
        public bool hasInfiniteAmmo = false;
        public bool canRoll = true;
        public bool canDodge = true;



        #region Variables

        private Transform spine;
        private Transform chest;
        private Transform upperChest;

        private ArmIKReference existingArmIKReference;

        private float spineWeight = 0.1f;
        private float chestWeight = 0.1f;
        private float upperChestWeight = 0.4f;



        public Animator Animator { get; private set; }
        private AnimGraph animGraph;
        private ItemEquipper itemEquipper;
        public CharacterID characterID { get; private set; }

        private bool waitForNextFire;

        private float ikWeight = 0;

        private Quaternion spineOriginal;
        private Quaternion chestOriginal;
        private Quaternion upperChestOriginal;

        public Action OnStartAim;
        public Action OnStopAim;
        public Action OnFire;
        public Action OnStartReload;
        public Action OnHitDamagable;
        public Action OnTargetLocked;   // Target is currently under the aim
        public Action OnTargetCleared;  // Target is no longer under the aim
        public Action<ShooterAmmoObject> OnAmmoExploded;

        bool enableIk = true;
        bool aimingAnimationPlaying;

        Transform rightHand;
        Transform leftHand;

        #endregion

        #region Properties

        public bool IsShooterWeaponEquipped => itemEquipper?.EquippedItem is ShooterWeapon;
        public bool IsInScopeView { get; set; }
        public bool IsWeaponEquipped => IsShooterWeaponEquipped || IsThrowableItemEquipped;

        public ShooterWeaponObject CurrentWeaponRight => itemEquipper?.EquippedItemRight as ShooterWeaponObject;
        public ShooterWeaponObject CurrentWeaponLeft => itemEquipper?.EquippedItemLeft as ShooterWeaponObject;
        private ShootingAimController rightHandAimController => CurrentWeaponRight?.aimController;
        private ShootingAimController leftHandAimController => CurrentWeaponLeft?.aimController;

        public ShooterWeaponObject CurrentShooterWeaponObject => itemEquipper.EquippedItemObject is ShooterWeaponObject ? itemEquipper.EquippedItemObject as ShooterWeaponObject : null;
        public ThrowableItemObject CurrentThrowableItemObject => itemEquipper.EquippedItemObject is ThrowableItemObject ? itemEquipper.EquippedItemObject as ThrowableItemObject : null;

        public bool IsShooting => Fighter.Action == FighterAction.Shooting;
        public bool IsLoadingAmmo { get; set; }
        public bool IsAiming { get; private set; }
        public bool IsReloading => Fighter.Action == FighterAction.Reloading;
        public bool IsChangingWeapon => itemEquipper.IsChangingItem;

        public bool CanAim { get; set; } = true;
        public bool IsAimPointObstructed { get; set; }
        public Vector3 TargetAimPoint { get; set; }
        public Vector3 BulletHitPoint { get; set; }
        public RaycastHit TargetHitData;
        public bool IsTransitioningToAim { get; private set; }
        public bool IsThrowableItemEquipped => itemEquipper?.EquippedItem is ThrowableItem;
        public bool IsEquipped => IsThrowableItemEquipped || IsShooterWeaponEquipped;


        // Debug varibles
        bool _ikSetupMode = false;
        bool _previousIkSetupMode = false;
        public bool IkSetupMode
        {
            get => _ikSetupMode;
            set
            {
                _ikSetupMode = value;

                if (_previousIkSetupMode != _ikSetupMode)
                {
                    if (CurrentShooterWeaponObject != null)
                    {
                        //if (_ikSetupMode)
                        //{
                        //    CurrentShooterWeaponObject.supportHandIkTarget.SetParent(null, true);
                        //    CurrentShooterWeaponObject.holdHandIkTarget.SetParent(null, true);
                        //}
                        //else
                        //{
                        //    if (CurrentShooterWeaponObject != null)
                        //    {
                        //        CurrentShooterWeaponObject.supportHandIkTarget.SetParent(CurrentShooterWeaponObject.transform, true);
                        //        CurrentShooterWeaponObject.holdHandIkTarget.SetParent(CurrentShooterWeaponObject.transform, true);
                        //    }
                        //}
                    }
                    _previousIkSetupMode = _ikSetupMode;
                }
            }
        }

        #endregion

        #region Events
        //public Action OnDead;
        public Action OnEndAction;

        public Action<EquippableItem> OnShooterWeaponEquipAction;
        public Action OnShooterWeaponUnEquipAction;
        public Action OnThrowableItemEquipAction;
        public Action OnThrowableItemUnEquipAction;

        public Action OnRecoil;

        public Action<EquippableItem> OnItemBecameUnused;
        #endregion

        private void Awake()
        {
            Fighter = GetComponent<FighterCore>();

            Animator = GetComponent<Animator>();
            
            animGraph = GetComponent<AnimGraph>();
            itemEquipper = GetComponent<ItemEquipper>();
            characterID = GetComponent<CharacterID>();

            leftHand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = Animator.GetBoneTransform(HumanBodyBones.RightHand);
            spine = Animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = Animator.GetBoneTransform(HumanBodyBones.Chest);
            upperChest = Animator.GetBoneTransform(HumanBodyBones.UpperChest);

            if (upperChest) upperChestOriginal = currentUpperChestRot = upperChest.localRotation;

            itemEquipper.OnEquip += EquipItem;
            itemEquipper.OnEquipComplete += (itemData) =>
            {
                if (itemData is ShooterWeapon)
                {
                    OnShooterWeaponEquipAction?.Invoke(itemData);
                    enableIk = true;

                    Fighter.ResetActionToNone(FighterAction.SwitchingWeapon);

                    if (CurrentShooterWeaponObject.CurrentAmmoCount == 0 && CurrentWeapon.autoReload)
                        Reload();
                }
                else if (itemData is ThrowableItem)
                {
                    OnThrowableItemEquipAction?.Invoke();
                    Fighter.ResetActionToNone(FighterAction.SwitchingWeapon);
                }
            };

            itemEquipper.OnUnEquip += UnEquipItem;
            itemEquipper.OnBeforeItemDisable += (wd) =>
            {
                if (wd is ShooterWeapon)
                {
                    DisableCurrentWeapon();
                    enableIk = true;
                    Fighter.ResetActionToNone(FighterAction.SwitchingWeapon);
                }
                else if (wd is ThrowableItem)
                {
                    Fighter.ResetActionToNone(FighterAction.SwitchingWeapon);
                }
            };
            IkSetupMode = false;

            Fighter.OnDeath += () =>
            {
                StopAiming();
                enabled = false;
            };
        }

        #region Aiming
        public void StartAiming()
        {
            if (IsShooterWeaponEquipped)
            {
                if (!CurrentWeapon.canAimWithoutAmmo && CurrentShooterWeaponObject.CurrentAmmoCount == 0)
                {
                    if (IsAiming && !IsShooting)
                        StopAiming();
                    return;
                }
            }
            else if (IsThrowableItemEquipped)
                CanAim = true;

            if (!IsAiming && CanAim && !Fighter.IsBusy && !IsLoadingAmmo)
            {
                OnStartAim?.Invoke();
                var clip = (IsThrowableItemEquipped ? CurrentThrowableItem.aimAnimation : CurrentWeapon.aimClip);
                var mask = (IsThrowableItemEquipped ? Mask.UpperBody : CurrentWeapon.aimAnimationMask);
                animGraph.PlayLoopingAnimation(null, clip, mask: mask);
                aimingAnimationPlaying = clip.clip != null;
                Animator.SetBool(AnimatorParameters.IsAiming, true);
                Animator.CrossFade(AnimationNames.ShootingAim, .2f);
                IsAiming = true;
                IsTransitioningToAim = true;
                StartCoroutine(AsyncUtil.RunAfterDelay(0.2f, () =>
                {
                    if (IsAiming)
                    {
                        IsTransitioningToAim = false;
                    }
                }));
            }
            else if (!CanAim && IsAiming)
                StopAiming();
        }
        public void StopAiming()
        {
            if (IsAiming)
            {
                IsAiming = false;
                OnStopAim?.Invoke();
                Animator.SetBool(AnimatorParameters.IsAiming, false);

                if (chargingAnimationPlayed || (CurrentWeapon != null && CurrentWeapon.isChargedWeapon))
                {
                    animGraph.StopLoopingAnimations(true);
                    chargingAnimationPlayed = false;
                    itemEquipper.PlayIdleAnimation();
                    aimingAnimationPlaying = false;
                }
                else
                {
                    StopAimingAnimation();
                }

            }
        }


        public void StopAimingAnimation()
        {
            if (aimingAnimationPlaying)
                animGraph.StopLoopingAnimations(false);
            aimingAnimationPlaying = false;
        }

        /// <summary>
        /// Sets the aiming target point with optional smooth blending on the XZ and Y axes.
        /// Clamps the horizontal aim direction within a specified angle from the character's forward direction.
        /// Use blending for smoother player transitions, but disable it (especially for enemies) when immediate snapping is needed.
        /// </summary>
        /// <param name="aimPoint">The desired world-space target point to aim at.</param>
        /// <param name="blendXZ">Whether to smoothly blend the X and Z components of the aim point.</param>
        /// <param name="blendY">Whether to smoothly blend the Y component of the aim point.</param>
        public void SetAimPoint(Vector3 aimPoint, bool blendXZ = false, bool blendY = false)
        {
            if (IsAiming)
            {
                Vector3 blendedTarget = TargetAimPoint;
                // Compute dynamic blend speed based on distance
                float dist = Vector3.Distance(TargetAimPoint, aimPoint);

                //float minSpeed = 5;
                //float maxSpeed = 1000;

                float speed = 1000;
                // Blend XZ
                if (blendXZ)
                {
                    Vector2 currentXZ = new Vector2(TargetAimPoint.x, TargetAimPoint.z);
                    Vector2 targetXZ = new Vector2(aimPoint.x, aimPoint.z);
                    Vector2 blendedXZ = Vector2.MoveTowards(currentXZ, targetXZ, Time.deltaTime * speed);

                    blendedTarget.x = blendedXZ.x;
                    blendedTarget.z = blendedXZ.y;
                }
                else
                {
                    blendedTarget.x = aimPoint.x;
                    blendedTarget.z = aimPoint.z;
                }

                // Blend Y
                if (blendY)
                {
                    blendedTarget.y = Mathf.MoveTowards(TargetAimPoint.y, aimPoint.y, Time.deltaTime * speed);
                }
                else
                {
                    blendedTarget.y = aimPoint.y;
                }

                // Clamp aim direction angle relative to player's forward
                Transform character = Animator.GetBoneTransform(HumanBodyBones.Hips);
                Vector3 toTarget = blendedTarget - character.position;
                Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z).normalized;

                float maxAngle = 75;
                float angle = Vector3.Angle(character.forward, flatToTarget);

                if (angle > maxAngle)
                {
                    // Clamp direction to max angle
                    Vector3 clampedDirection = Vector3.RotateTowards(character.forward, flatToTarget, Mathf.Deg2Rad * maxAngle, float.MaxValue);

                    // Maintain horizontal distance
                    float flatDistance = new Vector2(toTarget.x, toTarget.z).magnitude;
                    Vector3 clampedPosition = character.position + clampedDirection * flatDistance;

                    blendedTarget.x = clampedPosition.x;
                    blendedTarget.z = clampedPosition.z;
                }

                TargetAimPoint = blendedTarget;
            }
            else
            {
                TargetAimPoint = aimPoint;
            }
        }

        public void UpdateAimingTargets(float randomFactor = 0.5f)
        {
            // If we are actively in ShootState, �playerController� exists and we want to aim at them:
            if (Fighter.Target != null && !IsReloading)
            {
                // Face the target
                Vector3 look = Fighter.Target.transform.position - transform.position;

                if (look.sqrMagnitude > 0f)
                    transform.rotation = Quaternion.LookRotation(look.SetY(0));

                // Aim
                Vector3 tp = Fighter.Target.transform.position;

                if (Vector3.Distance(tp, transform.position) < 2f)
                {
                    tp += look.normalized * 2f; // Move the shooter fighter a bit forward to avoid aiming too close to the target
                }

                TargetAimPoint = tp + Vector3.up;
                BulletHitPoint = tp + Vector3.up + Random.insideUnitSphere * randomFactor;
            }
            else
            {
                // Otherwise, aim straight forward (so the gun doesn�t try to shoot backwards or break).
                // We choose a point �far in front� of the AI (e.g., preferredRange units forward).
                Vector3 forwardPoint = transform.position + transform.forward * 10f;
                TargetAimPoint = forwardPoint;
                BulletHitPoint = forwardPoint;
            }
        }


        #endregion

        #region Shooting
        [HideInInspector]
        public bool chargingAnimationPlayed;
        public void HandleCharging()
        {
            if (!IsShooterWeaponEquipped || waitForNextFire || !IsAiming || !CanAim || IsTransitioningToAim)
                return;
            chargingAnimationPlayed = CurrentWeapon.chargeClip.clip != null;
            animGraph.PlayLoopingAnimation(null, CurrentWeapon.chargeClip, mask: CurrentWeapon.chargeAnimationMask);
        }

        public void Shoot(float ammoForceMultiplier = 1)
        {
            if (!IsShooterWeaponEquipped || waitForNextFire || !IsAiming || !CanAim || IsTransitioningToAim || IsReloading) return;


            if (CurrentWeapon.isDualItem)
            {
                StartCoroutine(HandleShoot(CurrentWeaponRight, ammoForceMultiplier));
                StartCoroutine(HandleShoot(CurrentWeaponLeft, ammoForceMultiplier));
            }
            else
            {
                StartCoroutine(HandleShoot(CurrentShooterWeaponObject, ammoForceMultiplier));
            }
        }
        IEnumerator HandleShoot(ShooterWeaponObject weapon, float ammoForceMultiplier = 1)
        {
            if (IsTransitioningToAim)
            {
                if (CurrentWeapon.hipFire)
                {
                    yield return new WaitUntil(() => !IsTransitioningToAim);
                }
                else
                    yield break;
            }
            if (weapon.CurrentAmmoCount == 0)
            {
                if (!weapon.HasAmmo)
                {
                    var triggerAudio = weapon.AudioSource;
                    triggerAudio.clip = CurrentWeapon.triggerAudioClip;
                    triggerAudio.Play();
                }
                yield break;
            }

            StartCoroutine(ExecuteShooting(weapon, ammoForceMultiplier));
        }

        private IEnumerator ExecuteShooting(ShooterWeaponObject weapon, float ammoForceMultiplier = 1)
        {
            OnFire?.Invoke();
            if (!IsReloading)
                Fighter.SetAction(FighterAction.Shooting);

            // Detaching the ammo's parent must be delayed by one frame because Unity updates transform parenting at the end of the frame.
            yield return new WaitForEndOfFrame();

            if (weapon.CurrentLoadedAmmo != null)
                weapon.CurrentLoadedAmmo.transform.parent.SetParent(null, true);
            Vector3 shootDirection = (BulletHitPoint - weapon.aimController.handAlignment.Helper.position).normalized;
            ExecuteFire(shootDirection, CurrentWeapon.isBurst, weapon, ammoForceMultiplier);
            //Debug.DrawLine(BulletHitPoint, BulletHitPoint+ shootDirection, Color.red);
            //Debug.Break();
            OnRecoil?.Invoke();


            if (!CurrentWeapon.playLoadAnimationPerShot)
                StartCoroutine(HandleFireDelay(CurrentWeapon.GetAttributeValue<float>("Fire Frequency")));
            else
                waitForNextFire = true;
            var currentWeaponTemp = CurrentWeapon;
            if (!IsInScopeView)
                yield return animGraph.CrossfadeAsync(null, clipInfo: CurrentWeapon.shootClip, mask: CurrentWeapon.shootAnimationMask, isAdditiveLayerAnimation: true);
            if (CurrentWeapon != null && currentWeaponTemp == CurrentWeapon)
            {
                if (chargingAnimationPlayed)
                {
                    animGraph.StopLoopingAnimations(false);
                    chargingAnimationPlayed = false;
                    //itemEquipper.PlayIdleAnimation();
                    //aimingAnimationPlaying = false;
                }

                if (CurrentWeapon.playLoadAnimationPerShot && weapon.CurrentAmmoCount > 0)
                    StartCoroutine(LoadAmmoAfterShoot(weapon, CurrentWeapon.loadAmmoClip));

                if (CurrentWeapon.autoReload && weapon.CurrentAmmoCount == 0 && weapon.HasAmmo)
                {
                    if (IsInScopeView && CurrentWeapon.shootClip.clip != null)
                        yield return new WaitForSeconds(CurrentWeapon.shootClip.clip.length);
                    if (CurrentWeapon.playLoadAnimationPerShot)
                        waitForNextFire = false;
                    Reload();
                }
            }
            if (CurrentWeapon == null)
                waitForNextFire = false;

            Fighter.ResetActionToNone(FighterAction.Shooting);
        }

        private void ExecuteFire(Vector3 shootDirection, bool isBurst, ShooterWeaponObject weapon, float ammoForceMultiplier = 1)
        {
            if (isBurst)
            {
                for (int i = 0; i < CurrentWeapon.burstFireBulletCount; i++)
                {
                    Vector3 direction = GenerateRandomizedDirection(shootDirection,
                        CurrentWeapon.burstSpreadingAngleRange.x,
                        CurrentWeapon.burstSpreadingAngleRange.y);
                    SpawnBullet(direction, weapon, ammoForceMultiplier);
                }
            }
            else
            {
                ExecuteAmmo(BulletHitPoint, shootDirection, weapon, ammoForceMultiplier);
            }
        }
        private void ExecuteAmmo(Vector3 TargetAimPoint, Vector3 shootDirection, ShooterWeaponObject weapon, float ammoForceMultiplier = 1)
        {
            Vector3 newTargetPoint = GetRandomPositionByRadiusInDirection(TargetAimPoint, shootDirection, CurrentWeapon.bulletSpreadRadius);
            Vector3 direction = (newTargetPoint - weapon.ammoSpawnPoint.position).normalized;
            SpawnBullet(direction, weapon, ammoForceMultiplier);
        }

        Vector3 GetRandomPositionByRadiusInDirection(Vector3 center, Vector3 dir, float radius)
        {
            dir = dir.normalized;
            Vector3 randomPoint = center + dir * radius + Random.insideUnitSphere * radius;
            return randomPoint;
        }

        public Vector3 GenerateRandomizedDirection(Vector3 baseDirection, float minAngle = 0, float maxAngle = 3)
        {
            baseDirection.Normalize();
            float angleVariance = Random.Range(minAngle, maxAngle);
            Quaternion randomRotation = Quaternion.Euler(
                Random.Range(-angleVariance, angleVariance),
                Random.Range(-angleVariance, angleVariance),
                Random.Range(-angleVariance, angleVariance)
            );
            return (randomRotation * baseDirection).normalized;
        }


        private IEnumerator ExecuteEffects(ShooterWeaponObject weapon)
        {
            weapon.OnFire?.Invoke();
            weapon.OnFireAction?.Invoke();

            foreach (var particle in weapon.vfx)
            {
                particle.Emit(1);
            }

            if (weapon.CurrentLoadedAmmo.fireAudioSource != null)
            {
                weapon.CurrentLoadedAmmo.fireAudioSource.clip = CurrentWeapon.fireAudioClip;
                weapon.CurrentLoadedAmmo.fireAudioSource.Play();
            }

            //if (weapon.CurrentLoadedAmmo.trailObject != null)
            //    weapon.CurrentLoadedAmmo.trailObject.SetActive(true);
            if (weapon.flash != null)
            {
                weapon.flash.enabled = true;
                yield return new WaitForSeconds(.1f);
                if (weapon.flash.enabled)
                    weapon.flash.enabled = false;
            }
        }

        private void SpawnBullet(Vector3 direction, ShooterWeaponObject weapon, float ammoForceMultiplier = 1)
        {
            weapon.SpawnBullet(direction, ammoForceMultiplier);
            StartCoroutine(ExecuteEffects(weapon));

            if (weapon.CurrentAmmoCount > 0)
                weapon.SetAmmo();
        }

        private IEnumerator HandleFireDelay(float fireTime)
        {
            waitForNextFire = true;
            yield return new WaitForSeconds(fireTime);
            waitForNextFire = false;
        }

        private IEnumerator LoadAmmoAfterShoot(ShooterWeaponObject weapon, AnimGraphClipInfo clip)
        {
            IsLoadingAmmo = true;
            if (Fighter.Action != FighterAction.Dodging && !IsReloading)
            {
                if (CurrentWeapon.loadAmmoAudioClip != null)
                {
                    weapon.AudioSource.clip = CurrentWeapon.loadAmmoAudioClip;
                    weapon.AudioSource.Play();
                }
                yield return animGraph.CrossfadeAsync(null, clip, mask: Mask.Arm);
            }
            waitForNextFire = IsLoadingAmmo = false;

        }

        public void ThrowItem(Vector3 targetPoint)
        {
            if (CurrentThrowableItemObject.TotalAmmoCount == 0) return;

            Rigidbody itemRb = CurrentThrowableItemObject.CurrentEquippedAmmo.rigidBody;

            if (itemRb == null)
            {
                Debug.LogWarning("The Throwable Ammo does not contain rigid body component");
                return;
            }


            StopAiming();
            Fighter.SetAction(FighterAction.ThrowingObject);

            animGraph.Crossfade(null, CurrentThrowableItem.throwAnimation, mask: Mask.UpperBodyWithRoot, OnComplete: () =>
            {
                if (CurrentThrowableItemObject != null)
                {
                    EquipThrowableAmmo();
                    Fighter.ResetActionToNone(FighterAction.ThrowingObject);
                }
            });

            CurrentThrowableItemObject.CurrentEquippedAmmo.transform.parent.transform.parent = null;
            CurrentThrowableItemObject.CurrentEquippedAmmo.HandlePhysics(true);
            CurrentThrowableItemObject.CurrentEquippedAmmo.ReadyToPerform = true;


            Vector3 velocity = CalculateVelocityFromTargetPoint(CurrentThrowableItemObject.CurrentEquippedAmmo.transform.position, targetPoint);
            itemRb.velocity = velocity;
            // Apply random rotation
            itemRb.angularVelocity = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );
            CurrentThrowableItemObject.TotalAmmoCount--;


        }

        public void CancelThrowableAmmo()
        {
            StopAiming();
        }

        /// <summary>
        /// Calculate velocity based directly on the target point
        /// </summary>
        public Vector3 CalculateVelocityFromTargetPoint(Vector3 startPoint, Vector3 targetPoint)
        {
            // Calculate direct vector from start to hit
            Vector3 directVector = targetPoint - startPoint;
            // Calculate horizontal distance
            Vector3 horizontalVector = new Vector3(directVector.x, 0, directVector.z);
            float horizontalDistance = horizontalVector.magnitude;

            // Calculate direction
            Vector3 direction = directVector.normalized;

            // Adjust force based on distance
            float adjustedForce = CurrentThrowableItem.throwForce * (1 + horizontalDistance * CurrentThrowableItem.distanceMultiplier / CurrentThrowableItem.maxThrowDistance);

            // Create base velocity vector along the direct path
            Vector3 velocity = direction * adjustedForce;

            // Add arc by increasing Y component
            float heightAdjustment = CurrentThrowableItem.arcHeight * (1 - Mathf.Abs(directVector.y) / horizontalDistance);
            velocity.y += heightAdjustment * adjustedForce;


            // If target is above, add extra force to reach it
            if (targetPoint.y > startPoint.y)
            {
                float heightDiff = targetPoint.y - startPoint.y;
                velocity.y += Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * heightDiff) * 0.5f;
            }

            return velocity;
        }

        public void EquipThrowableAmmo()
        {
            CurrentThrowableItemObject.EquipThrowableAmmo();

            if (CurrentThrowableItemObject.TotalAmmoCount == 0)
            {
                OnItemBecameUnused?.Invoke(CurrentThrowableItem);
                itemEquipper.IsEquippingItem = false;
                itemEquipper.UnEquipItem(false);
            }
        }

        #endregion

        #region Reload

        public void Reload()
        {
            if (!IsShooterWeaponEquipped || Fighter.Action == FighterAction.Dodging) return;
            var weapons = CurrentWeapon.isDualItem ? new[] { CurrentWeaponRight, CurrentWeaponLeft } : new[] { CurrentShooterWeaponObject };
            foreach (var w in weapons)
            {
                HandleReload(w);
            }
        }

        private void HandleReload(ShooterWeaponObject weapon)
        {
            if (weapon == null || !weapon.HasAmmo || weapon.CurrentAmmoCount >= CurrentWeapon.GetAttributeValue<int>("Magazine Size") || IsReloading) return;

            StartCoroutine(ReloadWeapon(weapon));
        }

        private IEnumerator ReloadWeapon(ShooterWeaponObject weapon)
        {
            if (CurrentWeapon.reloadClip.clip != null || (!CurrentWeapon.useReloadAnimationDuration && CurrentWeapon.reloadTime > 0))
            {
                OnStartReload?.Invoke();
                Fighter.SetAction(FighterAction.Reloading);
                enableIk = false;
            }
            if (IsInScopeView)
            {
                StopAiming();
            }
            // Play reload sound if available
            if (weapon.AudioSource && CurrentWeapon.reloadAudioClip)
            {
                weapon.AudioSource.clip = CurrentWeapon.reloadAudioClip;
                weapon.AudioSource.Play();
            }
            bool ammoSpawned = false;

            var curWeapon = CurrentWeapon;
            // Play reload animation if available
            if (CurrentWeapon.reloadClip.clip)
            {
                if (CurrentWeapon.useReloadAnimationDuration)
                {
                    yield return animGraph.CrossfadeAsync(null, CurrentWeapon.reloadClip, mask: CurrentWeapon.reloadAnimationMask,
                        onAnimationUpdate: (normalizedTimer, timer) =>
                        {
                            if (!ammoSpawned && normalizedTimer >= CurrentWeapon.ammoEnableTime)
                            {
                                if (IsReloading && CurrentWeapon != null && curWeapon == CurrentWeapon)
                                {
                                    weapon?.SetAmmo();
                                    ammoSpawned = true;
                                }
                            }
                        });
                }
                else
                {
                    animGraph.Crossfade(null, CurrentWeapon.reloadClip, mask: CurrentWeapon.reloadAnimationMask);
                    yield return new WaitForSeconds(CurrentWeapon.reloadTime);
                }
            }
            else if (!CurrentWeapon.useReloadAnimationDuration)
            {
                yield return new WaitForSeconds(CurrentWeapon.reloadTime);
            }

            if (IsReloading && CurrentWeapon != null && curWeapon == CurrentWeapon)    // If reload was not interupted
            {
                if (!ammoSpawned)
                    weapon?.SetAmmo();

                weapon?.ReloadAmmo(CurrentWeapon.GetAttributeValue<int>("Magazine Size"));

                //if (aimingAnimationPlaying)
                //    animGraph.StopLoopingAnimations(false);
            }

            Fighter.ResetActionToNone(FighterAction.Reloading);
            enableIk = true;
        }


        #endregion

        #region Animation Handling

        private IEnumerator PlayCrossFadeAnimation(string animationName, int layerIndex, float transitionDuration = 0.2f, Action onComplete = null, params ActionData[] actions)
        {
            Animator.CrossFadeInFixedTime(animationName, transitionDuration);
            yield return null;

            var animationState = Animator.GetNextAnimatorStateInfo(layerIndex);
            float elapsedTime = 0f;
            float normalizedTime = 0f;

            while (elapsedTime <= animationState.length)
            {
                foreach (var action in actions)
                {
                    if (!action.actionInvoked && normalizedTime >= action.normalizeTime)
                    {
                        action.action?.Invoke();
                        action.actionInvoked = true;
                    }
                }
                elapsedTime += Time.deltaTime * Animator.speed;
                normalizedTime = elapsedTime / animationState.length;
                yield return null;
            }
            onComplete?.Invoke();
        }

        #endregion

        #region Utility Methods

        public void EvaluateShootingFeasibility()
        {
            float distance = Vector3.Distance(TargetAimPoint, upperChest.position);

            //CanShoot = distance >= minShootingDistance;
        }

        public void AmmoExploded(ShooterAmmoObject shooterAmmo)
        {
            OnAmmoExploded?.Invoke(shooterAmmo);
        }

        #endregion

        #region Equip Weapon

        public void EquipItem(EquippableItem itemData)
        {
            TargetAimPoint = transform.position;
            if (itemData is ShooterWeapon)
            {
                var weaponData = itemData as ShooterWeapon;

                enableIk = false;

                Fighter.SetAction(FighterAction.SwitchingWeapon);
                Fighter.TriggerOnWeaponEquip(weaponData);

                if (weaponData != null)
                {
                    if (weaponData.isDualItem)
                    {
                        // Right hand weapon
                        SetRightHand(weaponData);

                        // Left hand weapon
                        SetLeftHand(weaponData);
                        

                    }
                    else if (weaponData.holderBone == HumanBodyBones.RightHand)
                    {
                        SetRightHand(weaponData);
                        CurrentWeaponRight.IkTarget = AvatarIKGoal.LeftHand;
                    }
                    else if (weaponData.holderBone == HumanBodyBones.LeftHand)
                    {
                        SetLeftHand(weaponData);
                        CurrentWeaponLeft.IkTarget = AvatarIKGoal.RightHand;
                    }
                }

                UpdateExistingIkReference();

                waitForNextFire = false;
            }
            else if (itemData is ThrowableItem)
            {
                Fighter.SetAction(FighterAction.SwitchingWeapon);
                Fighter.TriggerOnWeaponEquip(itemData as ThrowableItem);

                CurrentThrowableItemObject.throwableItemModel.SetActive(false);
                CurrentThrowableItemObject.ParentShooterFighter = this;
                EquipThrowableAmmo();
            }
        }
        private void SetRightHand(ShooterWeapon weaponData)
        {
            SetWeapon(CurrentWeaponRight, rightHandAimController, weaponData, rightHandForward, rightHandUp,
                      HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.RightShoulder,
                      HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LeftShoulder);
        }

        private void SetLeftHand(ShooterWeapon weaponData)
        {
            SetWeapon(CurrentWeaponLeft, leftHandAimController, weaponData, leftHandForward, leftHandUp,
                      HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LeftShoulder,
                      HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.RightShoulder);
        }

        private void SetWeapon(ShooterWeaponObject currentWeapon, ShootingAimController aimController, ShooterWeapon weaponData, DirectionAxis handForward, DirectionAxis handUp,
                        HumanBodyBones upperArm, HumanBodyBones lowerArm, HumanBodyBones hand, HumanBodyBones shoulder,
                        HumanBodyBones supportUpperArm, HumanBodyBones supportLowerArm, HumanBodyBones supportHand, HumanBodyBones supportShoulder)
        {
            if (aimController != null)
                aimController.DestroyAimHelpers();

            currentWeapon.itemData = weaponData;
            currentWeapon.gameObject.SetActive(false);

            currentWeapon.aimController = new ShootingAimController(Animator, handForward, handUp, upperArm, lowerArm, hand, shoulder, supportHand, supportLowerArm, supportUpperArm, supportShoulder, 1f, 1f, 1f, CurrentShooterWeaponObject.aimReference);

            currentWeapon.ParentShooterFighter = this;

            if (!currentWeapon.AlreadyUsed)
            {
                currentWeapon.SetAmmo();
                currentWeapon.ReloadAmmo(weaponData.GetAttributeValue<int>("Magazine Size"));
            }
            else if (currentWeapon.CurrentAmmoCount != 0)
            {
                currentWeapon.SetAmmo();
            }

            currentWeapon.AlreadyUsed = true;

            // Update external aimController reference
            aimController = currentWeapon.aimController;
            currentWeapon.OnEquip?.Invoke();
        }

        public void UpdateExistingIkReference()
        {
            if(characterID == null) return; 

            existingArmIKReference = CurrentWeapon.iKReferences.Find(
                    r => r.characterReferences != null &&
                    r.characterReferences.Count > 0 &&
                    r.characterReferences.FirstOrDefault(c => c.charcaterId == characterID.id || c.avatar == Animator.avatar) != null);
        }

        #endregion

        #region UnEquip Weapon

        public void UnEquipItem()
        {
            if (CurrentWeapon != null)
            {
                //if (CurrentShooterState != ShooterStates.Idle && CurrentShooterState != ShooterStates.Aim) return;


                if (CurrentWeapon.isDualItem)
                {
                    if (CurrentWeaponLeft != null)
                    {
                        CurrentWeaponLeft.OnUnEquip?.Invoke();
                        CurrentWeaponLeft.ParentShooterFighter = null;
                    }
                    if (CurrentWeaponRight != null)
                    {
                        CurrentWeaponRight.OnUnEquip?.Invoke();
                        CurrentWeaponRight.ParentShooterFighter = null;
                    }
                }
                else
                {
                    if (CurrentShooterWeaponObject != null)
                    {
                        CurrentShooterWeaponObject.OnUnEquip?.Invoke();
                        CurrentShooterWeaponObject.ParentShooterFighter = null;
                    }

                }

                //OnStopAim?.Invoke();
                //animator.SetBool(AnimatorParameters.IsAiming, false);
                StopAiming();
                //if (IsAiming) animGraph.StopLoopingAnimations(true);
                OnShooterWeaponUnEquipAction?.Invoke();
                enableIk = false;

                Fighter.SetAction(FighterAction.SwitchingWeapon);
                Fighter.TriggerOnWeaponEquip(CurrentWeapon);

                existingArmIKReference = null;
            }
            else if (CurrentThrowableItem != null)
            {
                //OnStopAim?.Invoke();
                OnThrowableItemUnEquipAction?.Invoke();
                //animator.SetBool(AnimatorParameters.IsAiming, false);
                StopAiming();
                Fighter.SetAction(FighterAction.SwitchingWeapon);
                Fighter.TriggerOnWeaponEquip(CurrentThrowableItem);
                CurrentThrowableItemObject.throwableItemModel.SetActive(true);
            }
        }

        void DisableCurrentWeapon()
        {
            if (CurrentWeapon.isDualItem)
            {
                rightHandAimController?.DestroyAimHelpers();
                CurrentWeaponRight.aimController = null;

                leftHandAimController?.DestroyAimHelpers();
                CurrentWeaponLeft.aimController = null;
            }
            else
            {
                CurrentShooterWeaponObject.aimController?.DestroyAimHelpers();
                CurrentShooterWeaponObject.aimController = null;
            }
        }

        #endregion

        #region IK

        private void LateUpdate()
        {
            if (IsShooterWeaponEquipped && !IsChangingWeapon && !itemEquipper.IsCurrentItemUnusable)
            {
                UpdateAim();
            }
        }
        public void UpdateAim()
        {
            if (IsAiming && (!IsReloading || CurrentShooterWeaponObject.dontBreakAimingWhileReload) && CanAim)
            {
                RotateBodyTowardsTarget(TargetAimPoint);
                // Force weapon forward if in scope mode without canvas-based scope rendering
                if (IsInScopeView && !CurrentShooterWeaponObject.useCanvasImageForScope)
                {
                    CurrentShooterWeaponObject.transform.forward =
                        (TargetAimPoint - CurrentShooterWeaponObject.transform.position).normalized;
                }
                if (CurrentWeapon.isDualItem)
                {
                    if (!IsShooting)
                    {
                        var ikData = existingArmIKReference != null ? existingArmIKReference.aimIKData : CurrentWeapon.aimIKData;

                        // Update IK targets for both weapons (left and right)
                        UpdateHandIkTargetPositionAndRotation(CurrentWeaponLeft.holdHandIkTarget, CurrentWeaponLeft.holdHandIkSetupHelperTarget, leftHand, ikData.holdHandPosition, ikData.holdHandRotation, true);
                        UpdateHandIkTargetPositionAndRotation(CurrentWeaponRight.holdHandIkTarget, CurrentWeaponRight.holdHandIkSetupHelperTarget, rightHand, ikData.holdHandPosition, ikData.holdHandRotation, true);
                    }

                    // Align both arms towards the aim point
                    leftHandAimController.AlignBones(TargetAimPoint, IsShooting, false, 100);
                    rightHandAimController.AlignBones(TargetAimPoint, IsShooting, false, 100);
                }
                else
                {
                    if (!IsShooting && !IsInScopeView)
                    {
                        UpdateHandIkTarget(true);
                    }
                    
                    var holdHandRot = IkSetupMode? (CurrentShooterWeaponObject.holdHandIkTarget.localEulerAngles - CurrentShooterWeaponObject.holdHandIkSetupHelperTarget.localEulerAngles) : (existingArmIKReference != null
                                    ? existingArmIKReference.aimIKData.holdHandRotation
                                    : CurrentWeapon.aimIKData.holdHandRotation);
                    var handDefaultRot = CurrentWeapon.holderBone == HumanBodyBones.RightHand ? rightHand.eulerAngles : leftHand.eulerAngles;
                    
                    // Align weapon bones and apply IK
                    CurrentShooterWeaponObject.aimController.AlignBones(
                        TargetAimPoint, IsShooting, false, 100,
                        CurrentShooterWeaponObject.holdHandIkTarget.position,
                        CurrentShooterWeaponObject.holdHandIkTarget.eulerAngles,
                        applyHandIK: !IsInScopeView
                    );

                    
                    ApplyArmIK();
                }
                
            }
            else
            {
                ApplyBoneRotations(0, 0);

                if (!CurrentWeapon.isDualItem)
                {
                    if (!IsShooting)
                    {
                        UpdateHandIkTarget(false);
                    }
                    if(CurrentWeapon.UseHandIkForIdle)
                        ApplyArmIK();
                    ApplyArmIK(false);
                }
            }
        }
        public void UpdateHandIkTarget(bool isAiming)
        {
            Transform supportHandSource = (CurrentWeapon.holderBone == HumanBodyBones.RightHand) ? leftHand : rightHand;
            Transform holdHandSource = (CurrentWeapon.holderBone == HumanBodyBones.RightHand) ? rightHand : leftHand;

            var ikData = existingArmIKReference != null
                        ? (isAiming ? existingArmIKReference.aimIKData : existingArmIKReference.idleIKData)
                        : (isAiming ? CurrentWeapon.aimIKData : CurrentWeapon.idleIKData);

            UpdateHandIkTargetPositionAndRotation(CurrentShooterWeaponObject.supportHandIkTarget, CurrentShooterWeaponObject.supportHandIkSetupHelperTarget, supportHandSource, ikData.supportHandPosition, ikData.supportHandRotation);
            UpdateHandIkTargetPositionAndRotation(CurrentShooterWeaponObject.holdHandIkTarget, CurrentShooterWeaponObject.holdHandIkSetupHelperTarget, holdHandSource, ikData.holdHandPosition, ikData.holdHandRotation, true);
        }


        /// <summary>
        /// Updates a hand IK target's position and rotation with given offsets.
        /// </summary>
        private void UpdateHandIkTargetPositionAndRotation(Transform ikTarget, Transform ikSetupTarget, Transform sourceHand, Vector3 positionOffset, Vector3 rotationOffset, bool isHoldHand = false)
        {
            var targetSelected = false;
#if UNITY_EDITOR
            var supportTargetSelected = Equals(Selection.activeObject,CurrentShooterWeaponObject.supportHandIkTarget.gameObject);
            var holdTargetSelected = Equals(Selection.activeObject, CurrentShooterWeaponObject.holdHandIkTarget.gameObject);
            
            targetSelected = supportTargetSelected || holdTargetSelected;
#endif
            if (targetSelected && IkSetupMode)
            {
                ikSetupTarget.position = sourceHand.position;
                ikSetupTarget.eulerAngles = sourceHand.eulerAngles;

                //if(isHoldHand)
                //    ikTarget.eulerAngles = sourceHand.eulerAngles;
            }
            else
            {
                ikTarget.position = sourceHand.position;
                ikTarget.eulerAngles = sourceHand.eulerAngles;
                ikTarget.localPosition += positionOffset;
                ikTarget.localEulerAngles += rotationOffset;

                ikSetupTarget.position = sourceHand.position;
                ikSetupTarget.eulerAngles = sourceHand.eulerAngles;
            }
        }

        void ApplyArmIK(bool applyToSupportArm = true)
        {
            if (!enableIk || IsChangingWeapon || itemEquipper.IsCurrentItemUnusable || IsReloading || IsLoadingAmmo || Fighter.IsDead) return;

            Vector3 targetPos = Vector3.zero;
            Vector3 targetRot = Vector3.zero;
            if (applyToSupportArm)
            {
                targetPos = CurrentShooterWeaponObject.supportHandIkTarget.position;
                targetRot = CurrentShooterWeaponObject.supportHandIkTarget.eulerAngles;
            }
            else
            {
                targetPos = CurrentShooterWeaponObject.holdHandIkTarget.position;
                targetRot = CurrentShooterWeaponObject.holdHandIkTarget.eulerAngles;
            }
            CurrentShooterWeaponObject.aimController.ApplyArmIK(targetPos, targetRot, applyToSupportArm);
        }

        #region Body Rotation

        private Quaternion currentSpineRot;
        private Quaternion currentChestRot;
        private Quaternion currentUpperChestRot;

        private void RotateBodyTowardsTarget(Vector3 targetPoint)
        {
            spineOriginal = spine.localRotation;
            chestOriginal = chest.localRotation;
            upperChestOriginal = upperChest.localRotation;


            Vector3 directionToTarget = targetPoint - spine.position;
            Vector3 horizontalDirection = directionToTarget;
            horizontalDirection.y = 0;

            // Check if we have a valid direction before proceeding
            if (horizontalDirection.magnitude > 0.001f)  // Avoid zero vector
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection, Vector3.up);
                float verticalAngle = Vector3.SignedAngle(
                    horizontalDirection.normalized,
                    directionToTarget.normalized,
                    transform.right
                );

                Quaternion worldToLocal = Quaternion.Inverse(transform.rotation);
                float localYRotation = (worldToLocal * targetRotation).eulerAngles.y;
                if (localYRotation > 180f) localYRotation -= 360f;

                // Check if angles are within limits
                bool isWithinVerticalLimit = Mathf.Abs(verticalAngle) <= maxVerticalAngle;
                bool isWithinHorizontalLimit = Mathf.Abs(localYRotation) <= maxHorizontalAngle;

                if (isWithinVerticalLimit && isWithinHorizontalLimit)
                {
                    // Apply normal rotation when within limits
                    verticalAngle = Mathf.Clamp(verticalAngle, -maxVerticalAngle, maxVerticalAngle);
                    localYRotation = Mathf.Clamp(localYRotation, -maxHorizontalAngle, maxHorizontalAngle);
                    ApplyBoneRotations(verticalAngle, localYRotation);
                }
                else
                {
                    // Reset to default rotations when outside limits
                    ApplyBoneRotations(0, 0);
                }
            }
            else
            {
                // Reset if target is too close or direction is invalid
                ApplyBoneRotations(0, 0);
            }
        }
        private void ApplyBoneRotations(float verticalAngle, float localYRotation)
        {
            float smoothSpeed = rotationSpeed * Time.deltaTime;

            //if (spine)
            //{
            //    Quaternion spineRotation = Quaternion.Euler(
            //        verticalAngle * spineWeight,
            //        localYRotation * spineWeight,
            //        0f
            //    );
            //    Quaternion targetRotation = spineOriginal * spineRotation;
            //    currentSpineRot = Quaternion.Lerp(currentSpineRot, targetRotation, smoothSpeed);
            //    spine.localRotation = currentSpineRot;
            //}

            //if (chest)
            //{
            //    Quaternion chestRotation = Quaternion.Euler(
            //        verticalAngle * chestWeight,
            //        localYRotation * chestWeight,
            //        0f
            //    );
            //    Quaternion targetRotation = chestOriginal * chestRotation;
            //    currentChestRot = Quaternion.Lerp(currentChestRot, targetRotation, smoothSpeed);
            //    chest.localRotation = currentChestRot;
            //}

            if (upperChest)
            {
                Quaternion upperChestRotation = Quaternion.Euler(
                    verticalAngle * upperChestWeight,
                    localYRotation * upperChestWeight,
                    0f
                );
                Quaternion targetRotation = upperChestOriginal * upperChestRotation;
                currentUpperChestRot = Quaternion.Lerp(currentUpperChestRot, targetRotation, smoothSpeed);
                upperChest.localRotation = currentUpperChestRot;
            }
        }

        #endregion

        #endregion



        public void ExitFromShooterSystem()
        {
            if (IsAiming)
                StopAiming();

            //Fighter.SetAction(FighterAction.None);
        }

    }
}


