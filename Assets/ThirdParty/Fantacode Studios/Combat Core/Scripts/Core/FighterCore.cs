using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.InteropServices.WindowsRuntime;
using FS_Util;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FS_ThirdPerson
{
    public static partial class AnimatorParameters
    {
        public static int combatMode = Animator.StringToHash("combatMode");
        public static int isBlocking = Animator.StringToHash("IsBlocking");
    }
}

namespace FS_CombatCore {

    public enum FighterAction { None, Attacking, Blocking, Dodging, TakingHit, TakingBlockedHit, GettingUp, SwitchingWeapon, Taunt, Shooting, ThrowingObject, Reloading, Other }
    public enum AttackType { Single, Combo, Stealth, GroundAttack }

    public class FighterCore : MonoBehaviour
    {
        [Tooltip("Default weapon of the fighter")]
        public CombatWeapon defaultWeapon;
        [field: SerializeField] public float PreferredFightingRange { get; private set; } = 2f;

        [Tooltip("GameObjects in the target layer will be considered as a target")]
        public LayerMask targetLayer;

        [SerializeField] bool changeTargetWhenTakingHit = true;


        // Maximium range at which the fighter can perform an attack (Will be computed automatically)
        public float MaxAttackRange { get; set; }

        [Header("Optional Parameters")]
        [Space(5)]
        public DefaultReactions defaultAnimations = new DefaultReactions();

        [Tooltip("If true, replaces the default blood effects defined by attacks or weapon data.")]
        public bool overrideBloodEffects = false;
        [Tooltip("Blood effect prefab to be instantiated at the hit point when the fighter takes damage")]
        [ShowIf("overrideBloodEffects", true)]
        public GameObject bloodEffectPrefab;

        [Tooltip("Indicates if the fighter can dodge.")]
        [HideInInspector] public bool CanDodge;

        [HideInInspector] public DodgeData dodgeData;

        [Tooltip("If true, fighter will only be able to dodge in combat mode.")]
        [HideInInspector] public bool OnlyDodgeInCombatMode = true;

        [Tooltip("Indicates if the fighter can roll.")]
        [HideInInspector] public bool CanRoll;
        [HideInInspector] public DodgeData rollData;

        [Tooltip("If true, fighter will only be able to roll in combat mode.")]
        [HideInInspector] public bool OnlyRollInCombatMode = true;

        [Space(10)]

        [HideInInspector] public UnityEvent<CombatWeapon> OnWeaponEquipEvent;
        [HideInInspector] public UnityEvent<CombatWeapon> OnWeaponUnEquipEvent;

        public Action<CombatWeapon> OnWeaponEquipAction;
        public Action<CombatWeapon> OnWeaponUnEquipAction;

        public event Action OnResetFighter;


        public event Action<FighterCore> OnTargetChanged;

        public event Action<FighterCore, Vector3, float, bool, HitType> OnGotHit;
        public Action OnGotHitCompleted;
        [HideInInspector] public UnityEvent<FighterCore, Vector3, float> OnGotHitEvent;

        [HideInInspector] public UnityEvent<FighterCore> OnAttackEvent;
        public Action OnStartAction;
        public Action OnEndAction;


        public event Action OnHitComplete;

        public event Action OnDeath;
        public event Action OnDeathAnimationCompleted;
        [HideInInspector] public UnityEvent OnDeathEvent;

        public event Action OnKnockDown;
        [HideInInspector] public UnityEvent OnKnockDownEvent;

        public event Action OnGettingUp;
        [HideInInspector] public UnityEvent OnGettingUpEvent;

        // States
        public FighterAction Action { get; private set; }
        public bool IsDead { get;  set; }
        public bool IsKnockedDown { get; private set; }
        public bool CanDoActions { get; set; } = true;
        public bool InAction => Action != FighterAction.None && Action != FighterAction.Blocking && Action != FighterAction.Shooting;
        public bool IsBusy => InAction || IsDead || IsKnockedDown;
        public bool InInvinsibleAction => Action == FighterAction.Dodging || Action == FighterAction.GettingUp;
        public bool InAttackableAction => Action == FighterAction.None || Action == FighterAction.Blocking || Action == FighterAction.Attacking;
        public bool UseRootMotion => Action == FighterAction.Attacking || Action == FighterAction.Dodging || Action == FighterAction.TakingHit || Action == FighterAction.TakingBlockedHit || Action == FighterAction.GettingUp;

        public FighterCore CurrAttacker { get; private set; }
        public FighterCore TargetOfAttack { get; set; }
        public Reaction CurrReaction { get; private set; }


        public bool IsMatchingTarget { get; set; } = false;
        public Vector3 MatchingTargetDeltaPos { get; set; } = Vector3.zero;

        public bool IsInSyncedAnimation { get; set; } = false;
        public AnimGraphClipInfo CurrSyncedAction { get; set; }    // To avoid on attack to prevent reseting IsInSyncedAnimation set by another attack

        FighterCore target;
        public FighterCore Target {
            get => target;
            set {
                var prev = target;
                target = value;
                if (target != prev)
                    OnTargetChanged?.Invoke(target);
            }
        }
        public List<FighterCore> TargetsInRange { get; set; } = new List<FighterCore>();
        public bool IsBeingAttacked { get; private set; } = false;
        public bool PlayingBlockAnimationEarlier { get; set; } = false;     // For synced blocks, blocking animation should be played earlier

        public bool IgnoreCollisions { get; set; } // Some attacks might need to ignore collisions
        public bool StopMovement { get; set; } = false;

        int hitCount = 0;


        bool isBlocking;
        public bool IsBlocking {
            get => isBlocking;
            set {
                bool wasPreviouslyBlocking = isBlocking;
                isBlocking = value;
                HandleBlockingChanged(wasPreviouslyBlocking);
            }
        }

        public CombatWeapon CurrentWeapon {
            get {
                return (itemEquipper?.EquippedItem is CombatWeapon meleeWeaponData)
                    ? meleeWeaponData
                    : null;
            }
        }

        public Animator animator { get; private set; }
        public AnimGraph animGraph {get; private set;}
        public CharacterController characterController {get; private set;}
        public ItemEquipper itemEquipper {get; private set;}
        public CapsuleCollider capsuleCollider {get; private set;}
        public Damagable Damagable {get; private set;}
        public VisionSensor VisionSensor { get; set; }

        BoneMapper boneMapper;
        private void Awake()
        {
            animator = GetComponent<Animator>();
            animGraph = GetComponent<AnimGraph>();
            characterController = GetComponent<CharacterController>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            itemEquipper = GetComponent<ItemEquipper>();
            Damagable = GetComponent<Damagable>();
            boneMapper = GetComponent<BoneMapper>();

            hipsTransform = boneMapper.GetBone(BoneType.Hips);

            ragdollRigidBodies = GetRagdollRigidbodies();
            SetRagdollState(false);

            if(capsuleCollider  != null) colliderOriginalCenter = capsuleCollider.center;
        }

        private IEnumerator Start()
        {
            OnDeath += () => OnDeathEvent?.Invoke();

            // Only equip once all start functions are called
            yield return null;
            itemEquipper.EquipItem(defaultWeapon);
        }

        public IEnumerator Dodge(Vector3 dodgeDir, bool canTakeHit = false)
        {
            if (CanDodge)
            {
                var dodge = CurrentWeapon != null && CurrentWeapon.OverrideDodge ? CurrentWeapon.DodgeData : dodgeData;

                if (dodgeDir == Vector3.zero)
                    dodgeDir = dodge.GetDodgeDirection(transform, Target?.transform);

                AnimGraphClipInfo dodgeClip = dodge.GetClip(transform, dodgeDir);
                SetAction(FighterAction.Dodging);

                OnStartAction?.Invoke();
                Damagable.CanTakeHit = canTakeHit;

                //animGraph.CrossFade(dodgeClip, 0.2f);

                yield return animGraph.CrossfadeAsync(dodgeClip, clipInfo: dodgeClip, onAnimationUpdate: (float normalizedTime, float time) =>
                {
                    if (time <= dodgeClip.length * 0.8f && !dodge.useDifferentClipsForDirections)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dodgeDir), 1000 * Time.deltaTime);
                });

                ResetActionToNone(FighterAction.Dodging);
                if (!IsBusy)
                    OnEndAction?.Invoke();
                Damagable.CanTakeHit = true;
            }
        }

        public IEnumerator Roll(Vector3 rollDir, bool canTakeHit = false)
        {
            if (CanRoll)
            {
                var roll = CurrentWeapon != null && CurrentWeapon.OverrideRoll ? CurrentWeapon.RollData : rollData;

                if (rollDir == Vector3.zero)
                    rollDir = roll.GetDodgeDirection(transform, Target?.transform);

                AnimGraphClipInfo rollClip = roll.GetClip(transform, rollDir);
                SetAction(FighterAction.Dodging);

                OnStartAction?.Invoke();
                Damagable.CanTakeHit = canTakeHit;
                yield return animGraph.CrossfadeAsync(rollClip, clipInfo: rollClip, onAnimationUpdate: (float normalizedTime, float time) =>
                {
                    if (time <= rollClip.length * 0.8f && !roll.useDifferentClipsForDirections)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(rollDir), 1000 * Time.deltaTime);
                });

                ResetActionToNone(FighterAction.Dodging);
                if (!IsBusy)
                    OnEndAction?.Invoke();
                Damagable.CanTakeHit = true;
            }
        }

        void HandleBlockingChanged(bool wasPreviouslyBlocking)
        {
            if (isBlocking && !wasPreviouslyBlocking)
            {
                SetAction(FighterAction.Blocking);
                animGraph.PlayLoopingAnimation(CurrentWeapon.Blocking, mask: Mask.Arm, transitionIn: .1f);
            }
            else if (!isBlocking && wasPreviouslyBlocking)
            {
                ResetActionToNone(FighterAction.Blocking);
                animGraph.StopLoopingAnimations(false);
            }
        }

        public void SetAction(FighterAction state)
        {
            if (Action != state)
            {
                Action = state;
            }
        }

        public void ResetActionToNone(FighterAction stateToReset)
        {
            if (Action == stateToReset)
            {
                Action = FighterAction.None;
            }
        }

        public bool IsTarget(GameObject gameObject)
        {
            return LayersUtil.LayerMaskContainsLayer(targetLayer, gameObject.layer);
        }

        public void OnTriggerEnterAction(Collider other, BoxCollider weaponCollider, CombatHitData hitData)
        {
            if (other.tag == "Hitbox" && Damagable.CanTakeHit && !InInvinsibleAction)
            {
                var attacker = other.GetComponentInParent<FighterCore>();

                // Don't take hit during synced animation, while getting up and while getting knocked down
                if (IsInSyncedAnimation || Action == FighterAction.GettingUp || (Action == FighterAction.TakingHit && CurrReaction != null && CurrReaction.willBeKnockedDown))
                    return;

                // Ground attacks should only be hit while knocked down & standing attacks should only hit while standing
                if ((IsKnockedDown && hitData.AttackType != AttackType.GroundAttack) ||
                    (!IsKnockedDown && hitData.AttackType == AttackType.GroundAttack))
                    return;

                // Dont' take hit if the victim was not the intented target of the attack
                if (attacker.TargetOfAttack != this && !hitData.CanHitMultipleTargets)
                    return;

                if (PlayingBlockAnimationEarlier && Action == FighterAction.TakingBlockedHit)
                    return;

                other.enabled = true;
                var hitPoint = IsBlocking ? other.ClosestPoint(weaponCollider != null ? weaponCollider.transform.position : transform.position) : other.ClosestPoint(transform.position);
                other.enabled = false;
                hitData.HitPoint = hitPoint;

                if (hitData.Reaction == null)
                {
                    if (hitData.HitDirection == HitDirections.FromCollision)
                        hitData.HitDirection = GetHitDirection(hitPoint);
                }

                TakeHit(hitData, attacker);
            }
        }

        public void TakeHit(CombatHitData hitData, FighterCore attacker = null)
        {
            StopMovement = false;
            if (IsDead || !Damagable.CanTakeHit) return;

            if (hitData.WillBeBlocked == null)
                hitData.WillBeBlocked = isBlocking && !hitData.IsUnBlockableHit;

            var hitDamagable = (hitData.Damagable != null) ? hitData.Damagable : Damagable;

            if (!hitData.WillBeBlocked.Value)
            {
                if (hitData.IsFinisher)
                    Damagable.TakeDamage(Damagable.CurrentHealth, hitData.HitType);
                else
                    hitDamagable.TakeDamage(hitData.Damage, hitData.HitType);

                // If no reaction clip has been passed, then find a suitable reaction
                if (hitData.Reaction == null && hitData.PlayReaction)
                {
                    var weaponToUse = CurrentWeapon != null ? CurrentWeapon : defaultWeapon;
                    var reactionData = weaponToUse != null && weaponToUse.ReactionData != null && weaponToUse.ReactionData.HasReactionsForType(hitData.HitType) ? weaponToUse.ReactionData : defaultAnimations.hitReactionData;
                    hitData.Reaction = ChooseHitReaction(reactionData?.reactions, hitData.HitDirection, hitData.ReactionTag, attacker.transform, hitData.HitType);
                }
            }
            else
            {
                hitDamagable.TakeDamage(hitData.Damage * (CurrentWeapon.BlockedDamage / 100), hitData.HitType);

                if (hitData.Reaction == null && hitData.PlayReaction)
                {
                    var reactionData = CurrentWeapon != null && CurrentWeapon.ReactionData != null ? CurrentWeapon.BlockReactionData : defaultAnimations.blockedReactionData;
                    hitData.Reaction = ChooseHitReaction(reactionData?.reactions, hitData.HitDirection, hitData.ReactionTag, attacker.transform, hitData.HitType);
                }
            }
            // Go to combat mode
            //animator.SetBool(AnimatorParameters.combatMode, true);

            if (changeTargetWhenTakingHit && attacker != null && Target != attacker && IsTarget(attacker.gameObject))
                Target = attacker;

            CurrReaction = hitData.Reaction;
            OnGotHit?.Invoke(attacker, hitData.HitPoint, hitData.HittingTime, hitData.WillBeBlocked.Value, hitData.HitType);
            OnGotHitEvent?.Invoke(attacker, hitData.HitPoint, hitData.HittingTime);

            
            if (Damagable.CurrentHealth > 0 || (hitData.Reaction != null && (hitData.Reaction.willBeKnockedDown || hitData.IsFinisher)))
            {
                if (hitData.PlayReaction || hitData.IsFinisher)
                    StartCoroutine(PlayHitReaction(hitData.Reaction, hitData.WillBeBlocked.Value, isFinisher: hitData.IsFinisher, isAdditiveLayerAnimation: hitData.Reaction?.isAdditiveAnimation ?? false, 
                        waitTimeTillNextReaction : hitData.WaitTimeTillNextReaction, attackName: hitData.AttackName, attacker: attacker));
            }
            else
            {
                StartCoroutine(PlayDeathAnimation(hitData, attacker?.transform));
            }
        }

        IEnumerator PlayHitReaction(Reaction reaction = null, bool isBlockedReaction = false, AnimationClip getUpAnimation = null, bool isFinisher = false, 
            bool isAdditiveLayerAnimation = false,float waitTimeTillNextReaction = 0, string attackName = "", FighterCore attacker = null)
        {
            if (Action == FighterAction.TakingHit && animGraph.currentClipStateInfo?.currentClipInfo?.clip == reaction?.animationClipInfo?.clip && animGraph.currentClipStateInfo?.normalizedTime <= waitTimeTillNextReaction)
            {
                yield break;
            }
            CanDoActions = reaction.isAdditiveAnimation;

            if (!CanDoActions)
                SetAction(isBlockedReaction ? FighterAction.TakingBlockedHit : FighterAction.TakingHit);

            OnStartAction?.Invoke();
            ++hitCount;

            if (reaction.rotateToAttacker && attacker != null)
                RotateToAttacker(attacker.transform, reaction.rotationOffset);

            bool willBeDead = false;
            if (Damagable.CurrentHealth == 0 && ((reaction != null && reaction.willBeKnockedDown) || isFinisher))
            {
                Die();
                willBeDead = true;
            }
            if (willBeDead)
                OnDeath?.Invoke();
            if (reaction?.animationClipInfo == null && reaction?.animationClipInfo.clip == null)
            {
                Debug.LogError($"Reaction clips are not assigned for attack - " + attackName);
            }
            else
            {
                //animGraph.Crossfade(reaction.animationClipInfo.clip);

                yield return animGraph.CrossfadeAsync(reaction.animationClipInfo, clipInfo: reaction.animationClipInfo, transitionBack: !IsDead && !reaction.willBeKnockedDown, 
                    mask: reaction.animationMask, isAdditiveLayerAnimation: isAdditiveLayerAnimation,overridableByAnimator: isAdditiveLayerAnimation);
                //yield return null;
                //float animLength = animGraph.currentClipStateInfo.clipLength;
                //yield return new WaitUntil(() => animGraph.currentClipStateInfo.normalizedTime >= 0.8f);

                // If the character is knocked down, then play lying down and getting up animation
                if (!IsDead && reaction.willBeKnockedDown && !IsKnockedDown)
                {
                    StartCoroutine(GoToKnockedDownState(reaction));
                }
            }

            --hitCount;

            if (Action != FighterAction.TakingHit || Action != FighterAction.TakingBlockedHit)
                CanDoActions = true;

            if (hitCount == 0 && !IsDead)
            {
                OnHitComplete?.Invoke();

                if (isBlockedReaction && isBlocking)
                    SetAction(FighterAction.Blocking);
                else
                    ResetActionToNone(isBlockedReaction ? FighterAction.TakingBlockedHit : FighterAction.TakingHit);

                if (isBlockedReaction && PlayingBlockAnimationEarlier)
                    PlayingBlockAnimationEarlier = false;

                if (!IsBusy)
                    OnEndAction?.Invoke();
            }

            if (willBeDead)
            {
                OnDeathAnimationCompleted?.Invoke();
                SetAction(FighterAction.None);
            }
            OnGotHitCompleted?.Invoke();
        }

        public IEnumerator PlayDeathAnimation(CombatHitData hitData, Transform attacker = null)
        {
            Die();

            SetAction(FighterAction.TakingHit);

            OnStartAction?.Invoke();

            AnimGraphClipInfo clipInfo = null;
            Reaction reaction = null;
            if (defaultAnimations?.deathReactionData != null)
            {
                 reaction = ChooseHitReaction(defaultAnimations.deathReactionData.reactions, hitData.HitDirection, hitData.ReactionTag, attacker, hitData.HitType);
                clipInfo = reaction?.animationClipInfo;
            }

            if (reaction.rotateToAttacker && attacker != null)
                RotateToAttacker(attacker, reaction.rotationOffset);

            OnDeath?.Invoke();

            if (clipInfo?.clip == null)
                OnDeathAnimationCompleted?.Invoke();

            if (reaction != null && reaction.triggerRagdoll)
            {
                var hasRagdolled = false;
                yield return animGraph.CrossfadeAsync(clipInfo, clipInfo: clipInfo, transitionBack: false, OnComplete: OnDeathAnimationCompleted, onAnimationUpdate: (float normalizedTime, float time) =>
                {
                    if (!hasRagdolled && (reaction.ragdollTriggerTime >= normalizedTime || reaction.ragdollTriggerTime == 0))
                    {
                        SetRagdollState(true);
                        hasRagdolled = true;
                    }
                });
            }
            else
            {
                yield return animGraph.CrossfadeAsync(clipInfo, clipInfo: clipInfo, transitionBack: false, OnComplete: OnDeathAnimationCompleted);
            }

            ResetActionToNone(FighterAction.TakingHit);
        }

        void Die()
        {
            IsDead = true;
            Target = null;
            TargetsInRange = new List<FighterCore>();
            VisionSensor?.gameObject?.SetActive(false);
        }

        public IEnumerator PlayTauntAction(AnimationClip tauntClip)
        {
            SetAction(FighterAction.Taunt);
            OnStartAction?.Invoke();

            animGraph.Crossfade(tauntClip);
            yield return new WaitForSeconds(tauntClip.length);

            ResetActionToNone(FighterAction.Taunt);
            if (!IsBusy)
                OnEndAction?.Invoke();
        }

        void RotateToAttacker(Transform attacker, float rotationOffset = 0f)
        {
            var dispVec = attacker.position - transform.position;
            dispVec.y = 0;
            transform.rotation = Quaternion.LookRotation(dispVec) * Quaternion.Euler(new Vector3(0f, rotationOffset, 0f));
        }

        public bool CheckIfAttackKills(CombatWeapon weapon, float attackDamage, bool willBlock = false)
        {
            float damage = DamageCalculator.CalculateDamage(weapon.GetAttributeValue<float>("Damage"), attackDamage, Damagable);

            if (willBlock)
                damage *= (weapon.BlockedDamage / 100);

            return Damagable.CurrentHealth - damage <= 0;
        }

        Reaction ChooseHitReaction(List<ReactionContainer> reactions, HitDirections hitDirection = HitDirections.Any,
            string reactionTag = null, Transform attacker = null, HitType hitType = HitType.Any)
        {
            if (reactions == null || reactions.Count == 0) return null;

            // Filter by hit type
            if (hitType != HitType.Any)
            {
                var reactionsWithSameType = reactions.Where(r => r.hitType == hitType);
                if (reactionsWithSameType.Count() > 0)
                    reactions = reactionsWithSameType.ToList();
                else
                    reactions = reactions.Where(r => r.hitType == HitType.Any).ToList();
            }

            if (attacker != null)
            {
                // If attacked from behind
                bool attackedFromBehind = Vector3.Angle(transform.forward, attacker.transform.forward) <= 90;
                if (attackedFromBehind)
                {
                    var reactionsFromBehind = reactions.Where(r => r.attackedFromBehind);
                    if (reactionsFromBehind.Count() > 0)
                        reactions = reactionsFromBehind.ToList();
                }
            }

            // Tag match
            if (!String.IsNullOrEmpty(reactionTag))
            {
                var reactionsWithSameTag = reactions.Where(r => !String.IsNullOrEmpty(r.tag) && r.tag.ToLower() == reactionTag.ToLower());
                if (reactionsWithSameTag.Count() > 0)
                    reactions = reactionsWithSameTag.ToList();
                else
                {
                    var reactionsThatContainsTag = reactions.Where(r => !String.IsNullOrEmpty(r.tag)
                        && (r.tag.ToLower().Contains(reactionTag.ToLower()) || reactionTag.ToLower().Contains(r.tag.ToLower())));

                    if (reactionsThatContainsTag.Count() > 0)
                        reactions = reactionsThatContainsTag.ToList();
                }
            }

            // Match direction of the attack
            if (hitDirection != HitDirections.Any)
            {
                var reactionsWithSameDir = reactions.Where(c => c.direction == hitDirection);
                if (reactionsWithSameDir.Count() > 0)
                    reactions = reactionsWithSameDir.ToList();
                else
                {
                    var reactionsWithAnyDir = reactions.Where(c => c.direction == HitDirections.Any);
                    if (reactionsWithAnyDir.Count() > 0)
                        reactions = reactionsWithAnyDir.ToList();
                }
            }
            
            var hasStateMachineReactions = reactions.Where(c => c.isAnimationStateBasedReaction && c.MatchesAnyEntry(animator,0)).ToList();

            if (hasStateMachineReactions.Count > 0)
                reactions = hasStateMachineReactions;
            else
                reactions = reactions.Where(c => c.StateMachineNames.Count == 0).ToList();

            var selectedReactionContainer = reactions.Count > 0 ? reactions.GetRandom() : null;


            return selectedReactionContainer?.reaction;
        }

        public HitDirections GetHitDirection(Vector3 hitPoint)
        {
            var direction = (hitPoint - transform.position + Vector3.up * 0.5f).normalized;
            var right = Vector3.Dot(direction, transform.right);
            var up = Vector3.Dot(direction, transform.up);


            if (Mathf.Abs(right) > Mathf.Abs(up))
                return right > 0 ? HitDirections.Right : HitDirections.Left;
            else if (Mathf.Abs(up) > Mathf.Abs(right))
                return up > 0 ? HitDirections.Top : HitDirections.Bottom;

            return HitDirections.Any;
        }


        IEnumerator GoToKnockedDownState(Reaction reaction)
        {
            IsKnockedDown = true;
            OnKnockDown?.Invoke();
            OnKnockDownEvent.Invoke();

            AdjustColliderForKnockedDownState();

            AnimationClip lyingDownClip;
            AnimationClip getUpClip;

            if (reaction.overrideLyingDownAnimation)
            {
                lyingDownClip = reaction.lyingDownAnimation;
                getUpClip = reaction.getUpAnimation;
            }
            else
            {
                if (reaction.knockDownDirection == KnockDownDirection.LyingOnBack)
                {
                    lyingDownClip = defaultAnimations.lyingOnBackAnimation;
                    getUpClip = defaultAnimations.getUpFromBackAnimation;
                }
                else
                {
                    lyingDownClip = defaultAnimations.lyingOnFrontAnimation;
                    getUpClip = defaultAnimations.getUpFromFrontAnimation;
                }
            }

            if (lyingDownClip != null)
            {
                animGraph.PlayLoopingAnimation(lyingDownClip);
                yield return new WaitForSeconds(UnityEngine.Random.Range(reaction.lyingDownTimeRange.x, reaction.lyingDownTimeRange.y));
                yield return new WaitUntil(() => Action != FighterAction.TakingHit);
            }

            // Don't Getup if dead
            if (Damagable.CurrentHealth <= 0)
                yield break;

            ResetColliderAdjustments();
            //animGraph.StopLoopingClip = true;


            if (getUpClip == null)
            {
                Debug.LogWarning("No Get Up Animations is provided to get up from the knocked down state");
                SetAction(FighterAction.None);
                if (lyingDownClip != null)
                    animGraph.StopLoopingAnimations(false);
                yield break;
            }

            SetAction(FighterAction.GettingUp);
            OnGettingUp?.Invoke();
            OnGettingUpEvent.Invoke();

            if (lyingDownClip != null)
                StartCoroutine(AsyncUtil.RunAfterDelay(0.2f, () => animGraph.StopLoopingAnimations(false)));
            yield return animGraph.CrossfadeAsync(getUpClip);

            IsKnockedDown = false;
            ResetActionToNone(FighterAction.GettingUp);

        }

        Vector3 colliderOriginalCenter;
        void AdjustColliderForKnockedDownState()
        {
            if (capsuleCollider == null) return;
            capsuleCollider.direction = 2;
            capsuleCollider.center = new Vector3(capsuleCollider.center.x, 0, capsuleCollider.center.z);
        }

        void ResetColliderAdjustments()
        {
            if (capsuleCollider == null) return;

            capsuleCollider.direction = 1;
            capsuleCollider.center = colliderOriginalCenter;
        }

        public event Action<FighterCore> OnBeingAttacked;
        public void BeingAttacked(FighterCore attacker)
        {
            OnBeingAttacked?.Invoke(attacker);
            IsBeingAttacked = true;
            CurrAttacker = attacker;
        }

        public void AttackOver(FighterCore attacker)
        {
            if (CurrAttacker == attacker)
            {
                IsBeingAttacked = false;
                CurrAttacker = null;
            }
        }

        public void QuickSwitchWeapon(EquippableItem item = null)
        {
            if (item == null)
                itemEquipper.UnEquipItem(false);
            else
                itemEquipper.EquipItem(item, false);
        }

        public IEnumerator PullBackCharacter(FighterCore attacker, float moveDist = .75f)
        {
            float timer = 0;
            while (timer < moveDist)
            {
                characterController.Move((attacker.transform.forward * moveDist) * Time.deltaTime * 2);
                timer += Time.deltaTime * 2;
                yield return null;
            }
        }

        public void ResetFighter()
        {
            SetAction(FighterAction.None);
            animGraph.ResetAnimationGraph();
            TargetOfAttack = null;
            OnResetFighter?.Invoke();
        }

        public List<Rigidbody> ragdollRigidBodies { get; set; }
        // Add the following field inside FighterCore class (near other private fields)
        private Coroutine modelAdjustmentCoroutine;

        Transform hipsTransform;

        // Add this coroutine method inside FighterCore class
        private IEnumerator AdjustModelPositionCoroutine()
        {
            yield return new WaitForEndOfFrame();
            foreach (Rigidbody rb in ragdollRigidBodies)
            {
                rb.isKinematic = false;
                rb.GetComponent<Collider>().isTrigger = false;
            }
            yield return null;
            animator.enabled = false;
            var modelTransform = hipsTransform;
            //modelTransform.transform.localPosition = Vector3.zero;
            //animator.enabled = false
            //
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            while (true)
            {
                // Find the model transform (an Animator component that is not the main animator)
                modelTransform = hipsTransform;
                if (modelTransform != null)
                {
                    // Continuously update the parent's position to the model's position,
                    // and reset the model's local position to zero
                    transform.position = modelTransform.transform.position;
                    modelTransform.transform.localPosition = Vector3.zero;
                }
                yield return null;
            }
        }

        // Modify the SetRagdollState method as below:
        public void SetRagdollState(bool state)
        {
            // If state is true, start the coroutine if not running

            // Update physics for ragdoll rigidbodies as before

            if (ragdollRigidBodies.Count > 10)
            {
                if (state)
                {
                    if (modelAdjustmentCoroutine == null)
                        modelAdjustmentCoroutine = StartCoroutine(AdjustModelPositionCoroutine());
                }
                else
                {
                    animator.enabled = true;

                    foreach (Rigidbody rb in ragdollRigidBodies)
                    {
                        rb.isKinematic = true;
                        rb.GetComponent<Collider>().isTrigger = false;
                    }
                    // If state is false, stop the coroutine if running
                    if (modelAdjustmentCoroutine != null)
                    {
                        StopCoroutine(modelAdjustmentCoroutine);
                        modelAdjustmentCoroutine = null;
                    }

                    // Optionally, perform a one-time position reset similar to original logic when stopping
                    if (ragdollRigidBodies.Count > 10)
                    {
                        var modelTransform = hipsTransform;
                        if (modelTransform != null)
                        {
                            transform.position = modelTransform.transform.position;
                            modelTransform.transform.localPosition = Vector3.zero;
                        }
                    }
                }
            }
        }
        public List<Rigidbody> GetRagdollRigidbodies()
        {
            var animator = transform.GetComponent<Animator>();
            var ragdollRigidbodies = new List<Rigidbody>();

            if (animator == null || !animator.isHuman)
                return ragdollRigidbodies;

            // Loop through all HumanBodyBones that can have a Rigidbody in a ragdoll
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;

                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    Rigidbody rb = boneTransform.GetComponent<Rigidbody>();
                    if (rb != null && !ragdollRigidbodies.Contains(rb))
                        ragdollRigidbodies.Add(rb);
                }
            }

            return ragdollRigidbodies;
        }
        float ySpeed;
        public void ApplyAnimationGravity(bool isGrounded, bool systemIsInFocus)
        {
            if (isGrounded) ySpeed = Physics.gravity.y;
            if (animGraph.currentClipStateInfo.isPlayingAnimation && animGraph.currentClipStateInfo.currentClipInfo.useGravity && !IsMatchingTarget && systemIsInFocus && !IgnoreCollisions)
            {
                ySpeed += Physics.gravity.y * Time.deltaTime;
                characterController.Move(ySpeed * Vector3.up * animGraph.currentClipInfo.gravityModifier.GetValue(animGraph.currentClipStateInfo.normalizedTime) * Time.deltaTime);
            }
            animator.SetBool(AnimatorParameters.IsGrounded, isGrounded);
        }

        public void TriggerOnWeaponEquip(CombatWeapon weaponData)
        {
            OnWeaponEquipAction?.Invoke(weaponData);
            OnWeaponEquipEvent?.Invoke(weaponData);
        }

        public void TriggerOnWeaponUnEquip(CombatWeapon weaponData)
        {
            OnWeaponUnEquipAction?.Invoke(weaponData);
            OnWeaponUnEquipEvent?.Invoke(weaponData);
        }

        public void TriggerOnStartAction() => OnStartAction?.Invoke();
        public void TriggerOnEndAction() => OnEndAction?.Invoke();

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            //Handles.Label(transform.position + Vector3.up * 2f, Action.ToString(), new GUIStyle() { fontSize = 20 });
            //Handles.Label(transform.position + Vector3.up * 2f, transform.position.y.ToString(), new GUIStyle() { fontSize = 20 });
        }
#endif
    }

    public class CombatHitData
    {
        public float Damage { get; set; }
        public HitType HitType { get; set; }
        public HitDirections HitDirection { get; set; }
        public Damagable Damagable { get; set; }    // optional - useful in cases where damaged should be applied to certain body parts
        public string ReactionTag { get; set; }
        public bool PlayReaction { get; set; } = true;
        public Reaction Reaction { get; set; }
        public Vector3 HitPoint { get; set; }
        public bool IsFinisher { get; set; }
        public bool? WillBeBlocked { get; set; }
        public float HittingTime { get; set; }
        public AttackType AttackType { get; set; }
        public bool IsUnBlockableHit { get; set; }
        public bool CanHitMultipleTargets { get; set; }
        public float WaitTimeTillNextReaction { get; set; }
        public string AttackName { get; set; }  // For debugging purposes, to identify the attack that caused the hit
    }

    [Serializable]
    public class DefaultReactions
    {
        public ReactionsData hitReactionData;
        public ReactionsData blockedReactionData;
        public DeathReactionsData deathReactionData;

        [Header("Lying down and Get up Animations")]
        public AnimationClip lyingOnBackAnimation;
        public AnimationClip getUpFromBackAnimation;
        public AnimationClip lyingOnFrontAnimation;
        public AnimationClip getUpFromFrontAnimation;

        public bool useRagdollForExplosionDeath = true;
    }

    [Serializable]
    public class DodgeData
    {
        [HideInInspector]
        public AnimationClip clip;
        public AnimGraphClipInfo clipInfo;

        public DodgeDirection defaultDirection;
        public bool useDifferentClipsForDirections;

        public AnimGraphClipInfo frontClipInfo;
        public AnimGraphClipInfo backClipInfo;
        public AnimGraphClipInfo leftClipInfo;
        public AnimGraphClipInfo rightClipInfo;

        public Vector3 GetDodgeDirection(Transform transform, Transform target)
        {
            if (defaultDirection == DodgeDirection.Forward || (defaultDirection == DodgeDirection.TowardsTarget && target == null))
                return transform.forward;
            else if (defaultDirection == DodgeDirection.Backward || (defaultDirection == DodgeDirection.AwayFromTarget && target == null))
                return -transform.forward;
            else if (defaultDirection == DodgeDirection.AwayFromTarget)
                return -(target.position - transform.position);
            else if (defaultDirection == DodgeDirection.TowardsTarget)
                return (target.position - transform.position);

            return -transform.forward;
        }

        public AnimGraphClipInfo GetClip(Transform transform, Vector3 direction)
        {
            if (!useDifferentClipsForDirections)
                return clipInfo;

            var dir = transform.InverseTransformDirection(direction);
            //Debug.Log(dir);

            float h = dir.x;
            float v = dir.z;

            if (Math.Abs(v) >= Math.Abs(h))
                return (v > 0) ? frontClipInfo : backClipInfo;
            else
                return (h > 0) ? rightClipInfo : leftClipInfo;
        }
    }

    public enum DodgeDirection { AwayFromTarget, TowardsTarget, Backward, Forward }
}
