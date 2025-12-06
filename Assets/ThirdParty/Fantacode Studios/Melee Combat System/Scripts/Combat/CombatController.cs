using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

namespace FS_CombatSystem
{

    public enum TargetSelectionCriteria { DirectionAndDistance, Direction, Distance }

    public class CombatController : EquippableSystemBase
    {
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float rotationSpeed = 500f;

        [ShowEquipmentDropdown]
        [SerializeField] int meleeWeaponSlot;

        [Tooltip("Criteria used for selecting the target enemy the player should attack")]
        [SerializeField] TargetSelectionCriteria targetSelectionCriteria;

        [Tooltip("Increase this value if direction should be given more weight than distance for selecting the target. If distance should be given more weight, then decrease it.")]
        [HideInInspector] public float directionScaleFactor = 0.1f;

        public FighterCore fighterCore { get; private set; }
        public MeleeFighter meleeFighter { get; private set; }
        public Vector3 InputDir { get; private set; }

        Animator animator;
        AnimGraph animGraph;
        CharacterController characterController;
        MeleeCombatInputManager inputManager;
        CombatInputManger combatInputManger;
        LocomotionICharacter locomotionICharacter;
        Damagable damagable;
        PlayerController playerController;
        CombatAIController targetEnemy;
        ItemEquipper itemEquipper;
        ItemAttacher itemAttacher;

        List<Collider> colliders = new List<Collider>();
        //bool isGrounded;
        float ySpeed;
        Quaternion targetRotation;
        bool combatMode;
        bool prevCombatMode;
        float _moveSpeed;
        bool inAction;

        public override List<Type> EquippableItems => new List<Type>() { typeof(MeleeWeapon) };
        public override SystemState State => SystemState.Combat;

        public override void OnResetFighter()
        {
            locomotionICharacter.PreventAllSystems = false;
            characterController.enabled = true;
            colliders.ForEach(c => c.enabled = true);

            fighterCore.ResetFighter();
        }

        public bool CombatMode
        {
            get => combatMode;
            set
            {
                combatMode = value;
                if (fighterCore.Target == null)
                    combatMode = false;

                if (prevCombatMode != combatMode)
                {
                    if (combatMode)
                    {
                        locomotionICharacter.OnStartSystem(this);
                    }
                    else if (!fighterCore.IsBusy)
                    {
                        locomotionICharacter.OnEndSystem(this);
                    }
                    prevCombatMode = combatMode;
                }

                animator?.SetBool(AnimatorParameters.combatMode, combatMode);
            }
        }

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            locomotionICharacter = GetComponent<LocomotionICharacter>();
            damagable = GetComponent<Damagable>();
            characterController = GetComponent<CharacterController>();

            characterController.excludeLayers = LayerMask.GetMask("Hitbox");

            fighterCore = GetComponent<FighterCore>();
            meleeFighter = GetComponent<MeleeFighter>();
            inputManager = GetComponent<MeleeCombatInputManager>();
            combatInputManger = GetComponent<CombatInputManger>();
            locomotionICharacter = GetComponent<LocomotionController>();
            animGraph = GetComponent<AnimGraph>();
            itemEquipper = GetComponent<ItemEquipper>();
            itemAttacher = GetComponent<ItemAttacher>();

            _moveSpeed = moveSpeed;
        }

        private void Start()
        {
            animator = locomotionICharacter.Animator;

            inputManager.OnAttackPressed += OnAttackPressed;

            fighterCore.OnGotHit += (FighterCore attacker, Vector3 hitPoint, float hittingTime, bool isBlockedHit, HitType hitType) =>
            {
                if (itemEquipper.EquippedItem is MeleeWeapon && hitType == HitType.Melee)
                {
                    locomotionICharacter.OnStartSystem(this);
                    CombatMode = true;
                    inAction = true;
                    var meleeFighter = attacker.GetComponent<MeleeFighter>();
                    playerController.OnStartCameraShake.Invoke(meleeFighter.CurrAttack.CameraShakeAmount, meleeFighter.CurrAttack.CameraShakeDuration);
                }
            };

            meleeFighter.OnAttackHit += (attackData, targetFighter) =>
            {
                playerController.OnStartCameraShake.Invoke(attackData.CameraShakeAmount, attackData.CameraShakeDuration);
            };

            fighterCore.OnTargetChanged += (FighterCore target) =>
            {
                if (!itemEquipper.IsCurrentItemUnusable && itemEquipper.EquippedItem is MeleeWeapon)
                    CombatMode = target != null;
            };

            meleeFighter.OnAttack += (target) =>
            {
                locomotionICharacter.UseRootMotion = true;
                locomotionICharacter.OnStartSystem(this);
                itemEquipper.PreventItemSwitching = true;
                if (target != null && !combatMode) CombatMode = true;
                inAction = true;
            };

            fighterCore.OnDeathAnimationCompleted += Death;
            fighterCore.OnWeaponEquipAction += (CombatWeapon weapon) =>
            {
                if (weapon.OverrideMoveSpeed)
                {
                    moveSpeed = weapon.CombatMoveSpeed;
                }
            };
            fighterCore.OnWeaponUnEquipAction += (CombatWeapon weapon) =>
            {
                locomotionICharacter.ResetMoveSpeed();
                moveSpeed = _moveSpeed;
                if (IsInFocus)
                    locomotionICharacter.OnEndSystem(this);
            };

            fighterCore.OnStartAction += StartSystem;
            fighterCore.OnEndAction += EndSystem;

            itemAttacher.DefaultItem = fighterCore.defaultWeapon;
        }

        void StartSystem()
        {
            if (fighterCore.Action != FighterAction.SwitchingWeapon && playerController.CurrentEquippedSystem?.State == SystemState.Combat
                && (playerController.CurrentSystemState == SystemState.Combat || playerController.CurrentSystemState == SystemState.Locomotion))
            {
                if (!fighterCore.CanDoActions || fighterCore.Action == FighterAction.Dodging)
                {
                    locomotionICharacter.UseRootMotion = true;
                    locomotionICharacter.OnStartSystem(this);
                    itemEquipper.PreventItemSwitching = true;
                    inAction = true;
                }
            }
        }

        void EndSystem()
        {
            if (playerController.CurrentSystemState != SystemState.Combat) return;

            if (!combatMode)
                locomotionICharacter.OnEndSystem(this);
            locomotionICharacter.UseRootMotion = false;
            itemEquipper.PreventItemSwitching = false;
            inAction = false;
        }

        void OnAttackPressed(float holdTime, bool isHeavyAttack, bool isCounter, bool isCharged, bool isSpecialAttack)
        {
            if (locomotionICharacter.PreventAllSystems || (playerController.CurrentEquippedSystem != null && playerController.CurrentEquippedSystem != this)
                || (playerController.CurrentSystemState != SystemState.Locomotion && playerController.CurrentSystemState != SystemState.Combat && playerController.CurrentSystemState != SystemState.Cover))
                return;
            if (fighterCore.Target == null && isCounter && !MeleeCombatSettings.i.SameInputForAttackAndCounter) return;

            if (locomotionICharacter.IsGrounded && !fighterCore.IsDead)
            {
                var dirToAttack = locomotionICharacter.MoveDir == Vector3.zero ? transform.forward : locomotionICharacter.MoveDir;

                var enemyToAttack = GetEnemyToTarget(dirToAttack);
                fighterCore.Target = enemyToAttack != null ? enemyToAttack.Fighter : null;

                if (MeleeCombatSettings.i.SameInputForAttackAndCounter)
                {
                    if (fighterCore.IsBeingAttacked && fighterCore.CurrAttacker.GetComponent<MeleeFighter>().AttackState == AttackStates.Windup)
                        isCounter = true;
                    else
                        isCounter = false;
                }

                meleeFighter.TryToAttack(fighterCore.Target, isHeavyAttack: isHeavyAttack, isCounter: isCounter, isCharged: isCharged, isSpecialAttack: isSpecialAttack);
            }
        }

        void Death()
        {
            characterController.enabled = false;
            //colliders = GetComponentsInChildren<Collider>().ToList().Where(c => c.enabled).ToList();
            //foreach (var collider in colliders)
            //    collider.enabled = false;

            locomotionICharacter.UseRootMotion = false;
            locomotionICharacter.PreventAllSystems = true;
            locomotionICharacter.OnEndSystem(this);
        }

        public Vector3 GetTargetingDir()
        {
            if (!CombatMode)
            {
                var vecFromCam = transform.position - Camera.main.transform.position;
                vecFromCam.y = 0f;
                return vecFromCam.normalized;
            }
            else
            {
                return transform.forward;
            }
        }

        private void Update()
        {

            // Equip
            if (combatInputManger.Equip)
            {
                var attachedWeapon = itemAttacher.GetAttachedItem(meleeWeaponSlot) as MeleeWeapon;
                if (attachedWeapon != null)
                    itemEquipper.EquipItem(attachedWeapon, true, onItemEnabled: () => itemAttacher.EquipItem(attachedWeapon));
            }
        }

        public override void HandleUpdate()
        {
            if (fighterCore.Target != null && fighterCore.Target.IsDead)
                FindNewTarget();

            if (fighterCore.CurrentWeapon == null || fighterCore.Damagable.CurrentHealth <= 0)
            {
                //CombatMode = false;
                return;
            }

            if (inputManager.CombatMode && !fighterCore.IsBusy)
            {
                if (!combatMode)
                {
                    // Set target if target is null
                    if (fighterCore.Target == null)
                        fighterCore.Target = GetEnemyToTarget(transform.forward)?.Fighter;

                    if (!combatMode)    // combat mode might have been set to true if target changed
                        CombatMode = true;
                }
                else
                    CombatMode = false;
            }

            fighterCore.IsBlocking = inputManager.Block && (!fighterCore.IsBusy || fighterCore.Action == FighterAction.TakingBlockedHit) && fighterCore.CurrentWeapon.CanBlock;


            if (meleeFighter.canDodge && fighterCore.CanDodge && combatInputManger.Dodge && !fighterCore.IsBusy)
            {
                if (!IsInFocus) return;
                if (!fighterCore.OnlyDodgeInCombatMode || CombatMode)
                    StartCoroutine(fighterCore.Dodge(locomotionICharacter.MoveDir));
                return;
            }

            if (meleeFighter.canRoll && fighterCore.CanRoll && combatInputManger.Roll && !fighterCore.IsBusy)
            {
                if (!IsInFocus) return;

                if (!fighterCore.OnlyRollInCombatMode || CombatMode)
                    StartCoroutine(fighterCore.Roll(locomotionICharacter.MoveDir));
                return;
            }

            // UnEquip
            if (combatInputManger.UnEquip)
            {
                var equippedItem = itemEquipper.EquippedItem as MeleeWeapon;
                if (equippedItem != null)
                {
                    itemEquipper.EquipItem(fighterCore.defaultWeapon, onPrevItemDisabled: () => itemAttacher.UnEquipItem(equippedItem));
                }
            }

            if (!CombatMode)
            {
                fighterCore.ApplyAnimationGravity(locomotionICharacter.CheckIsGrounded(), IsInFocus);
                return;
            }

            if (fighterCore != null && inAction)
            {
                targetRotation = transform.rotation;
                animator.SetFloat(AnimatorParameters.moveAmount, 0f);
                fighterCore.ApplyAnimationGravity(locomotionICharacter.CheckIsGrounded(), IsInFocus);
                return;
            }

            float h = locomotionICharacter.MoveDir.x;
            float v = locomotionICharacter.MoveDir.z;

            float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

            var moveDir = locomotionICharacter.MoveDir;
            InputDir = locomotionICharacter.MoveDir;

            var velocity = moveDir * moveSpeed;

            // Rotate and face the target enemy
            Vector3 targetVec = transform.forward;
            if (fighterCore.Target != null)
            {
                targetVec = fighterCore.Target.transform.position - transform.position;
                targetVec.y = 0;
            }

            if (moveAmount > 0)
            {
                targetRotation = Quaternion.LookRotation(targetVec);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    rotationSpeed * Time.deltaTime);
            }


            // Split the velocity into it's forward and sideward component and set it into the forwardSpeed and strafeSpeed
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            animator.SetFloat(AnimatorParameters.moveAmount, forwardSpeed / moveSpeed, 0.2f, Time.deltaTime);

            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
            float strafeSpeed = Mathf.Sin(angle * Mathf.Deg2Rad);
            animator.SetFloat(AnimatorParameters.strafeAmount, strafeSpeed, 0.2f, Time.deltaTime);


            if (meleeFighter.CurrentMeleeWeapon.UseRootmotion)
            {
                velocity = animator.deltaPosition;
                transform.rotation *= animator.deltaRotation;
            }
            else
                velocity = velocity * Time.deltaTime;


            velocity.y = 0;
            (moveDir, velocity) = locomotionICharacter.LedgeMovement(moveDir.normalized, velocity);
            velocity.y = ySpeed * Time.deltaTime;

            if (locomotionICharacter.CheckIsGrounded()) ySpeed = -0.5f;
            ySpeed += Physics.gravity.y * Time.deltaTime;
            if (!fighterCore.StopMovement)
                characterController.Move(velocity);
        }

        public void FindNewTarget()
        {
            var enemyToTarget = GetEnemyToTarget(transform.forward);
            fighterCore.Target = enemyToTarget?.Fighter;
        }

        public CombatAIController GetEnemyToTarget(Vector3 direction)
        {
            float minDistance = Mathf.Infinity;
            float minAngle = Mathf.Infinity;
            float minSum = Mathf.Infinity;
            CombatAIController closestTarget = null;

            // remove any dead enemies form the target
            fighterCore.TargetsInRange.RemoveAll(t => t == null || t.gameObject == null || !t.gameObject.activeSelf || t.Damagable.CurrentHealth <= 0);

            foreach (var target in fighterCore.TargetsInRange.Select(t => CombatAILookup.Get(t)))
            {
                var vecToEnemy = target.transform.position - transform.position;
                vecToEnemy.y = 0;

                if (TargetSelectionCriteria == TargetSelectionCriteria.Direction)
                {
                    float angle = Vector3.Angle(direction, vecToEnemy);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        closestTarget = target;
                    }
                }
                else if (TargetSelectionCriteria == TargetSelectionCriteria.Distance)
                {
                    float distance = target.DistanceToTarget;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestTarget = target;
                    }
                }
                else if (TargetSelectionCriteria == TargetSelectionCriteria.DirectionAndDistance)
                {
                    float weightedSum = target.DistanceToTarget + (Vector3.Angle(direction, vecToEnemy) * directionScaleFactor);
                    if (weightedSum < minSum)
                    {
                        minSum = weightedSum;
                        closestTarget = target;
                    }
                }
            }

            return closestTarget;
        }


        public override void HandleOnAnimatorMove(Animator animator)
        {
            if (locomotionICharacter.UseRootMotion && !fighterCore.StopMovement)
            {
                //if (meleeFighter.IsDead)
                //    Debug.Log("Using root motion for death - Matching target - " + meleeFighter.IsMatchingTarget);
                transform.rotation *= animator.deltaRotation;

                var deltaPos = animator.deltaPosition;

                if (fighterCore.IsMatchingTarget)
                    deltaPos = fighterCore.MatchingTargetDeltaPos;
                var (newDeltaDir, newDelta) = locomotionICharacter.LedgeMovement(deltaPos.normalized, deltaPos);
                if (locomotionICharacter.IsOnLedge) return;

                if (fighterCore.IgnoreCollisions || characterController == null || !characterController.enabled)
                    transform.position += newDelta;
                else
                    characterController.Move(newDelta);
            }
        }

        public TargetSelectionCriteria TargetSelectionCriteria => targetSelectionCriteria;

        public override void ExitSystem()
        {
            if (fighterCore.IsBlocking)
                fighterCore.IsBlocking = false;

            CombatMode = false;
        }
    }
}