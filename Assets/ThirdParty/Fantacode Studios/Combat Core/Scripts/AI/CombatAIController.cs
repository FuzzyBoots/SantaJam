using FS_Core;
using FS_ThirdPerson;
using FS_Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;

namespace FS_CombatCore
{

    public enum AIStates { Idle, CombatMovement, Attack, RetreatAfterAttack, Dead, GettingHit, Chase, Cover, Shoot, Regroup, Flank, AuthoredBehavior }
    public enum AICombatType { Melee, Ranged }

    [FoldoutGroup("Block and Dodge Settings", false, "chanceForBlockingAttack", "canInteruptActionToBlock", "chanceForDodgingAttack", "canInteruptActionToDodge")]
    [FoldoutGroup("Detection Settings", false, "detectionRayOriginOffset", "detectionRayTargetOffset", "detectionRayTargetInCoverOffset")]
    [MovedFrom("EnemyController")]
    public class CombatAIController : MonoBehaviour
    {
        [field: Tooltip("Field of view angle (in degrees) for AI perception. Determines how wide the AI can see targets.")]
        [field: SerializeField] public float Fov { get; private set; } = 180f;

        [field: Tooltip("The maximum distance at which the AI can alert nearby allies or detect threats.")]
        [field: SerializeField] public float AlertRange { get; private set; } = 20f;

        [field: Tooltip("Movement speed when the AI is walking (non-combat).")]
        [field: SerializeField] public float WalkSpeed { get; private set; } = 2f;

        [field: Tooltip("Movement speed when the AI is running (non-combat).")]
        [field: SerializeField] public float RunSpeed { get; private set; } = 4.5f;

        [field: Tooltip("Movement speed when the AI is in combat mode.")]
        [field: SerializeField] public float CombatModeSpeed { get; private set; } = 2f;

        [Tooltip("The combat type of this AI (Melee or Ranged).")]
        [SerializeField] AICombatType aiCombatType;

        [Tooltip("If true, the AI will not be able to move out of a certain range while in combat. Useful to create enemies that defend a certain area.")]
        [SerializeField] bool hasMovementRange = false;

        [Tooltip("The maximum distance the AI can move from its initial position while in combat. Only used if 'hasMovementRange' is true.")]
        [ShowIf("hasMovementRange", true)]
        [SerializeField] float movementRange = 15f;

        [Tooltip("If true, the AI will drop its weapon upon death.")]
        [SerializeField] bool dropWeaponOnDeath = false;

        [Tooltip("Chance (0-100) that the AI will attempt to block an incoming attack.")]
        [Range(0, 100f)]
        [SerializeField] float chanceForBlockingAttack = 0f;

        [Tooltip("If true, the AI can block attacks even while busy with another action. If false, the AI must not be busy to block.")]
        [SerializeField] bool canInteruptActionToBlock = true;

        [Tooltip("Chance (0-100) that the AI will attempt to dodge an incoming attack.")]
        [Range(0, 100f)]
        [SerializeField] float chanceForDodgingAttack = 0f;

        [Tooltip("If true, the AI can dodge attacks even while busy with another action. If false, the AI must not be busy to dodge.")]
        [SerializeField] bool canInteruptActionToDodge = true;

        [Tooltip("Vertical offset for the origin of detection rays (used for vision checks).")]
        [SerializeField] float detectionRayOriginOffset = 1.3f;

        [Tooltip("Vertical offset for the target's head position when performing detection checks.")]
        [SerializeField] float detectionRayTargetOffset = 1.5f;

        [Tooltip("Vertical offset for the target's head position when the target is in cover.")]
        [SerializeField] float detectionRayTargetInCoverOffset = 1f;

        [Tooltip("Interval (in seconds) at which the AI recalculates the distance to its target.")]
        [SerializeField] float distanceCalculationInterval = 0.1f;

        public bool HasMovementRange => hasMovementRange;
        public float GetMovementRange() => hasMovementRange ? movementRange : float.MaxValue;
        public Vector3 InitialPosition { get; private set; }

        public bool DropWeaponOnDeath => dropWeaponOnDeath;

        public float CombatMovementTimer { get; set; } = 0f;
        public float TimeSinceLastAttack { get; private set; } = 0f;

        public StateMachine<CombatAIController> StateMachine { get; private set; }

        public Dictionary<AIStates, State<CombatAIController>> stateDict = new Dictionary<AIStates, State<CombatAIController>>();

        public event Action<AIStates> OnStateChanged;

        public NavMeshAgent NavAgent { get; private set; }
        public IAICharacter iAICharacter { get; private set; }
        public CharacterController CharacterController { get; private set; }
        public Animator Animator { get; private set; }
        public FighterCore Fighter { get; private set; }
        public BoneMapper BoneMapper { get; private set; }

        Damagable damagable;

        public LayerMask obstacleMask = 1;

        public Action OnSelectedAsTarget;
        public Action OnRemovedAsTarget;

        float blockingTimer = 0f;

        public AICombatType AICombatType {
            get => aiCombatType;
            set { aiCombatType = value; }
        }

        private void Start()
        {
            Fighter = GetComponent<FighterCore>();

            NavAgent = GetComponent<NavMeshAgent>();
            CharacterController = GetComponent<CharacterController>();
            Animator = GetComponent<Animator>();
            iAICharacter = GetComponent<IAICharacter>();
            BoneMapper = GetComponent<BoneMapper>();
            damagable = GetComponent<Damagable>();

            InitialPosition = transform.position;

            stateDict = new Dictionary<AIStates, State<CombatAIController>>();
            var states = GetComponents<State<CombatAIController>>().ToList();

            foreach (var state in states)
            {
                var stateKey = (state as IAIState)?.StateKey;

                if (stateKey != null)
                    stateDict[stateKey.Value] = state;
                else
                    Debug.LogError("Cannot find stat for " + state.GetType());
            }

            CombatAIManager.i.RegisterCombatAI(this);

            StateMachine = new StateMachine<CombatAIController>(this);
            StateMachine.ChangeState(stateDict[AIStates.Idle]);

            Fighter.OnBeingAttacked += (FighterCore attacker) =>
            {
                if (Fighter.CurrentWeapon == null || Fighter.IsInSyncedAnimation || Fighter.Target == null)
                    return;

                // Only allow blocking if not in another action or CanInteruptActionToBlock is true
                if (Fighter.CurrentWeapon.CanBlock && (canInteruptActionToBlock || !Fighter.IsBusy))
                {
                    if (UnityEngine.Random.Range(1, 101) <= chanceForBlockingAttack)
                    {
                        Fighter.IsBlocking = true;
                        blockingTimer = UnityEngine.Random.Range(1f, 2f);
                    }
                }

                // Only allow dodging if not busy, unless CanInteruptActionToDodge is true
                if (Fighter.CanDodge && Fighter.Action != FighterAction.Dodging && !IsInState(AIStates.Attack) && !Fighter.IsKnockedDown && (canInteruptActionToDodge || !Fighter.IsBusy))
                {
                    if (UnityEngine.Random.Range(1, 101) <= chanceForDodgingAttack)
                        StartCoroutine(DodgeAndAttack());
                }
            };

            Fighter.OnGotHit += (FighterCore attacker, Vector3 hitPoint, float hittingTime, bool isBlockedHit, HitType HitType) =>
            {
                if (Fighter.Damagable.CurrentHealth > 0)
                {
                    if (Fighter.Target == null)
                    {
                        Fighter.Target = attacker;
                        AlertNearbyAllies();
                    }
                    if (!isBlockedHit && !Fighter.CurrReaction.isAdditiveAnimation)
                    {
                        iAICharacter.OnStartAction(false, itemBecomeUnUsable: true);
                        ChangeState(AIStates.GettingHit);
                    }
                }
                else
                {
                    iAICharacter.IsBusy = true;
                    ChangeState(AIStates.Dead);
                }
            };

            Fighter.OnGotHitCompleted += () =>
            {
                if (Fighter.Action != FighterAction.TakingHit && Fighter.Action != FighterAction.TakingBlockedHit)
                {
                    iAICharacter.OnEndAction();
                    iAICharacter.IsBusy = false;
                }
            };
            Fighter.OnDeathAnimationCompleted += () =>
            {
                iAICharacter.IsBusy = false;
            };

            Fighter.OnWeaponEquipAction += (CombatWeapon weaponData) =>
            {
                if (weaponData.OverrideMoveSpeed) 
                {
                    //WalkSpeed = weaponData.WalkSpeed;
                    //RunSpeed = weaponData.RunSpeed;
                    CombatModeSpeed = weaponData.CombatMoveSpeed;
                }
            };

            Fighter.OnResetFighter += () =>
            {
                CharacterController.enabled = true;
                colliders.ForEach(c => c.enabled = true);
                Fighter.StopMovement = false;
            };
            Fighter.OnDeath += Death;

            prevPos = transform.position;

            NavAgent.stoppingDistance = 0f;
        }


        List<Collider> colliders = new List<Collider>();
        public void Death()
        {
            if (CharacterController != null)
                CharacterController.enabled = false;

            var capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
                capsuleCollider.enabled = false;

            //colliders = GetComponentsInChildren<Collider>().ToList().Where(c => c.enabled).ToList();
            //colliders.ForEach(c => c.enabled = false);

            // Fighter.SetRagdollState(true);
            // Fighter.StopMovement = true;
        }

        AIStates currState;
        public AIStates CurrentState => currState;
        public void ChangeState(AIStates state)
        {
            currState = state;
            OnStateChanged?.Invoke(state);
            StateMachine.ChangeState(stateDict[state]);
        }

        public bool IsInState(AIStates state)
        {
            if (!stateDict.ContainsKey(state)) return false;

            return StateMachine.CurrentState == stateDict[state];
        }

        float timer = 0f;
        Vector3 prevPos;
        private void Update()
        {
            if (Time.deltaTime == 0 || iAICharacter.IsBusy) return;

            if (IsInState(AIStates.Attack))
                TimeSinceLastAttack = 0;
            else
                TimeSinceLastAttack += Time.deltaTime;

            if (Fighter.IsBlocking)
            {
                if (blockingTimer > 0f)
                    blockingTimer -= Time.deltaTime;

                if (blockingTimer <= 0f || (Fighter.Action != FighterAction.TakingBlockedHit && Fighter.Action != FighterAction.Blocking))
                    Fighter.IsBlocking = false;
            }

            if (Fighter.IsBusy && Fighter.Action != FighterAction.TakingHit)
            {
                prevPos = transform.position;
                Animator.SetFloat(AnimatorParameters.moveAmount, 0f, 0.2f, Time.deltaTime);
                Animator.SetFloat(AnimatorParameters.strafeAmount, 0f, 0.2f, Time.deltaTime);

                return;
            }

            timer += Time.deltaTime;
            if (Fighter.Target != null && timer >= 1)
            {
                timer = 0f;
                CalculateDistanceToTarget();
            }

            StateMachine.Execute();

            // v = dx / dt
            UpdateAnimatorMovement();

            prevPos = transform.position;
        }
        public void UpdateAnimatorMovement()
        {
            //animator.SetBool(AnimatorParameters.CoverMode, false);
            //navMeshAgent.speed = 3.5f;

            // v = dx / dt
            var deltaPos = transform.position - prevPos;
            var velocity = deltaPos / Time.deltaTime;
            velocity.y = 0;

            var speed = Fighter.Target != null? CombatModeSpeed : RunSpeed;

            Animator.SetFloat(
                AnimatorParameters.moveAmount,
                Vector3.Dot(velocity, transform.forward) / speed,
                0.2f,
                Time.deltaTime
            );
            Animator.SetFloat(
               AnimatorParameters.strafeAmount,
                Vector3.Dot(velocity, transform.right) / speed,
                0.2f,
                Time.deltaTime
            );
        }

        private void OnAnimatorMove()
        {
            if (Fighter == null || Time.timeScale == 0) return;

            if (!Fighter.StopMovement && Fighter.UseRootMotion)
            {
                transform.rotation *= Animator.deltaRotation;
                var deltaPos = Animator.deltaPosition;

                if (Fighter.IsMatchingTarget)
                    deltaPos = Fighter.MatchingTargetDeltaPos;

                if (Fighter.CurrReaction != null && Fighter.CurrReaction.preventFallingFromLedge && LedgeMovement(deltaPos.normalized, deltaPos)) return;

                if (Fighter.IgnoreCollisions || CharacterController == null || !CharacterController.enabled)
                    transform.position += deltaPos;
                else
                    CharacterController.Move(deltaPos);
            }
        }

        IEnumerator DodgeAndAttack()
        {
            yield return Fighter.Dodge(Vector3.zero);
            ChangeState(AIStates.Attack);
        }

        public bool LedgeMovement(Vector3 currMoveDir, Vector3 currVelocity)
        {
            if (currMoveDir == Vector3.zero) return false;

            float yOffset = 1f;
            float xOffset = 0.7f;

            var radius = 0.2f; // can control moveAngle here
            float maxAngle = 60f;

            RaycastHit newHit;
            var positionOffset = transform.position + currMoveDir * xOffset;

            var ledgeHeightThreshold = 1.4f;

            if (!(Physics.SphereCast(positionOffset + Vector3.up * yOffset /* + Vector3.up * radius */, radius, Vector3.down, out newHit, yOffset + ledgeHeightThreshold, obstacleMask)) || ((newHit.distance - yOffset) > ledgeHeightThreshold && Vector3.Angle(Vector3.up, newHit.normal) > maxAngle))
            {
                return true;

            }

            return false;
        }

        public FighterCore FindTarget()
        {
            foreach (var target in Fighter.TargetsInRange)
            {
                var vecToTarget = target.transform.position - transform.position;
                float angle = Vector3.Angle(transform.forward, vecToTarget);

                if (angle <= Fov / 2 && LineOfSightCheck(target))
                {
                    return target;
                }
            }
            return null;
        }

        public bool LineOfSightCheck(FighterCore target)
        {
            if (NavAgent.isOnOffMeshLink) return false;

            var origin = GetDetectionRayOrigin();
            var headPos = GetDetectionRayTarget(target);
            // var bodyPos = target.transform.position + Vector3.up;

            if (!Physics.SphereCast(origin, 0.2f, (headPos - origin), out RaycastHit info, (headPos - origin).magnitude, obstacleMask)) return true;
            // if (!Physics.SphereCast(origin, 0.2f, (bodyPos - origin), out info, (bodyPos - origin).magnitude, obstacleMask)) return true;

            return false;
        }

        public void AlertNearbyAllies()
        {
            var colliders = Physics.OverlapBox(transform.position, new Vector3(AlertRange / 2f, 5f, AlertRange / 2f),
                Quaternion.identity, 1 << gameObject.layer);

            foreach (var collider in colliders)
            {
                if (collider.gameObject == gameObject) continue;

                var nearbyAlly = collider.GetComponent<CombatAIController>();
                if (nearbyAlly != null && nearbyAlly.Fighter.Target == null && Physics.Linecast(Animator.GetBoneTransform(HumanBodyBones.Neck).position, nearbyAlly.Animator.GetBoneTransform(HumanBodyBones.Neck).position))
                {
                    StartCoroutine(AsyncUtil.RunAfterDelay(0.5f, () => nearbyAlly.SetTarget(Fighter.Target)));
                }
            }
        }

        public void SetTarget(FighterCore target)
        {
            if (Fighter.IsDead) return;

            Fighter.Target = target;
            ChangeState(aiCombatType == AICombatType.Melee ? AIStates.CombatMovement : AIStates.Chase);
        }

        public bool IsOutsideMovementRange()
        {
            if (hasMovementRange)
            {
                var vecToInitialPos = transform.position - InitialPosition;
                vecToInitialPos.y = 0; // Ignore height difference
                return vecToInitialPos.magnitude > movementRange;
            }

            return false;
        }

        public float DistanceToTarget { get; private set; }
        public float CalculateDistanceToTarget()
        {
            if (Fighter.Target == null) return 999;

            DistanceToTarget = Vector3.Distance(transform.position, Fighter.Target.transform.position);
            return DistanceToTarget;
        }

        public static List<CombatAITag> combatAITags = new();
        public CombatAITag tagCost { get; set; } = new CombatAITag() { Tag = "", maxCost = 1 };

        public CombatAITag GetTagCost()
        {
            foreach (var combatTag in combatAITags)
            {
                if (combatTag.Tag == tagCost.Tag)
                {
                    return combatTag;
                }
            }
            return combatAITags.LastOrDefault(t => t.Tag == ""); // Return -1 if tag is not found
        }
        public CombatAITag GetTagCost(string tag)
        {
            foreach (var combatTag in combatAITags)
            {
                if (combatTag.Tag == tag)
                {
                    return combatTag;
                }
            }
            return combatAITags.LastOrDefault(t => t.Tag == ""); // Return -1 if tag is not found
        }

        public Vector3 GetDetectionRayOrigin()
        {
            return transform.position + Vector3.up * detectionRayOriginOffset;
        }

        public Vector3 GetDetectionRayTarget(FighterCore target = null)
        {
            if (target == null)
                target = Fighter.Target;

            return target.transform.position + Vector3.up * (target.animator.GetBool(AnimatorParameters.coverMode) ? detectionRayTargetInCoverOffset : detectionRayTargetOffset);
        }

        //public bool CanAttack => GetTagCost().currentCost + tagCost.maxCost <= GetTagCost().maxCost;

        private void OnDisable()
        {
            CombatAIManager.i.RemoveCombatAI(this);
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Handles.Label(transform.position + Vector3.up * 2.2f, currState.ToString(), new GUIStyle() { fontSize = 20 });

            Handles.color = Color.blue;
            if (hasMovementRange)
                Handles.DrawWireDisc(transform.position, transform.up, movementRange);

            Handles.color = Color.red;
            Handles.DrawWireDisc(transform.position, transform.up, AlertRange);
        }
#endif
    }

    public interface IAIState
    {
        public AIStates StateKey { get; }
    }

    [Serializable]
    public class CombatAITag
    {
        public string Tag;
        public float maxCost = 2;
        public float currentCost { get; set; } = 0;
    }
}