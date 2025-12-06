using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderKeywordFilter;

#endif
using UnityEngine;
using UnityEngine.Events;
//using UnityEngine.InputSystem.Utilities;
using Random = UnityEngine.Random;

namespace FS_CombatSystem
{
    public enum AttackStates { Idle, Windup, Impact, Cooldown }

    public class MeleeFighter : MonoBehaviour
    {
        [SerializeField] float rotationSpeedDuringAttack = 500f;

        public bool canRoll = true;
        public bool canDodge = true;

        public AttackStates AttackState { get; private set; }

        public bool IsCounterable => AttackState == AttackStates.Windup && (!MeleeCombatSettings.i.OnlyCounterFirstAttackOfCombo || comboCount == 0);

        BoxCollider weaponCollider;
        BoxCollider leftHandCollider, rightHandCollider, leftFootCollider, rightFootCollider;
        BoxCollider leftElbowCollider, rightElbowCollider, leftKneeCollider, rightKneeCollider, headCollider;

        BoxCollider activeCollider;
        Vector3 prevColliderPos;
        GameObject prevGameObj;

        bool doCombo;
        int comboCount = 0;

        public AttackData CurrAttack { get; set; }
        public AttackContainer CurrAttackContainer { get; set; }

        public event Action<FighterCore> OnAttack;
        public event Action<AttackData,FighterCore> OnAttackHit;

        public Action OnStartAttack;
        public Action OnEndAttack;

        public event Action OnCounterMisused;
        [HideInInspector] public UnityEvent OnCounterMisusedEvent;


        public event Action<MeleeWeaponObject> OnEnableHit;

        public float AttackTimeNormalized { get; set; }


        public List<AttackSlot> CurrAttacksList { get; private set; }
        
        public bool CanSwitchWeapon { get; set; } = true;

        public MeleeWeapon CurrentMeleeWeapon => core.CurrentWeapon as MeleeWeapon;


        public bool IsMeleeWeaponEquipped => itemEquipper?.EquippedItem != null && itemEquipper?.EquippedItem is MeleeWeapon
                    && (itemEquipper.EquippedItemRight is MeleeWeaponObject || itemEquipper.EquippedItemLeft is MeleeWeaponObject);


        //public MeleeWeaponObject CurrentWeaponRight => itemEquipper?.EquippedItemRight as MeleeWeaponObject;
        //public MeleeWeaponObject CurrentWeaponLeft => itemEquipper?.EquippedItemLeft as MeleeWeaponObject;
        public MeleeWeaponObject CurrentMeleeWeaponObject => itemEquipper.EquippedItemObject as MeleeWeaponObject;
        public MeleeWeaponObject CurrentMeleeBoneWeapon { get; set; }



        FighterCore core;
        Animator animator;
        ItemEquipper itemEquipper;
        AnimGraph animGraph;
        BoneMapper boneMapper;

        private void Awake()
        {
            core = GetComponent<FighterCore>();
            animator = GetComponent<Animator>();
            itemEquipper = GetComponent<ItemEquipper>();
            animGraph = GetComponent<AnimGraph>();
            boneMapper = GetComponent<BoneMapper>();

            itemEquipper.OnEquip += OnItemEquipped;
            itemEquipper.OnEquipComplete += (wd) =>
            {
                if (wd is MeleeWeapon)
                    core.ResetActionToNone(FighterAction.SwitchingWeapon);
            };
            itemEquipper.OnUnEquip += OnItemUnEquipped;
            itemEquipper.OnBeforeItemDisable += (wd) =>
            {
                if (wd is MeleeWeapon)
                    core.ResetActionToNone(FighterAction.SwitchingWeapon);
            };
        }

        private void Start()
        {
            // Note: Checking if EquipableItemHolder is null to make sure that we don't take attached weapons like swords
            leftHandCollider = boneMapper.GetBone(BoneType.LeftHand)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
            rightHandCollider = boneMapper.GetBone(BoneType.RightHand)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
            leftFootCollider = boneMapper.GetBone(BoneType.LeftFoot)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
            rightFootCollider = boneMapper.GetBone(BoneType.RightFoot)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();

            leftElbowCollider = boneMapper.GetBone(BoneType.LeftElbow)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
            rightElbowCollider = boneMapper.GetBone(BoneType.RightElbow)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
            leftKneeCollider = boneMapper.GetBone(BoneType.LeftKnee)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
            rightKneeCollider = boneMapper.GetBone(BoneType.RightKnee)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();

            headCollider = boneMapper.GetBone(BoneType.Head)?.GetComponentsInChildren<MeleeWeaponObject>().FirstOrDefault(m => m.GetComponentInParent<EquippableItemHolder>() == null)?.GetComponent<BoxCollider>();
        }

        // For testing, remove later
        public bool IsPlayerForDebug { get; set; }

        public void TryToAttack(FighterCore target = null, bool isHeavyAttack = false, bool isCounter = false, bool isCharged = false, bool isSpecialAttack = false)
        {
            if (core.IsKnockedDown || core.IsDead ) return;

            if (CurrentMeleeWeapon == null && core.defaultWeapon != null && core.defaultWeapon is MeleeWeapon)
                itemEquipper.EquipItem(core.defaultWeapon, false);

            if (CurrentMeleeWeapon == null) return;

            // TODO: Check if CurrentMeleeWeapon causes issue when equipped
            if (core.Action == FighterAction.TakingBlockedHit && isCounter && CurrentMeleeWeapon.CanCounter)
            {
                // Taking Blocked Hit is a special case where counter can be peformed. So don't check the normal conditions
            }
            else
            {
                if (!core.InAttackableAction || core.IsInSyncedAnimation || target != null && target.IsInSyncedAnimation) return;
            }

            HandleAttack(target, isHeavyAttack, isCounter, isCharged, isSpecialAttack);
        }

        void HandleAttack(FighterCore target = null, bool isHeavyAttack = false, bool isCounter = false, bool isCharged = false, bool isSpecialAttack = false)
        {
            // Don't attack if already attacking and wind up is not over
            if (core.Action == FighterAction.Attacking && AttackState == AttackStates.Windup) return;
            if (CurrentMeleeWeapon == null) return;

            if (target != null)
            {
                if (Mathf.Abs(transform.position.y - target.transform.position.y) > MeleeCombatSettings.i.VerticalLimitForAttacks)
                    return;
            }

            core.Target = target;

            if (!ChooseAttacks(target, comboCount, isHeavyAttack: isHeavyAttack, isCounter: isCounter, isSpecialAttack))
            {
                if (isCounter && CurrentMeleeWeapon.PlayActionIfCounterMisused && CurrentMeleeWeapon.CounterMisusedAction != null
                    && !core.InAction && core.Target != null)
                {
                    StartCoroutine(core.PlayTauntAction(CurrentMeleeWeapon.CounterMisusedAction));
                    OnCounterMisused?.Invoke();
                    OnCounterMisusedEvent?.Invoke();
                }

                return;
            }

            isChargedInput = isCharged;

            if (!core.InAction || core.Action == FighterAction.TakingBlockedHit)
            {
                StartCoroutine(Attack(core.Target));
            }
            else if (AttackState == AttackStates.Impact || AttackState == AttackStates.Cooldown)
            {
                if (!isCounter)
                    doCombo = true;
            }
        }


        public bool ChooseAttacks(FighterCore target = null, int comboCount = 0, bool isHeavyAttack = false, bool isCounter = false, bool isSpecialAttack = false)
        {
            if (CurrAttacksList == null)
                CurrAttacksList = new List<AttackSlot>();

            if (target != null && !target.Damagable.CanTakeHit) return false;

            // Select Counters
            if (isCounter)
            {
                if (!CurrentMeleeWeapon.CanCounter) return false;

                bool counterPossible = ChooseCounterAttacks(target);
                if (!counterPossible)
                    CurrAttacksList = new List<AttackSlot>();

                return counterPossible;
            }

            if (CurrentMeleeWeapon.Attacks != null && CurrentMeleeWeapon.Attacks.Count > 0)
            {
                var possibleAttacks = CurrentMeleeWeapon.Attacks.ToList();
                if (isHeavyAttack) possibleAttacks = CurrentMeleeWeapon.HeavyAttacks.ToList();
                if (isSpecialAttack) possibleAttacks = CurrentMeleeWeapon.SpecialAttacks.ToList();

                var normalAttacks = possibleAttacks.Where(a => a.AttackType == AttackType.Single || a.AttackType == AttackType.Combo).OrderBy(a => a.MinDistance).ToList();
                var normalAttacksNoSynced = normalAttacks.Where(a => a.AttackSlots.Any(s => !s.Attack.IsSyncedReaction && !s.Attack.IsFinisher)).ToList();

                bool inCombo = core.Action == FighterAction.Attacking && CurrAttacksList.Count > 0 && comboCount != CurrAttacksList.Count - 1;

                if (target != null && !target.IsDead)
                {
                    // If target is blocking, then avoid attacks with synced reaction
                    if (target.IsBlocking)
                        possibleAttacks.RemoveAll(a => a.AttackSlots.Any(s => s.Attack.IsSyncedReaction && !s.Attack.IsUnblockableAttack));

                    bool attackerUndetected = target.Target == null;

                    // Filter by attack type
                    if (attackerUndetected && possibleAttacks.Any(a => a.AttackType == AttackType.Stealth))
                        possibleAttacks = possibleAttacks.Where(a => a.AttackType == AttackType.Stealth).ToList();
                    else if (target.IsKnockedDown)
                        possibleAttacks = possibleAttacks.Where(a => a.AttackType == AttackType.GroundAttack).ToList();
                    else
                        possibleAttacks = normalAttacks;

                    // Filter by distance and health
                    float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
                    possibleAttacks = possibleAttacks.Where(c => distanceToTarget >= c.MinDistance && distanceToTarget <= c.MaxDistance
                        && (target.Damagable.CurrentHealth / target.Damagable.MaxHealth) * 100 <= c.HealthThreshold).OrderBy(a => a.HealthThreshold).ToList();
                }
                else
                {
                    // No target: use normal attacks without synced/finishers
                    possibleAttacks = normalAttacksNoSynced;
                }

                if (possibleAttacks.Count > 0)
                {
                    // If a finisher is possible then choose it
                    var possibleFinishers = possibleAttacks.Where(c => c.AttackSlots.Any(a => a.Attack.IsFinisher)).ToList();
                    if (possibleFinishers.Count > 0)
                    {
                        CurrAttacksList = possibleFinishers[Random.Range(0, possibleFinishers.Count)].AttackSlots;
                        return true;
                    }

                    // If it's not a combo or if the target went down in the middle of a combo then change the attack
                    if (!inCombo || (target != null && target.IsKnockedDown))
                    {
                        float lowestHealthTreshold = possibleAttacks.First().HealthThreshold;
                        var bestAttacks = possibleAttacks.Where(a => a.HealthThreshold == lowestHealthTreshold).ToList();

                        CurrAttacksList = bestAttacks[Random.Range(0, bestAttacks.Count)].AttackSlots;
                        return true;
                    }
                }
                else
                {
                    // If it's not a combo or if the target went down in the middle of a combo then, try to change the attack
                    if (!inCombo || (target != null && target.IsKnockedDown))
                    {
                        // No possible attacks, then just play the normal attacks if any
                        if (normalAttacksNoSynced.Count > 0)
                        {
                            CurrAttacksList = normalAttacks.First().AttackSlots;
                            return true;
                        }
                        else
                        {
                            Debug.LogWarning("No possible attacks for the given range!");
                            CurrAttacksList = new List<AttackSlot>();
                            return false;
                        }
                    }
                }
            }

            return CurrAttacksList.Count > 0;
        }

        public bool ChooseCounterAttacks(FighterCore target)
        {
            if (target == null || target.IsDead) return false;

            var attacker = core.CurrAttacker?.GetComponent<MeleeFighter>();
            if (attacker == null) return false;

            if (core.IsBeingAttacked && attacker.IsCounterable && (!MeleeCombatSettings.i.OnlyCounterWhileBlocking || WillBlockAttack(attacker.CurrAttack)))
            {
                core.Target = core.CurrAttacker;

                var currAttack = attacker.CurrAttack;
                if (currAttack.CanBeCountered && currAttack.CounterAttacks.Count > 0)
                {
                    var possibleCounters = currAttack.CounterAttacks.Where(a => (target.Damagable.CurrentHealth / target.Damagable.MaxHealth) * 100 <= a.HealthThresholdForCounter).
                        OrderBy(a => a.HealthThresholdForCounter).ToList();
                    if (possibleCounters.Count == 0)
                        return false;

                    float lowestHealth = possibleCounters.First().HealthThresholdForCounter;
                    var counterAttack = possibleCounters.Where(a => a.HealthThresholdForCounter == lowestHealth).ToList().
                        GetRandom<CounterAttack>();

                    if (target.CheckIfAttackKills(core.CurrentWeapon, counterAttack.Attack.Damage, WillTargetBlockAttack(target, counterAttack.Attack)) && !counterAttack.Attack.IsFinisher && !counterAttack.Attack.Reaction.willBeKnockedDown)
                        return false;

                    var counterSlot = new AttackSlot()
                    {
                        Attack = counterAttack.Attack,
                        Container = new AttackContainer() { AttackType = AttackType.Single }
                    };

                    CurrAttacksList = new List<AttackSlot>() { counterSlot };
                    attackStartDelay = counterAttack.CounterStartTime;

                    //Time.timeScale = 0.1f;

                    return true;
                }
            }

            return false;
        }

        Vector3 moveToPos = Vector3.zero;
        bool isChargedInput = false;
        float attackStartDelay = 0f;
        
        IEnumerator Attack(FighterCore target = null)
        {
            core.StopMovement = false;
            core.TriggerOnStartAction();
            OnStartAttack?.Invoke();
            core.SetAction(FighterAction.Attacking);
            core.TargetOfAttack = target;
            AttackState = AttackStates.Windup;

            var attackSlot = CurrAttacksList[comboCount];
            var attack = attackSlot.Attack;
            if (isChargedInput && attackSlot.CanBeCharged && attackSlot.ChargedAttack != null)
                attack = attackSlot.ChargedAttack;

            if (target != null && attack.MoveToTarget)
            {
                if (attack.MoveType == TargetMatchType.Snap)
                    attack.IgnoreCollisions = true;

                core.IgnoreCollisions = attack.IgnoreCollisions;

                if (attack.IsSyncedReaction || attack.IsSyncedBlockedReaction)
                    target.IgnoreCollisions = core.IgnoreCollisions;
            }

            bool wasChargedAttack = isChargedInput;
            isChargedInput = false;

            CurrAttack = attack;
            CurrAttackContainer = attackSlot.Container;

            var meleeTarget = target?.GetComponent<MeleeFighter>();

            if (attackStartDelay > 0f && meleeTarget != null && meleeTarget.CurrAttack != null)
            {
                yield return new WaitUntil(() => meleeTarget.AttackTimeNormalized >= attackStartDelay);
                attackStartDelay = 0f;
            }

            OnAttack?.Invoke(target);
            var attackDir = transform.forward;
            Vector3 startPos = transform.position;
            Vector3 targetPos = Vector3.zero;
            Vector3 rootMotionScaleFactor = Vector3.one;

            bool willAttackBeBlocked = false;

            bool syncedReactionPlayed = false;
            bool shouldStartBlockingEarlier = false;

            if (target != null)
            {
                willAttackBeBlocked = WillTargetBlockAttack(target, attack);

                target.BeingAttacked(core);

                var vecToTarget = target.transform.position - transform.position;
                vecToTarget.y = 0;

                attackDir = vecToTarget.normalized;
                float distance = vecToTarget.magnitude - attack.DistanceFromTarget;

                // Rotate to target
                if (attack.RotateToTarget)
                {
                    if (attack.RotateToTargetSettings.RotationType == RotationType.Snap)
                    {

                        //Debug.Break();
                        transform.rotation = Quaternion.LookRotation(attackDir) * Quaternion.Euler(new Vector3(0f, attack.RotateToTargetSettings.RotationOffset, 0f));
                        // target.transform.rotation = Quaternion.LookRotation(-attackDir) * Quaternion.Euler(new Vector3(0f, attack.RotateToAttackerOffset, 0f));

                    }
                }
                
                
                // Move to target
                if (attack.MoveToTarget)
                {
                    targetPos = target.transform.position - attackDir * attack.DistanceFromTarget;

                    var previousPos = transform.position;

                    if (attack.MoveType == TargetMatchType.ScaleRootMotion)
                    {
                        var endFramePos = attack.RootCurves.GetPositionAtTime(attack.MoveEndTime * attack.Clip.length);
                        var startFramePos = attack.RootCurves.GetPositionAtTime(attack.MoveStartTime * attack.Clip.length);

                        var destDisp = targetPos - transform.position;
                        var rootMotionDisp = Quaternion.LookRotation(destDisp) * (endFramePos - startFramePos);

                        // Having a zero value can casue problems when we find the ratio
                        if (rootMotionDisp.x == 0) rootMotionDisp.x = 1;
                        if (rootMotionDisp.y == 0) rootMotionDisp.y = 1;
                        if (rootMotionDisp.z == 0) rootMotionDisp.z = 1;

                        rootMotionScaleFactor = new Vector3(attack.WeightMask.x * destDisp.x / rootMotionDisp.x, attack.WeightMask.y * destDisp.y / rootMotionDisp.y, attack.WeightMask.z * destDisp.z / rootMotionDisp.z);
                    }
                    moveToPos = targetPos;

                    if (attack.MoveType == TargetMatchType.Snap)
                    {
                        if (attack.SnapTarget == SnapTarget.Attacker)
                        {
                            if (attack.SnapType == SnapType.LocalPosition)
                                moveToPos = target.transform.TransformPoint(attack.LocalPosFromTarget);

                            transform.position = moveToPos;
                        }
                        else if (attack.SnapTarget == SnapTarget.Victim)
                        {
                            if (attack.SnapType == SnapType.LocalPosition)
                                moveToPos = transform.TransformPoint(attack.LocalPosFromTarget);
                            else if (attack.SnapType == SnapType.Distance)
                                moveToPos = transform.position + attackDir * attack.DistanceFromTarget;

                            target.transform.position = moveToPos;
                        }
                    }

                    if (Physics.Raycast(moveToPos + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, MeleeCombatSettings.i.VerticalLimitForAttacks + 1f, FSSettings.i.GroundLayer, QueryTriggerInteraction.Ignore))
                    {
                        moveToPos.y = hit.point.y;
                        targetPos.y = hit.point.y;
                    }

                }
                if (vecToTarget.magnitude < CurrentMeleeWeapon.MinAttackDistance && !attack.IsSyncedReaction && !attack.IsSyncedBlockedReaction && !attack.MoveToTarget)
                {
                    var moveDist = CurrentMeleeWeapon.MinAttackDistance - vecToTarget.magnitude;
                    StartCoroutine(target.PullBackCharacter(core, moveDist));
                }

                // Synced Reaction
                if (target.CheckIfAttackKills(core.CurrentWeapon, attack.Damage, WillTargetBlockAttack(target, attack)) && !attack.IsFinisher && !attack.Reaction.willBeKnockedDown)
                {
                    core.IsInSyncedAnimation = target.IsInSyncedAnimation = false;
                }
                else
                {
                    if (willAttackBeBlocked && attack.OverrideBlockedReaction && attack.IsSyncedBlockedReaction)
                        shouldStartBlockingEarlier = true;

                    if (!willAttackBeBlocked && attack.OverrideReaction && attack.IsSyncedReaction)
                        core.IsInSyncedAnimation = target.IsInSyncedAnimation = true;

                    if (core.IsInSyncedAnimation)
                        core.CurrSyncedAction = target.CurrSyncedAction = attack.Clip;
                }
            }

            animGraph.Crossfade(attack.Clip, clipInfo: attack.Clip, transitionOut: 0.4f);

            core.MatchingTargetDeltaPos = Vector3.zero;
            core.IsMatchingTarget = false;

            float timer = 0f;
            float attackLength = animGraph.currentClipStateInfo.clipLength;
            while (animGraph.currentClipStateInfo.timer <= attackLength)
            {
                if (core.Action == FighterAction.TakingHit || core.IsDead) break;
                if (Time.timeScale == 0)
                {
                    yield return null;
                    continue;
                }

                if (timer >= animGraph.currentClipStateInfo.timer) break;

                timer = animGraph.currentClipStateInfo.timer;
                float normalizedTime = timer / attackLength;
                AttackTimeNormalized = normalizedTime;

                // Play Synced Reaction
                if (core.IsInSyncedAnimation && !syncedReactionPlayed)
                {
                    if (!willAttackBeBlocked && attack.OverrideReaction && attack.IsSyncedReaction)
                    {
                        var hittingTimer = Mathf.Clamp(attack.HittingTime * attackLength - timer, 0, attack.HittingTime * attackLength);
                        target.StopMovement = true;
                        if (normalizedTime >= attack.SyncStartTime)
                        {
                            syncedReactionPlayed = true;
                            var hitData = GetHitData(this, target, reaction: attack.Reaction, hittingTime: hittingTimer);
                            target.TakeHit(hitData, core);
                        }
                    }
                }

                if (shouldStartBlockingEarlier)
                {
                    var hittingTimer = Mathf.Clamp(attack.BlockedHittingTime * attackLength - timer, 0, attack.BlockedHittingTime * attackLength);
                    if (normalizedTime >= attack.BlockSyncStartTime)
                    {
                        shouldStartBlockingEarlier = false;
                        target.PlayingBlockAnimationEarlier = true;
                        target.TakeHit(GetHitData(this, target, reaction: attack.BlockedReaction, willBeBlocked: true, hittingTime: hittingTimer), core);
                    }
                }

                // Move the attacker towards the target while performing attack
                if (target != null && attack.MoveType != TargetMatchType.Snap
                    && attack.MoveToTarget && normalizedTime >= attack.MoveStartTime && normalizedTime <= attack.MoveEndTime)
                {
                    core.IsMatchingTarget = true;

                    float percTime = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime);

                    if (attack.MoveType == TargetMatchType.Linear)
                    {
                        core.MatchingTargetDeltaPos = (targetPos - startPos) * Time.deltaTime / ((attack.MoveEndTime - attack.MoveStartTime) * attackLength);
                    }
                    else
                    {
                        var disp = Vector3.Scale(animator.deltaPosition, rootMotionScaleFactor);
                        core.MatchingTargetDeltaPos = disp;
                    }
                }
                else if(attack.MoveToTarget && attack.MoveType == TargetMatchType.Snap && normalizedTime <= animGraph.currentClipInfo.TranistionInAndOut.x)
                {
                    if (attack.SnapTarget == SnapTarget.Attacker)
                    {
                        transform.position = moveToPos;
                    }
                    else if (attack.SnapTarget == SnapTarget.Victim)
                    {
                        target.transform.position = moveToPos;
                    }
                }
                else
                {
                    core.IsMatchingTarget = false;
                }

                // Rotate the attacker towards the target while performing attack
                if (CurrAttack.RotateToTarget && attackDir != Vector3.zero)
                {
                    var targetRotation = Quaternion.LookRotation(attackDir) * Quaternion.Euler(new Vector3(0f, CurrAttack.RotateToTargetSettings.RotationOffset, 0f));

                    if (CurrAttack.RotationType == RotationType.AlwaysRotate)
                    {
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation ,rotationSpeedDuringAttack * Time.deltaTime);
                    }
                    else if (CurrAttack.RotationType == RotationType.Linear)
                    {
                        if (normalizedTime >= attack.RotateStartTime && normalizedTime <= attack.RotateEndTime)
                        {
                            float percTime = (normalizedTime - attack.RotateStartTime) / (attack.RotateEndTime - attack.RotateStartTime);
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, percTime);
                        }
                    }
                }

                Vector3 hitPoint = Vector3.zero;
                if (AttackState == AttackStates.Windup)
                {
                    if (normalizedTime >= attack.ImpactStartTime)
                    {
                        AttackState = AttackStates.Impact;
                        EnableActiveCollider(attack);
                        if (activeCollider)
                        {
                            prevColliderPos = activeCollider.transform.TransformPoint(activeCollider.center);
                            prevGameObj = null;
                        }
                    }
                }
                else if (AttackState == AttackStates.Impact)
                {
                    if (!core.IsInSyncedAnimation)
                        HandleColliderSweep(target);

                    if (normalizedTime >= attack.ImpactEndTime)
                    {
                        AttackState = AttackStates.Cooldown;
                        DisableActiveCollider();
                        prevGameObj = null;
                    }
                }
                else if (AttackState == AttackStates.Cooldown)
                {
                    if (doCombo && CurrAttacksList.Count > 0)
                    {
                        // Don't do combo till wait for attack time is reached
                        if (attack.WaitForNextAttack && normalizedTime < attack.WaitForAttackTime)
                        {
                            yield return null;
                            continue;
                        }

                        // Play next attack from combo
                        doCombo = false;
                        if (wasChargedAttack)
                            comboCount = 0;
                        else
                            comboCount = (comboCount + 1) % CurrAttacksList.Count;

                        if (target == null || !target.IsDead)
                        {
                            if (core.CurrSyncedAction == attack.Clip && (attack.IsSyncedReaction || attack.IsSyncedBlockedReaction))
                            {
                                target.IsInSyncedAnimation = core.IsInSyncedAnimation = false;
                            }
                            StartCoroutine(Attack(target));
                            yield break;
                        }
                    }
                }

                yield return null;
            }


            if (core.CurrSyncedAction == attack.Clip && (attack.IsSyncedReaction || attack.IsSyncedBlockedReaction))
            {
                target.IsInSyncedAnimation = core.IsInSyncedAnimation = false;
            }

            if (core.IgnoreCollisions && !core.IsInSyncedAnimation)
            {
                core.IgnoreCollisions = false;

                if (target != null)
                    target.IgnoreCollisions = false;
            }

            if (core.IsMatchingTarget)
                core.IsMatchingTarget = false;
            core.MatchingTargetDeltaPos = Vector3.zero;

            if (target != null)
                target.AttackOver(core);


            AttackState = AttackStates.Idle;

            comboCount = 0;
            core.TargetOfAttack = null;
            CurrAttack = null;

            if (core.Action == FighterAction.Attacking)
            {
                core.TriggerOnEndAction();
                OnEndAttack?.Invoke();
            }

            core.ResetActionToNone(FighterAction.Attacking);
        }

        CombatHitData GetHitData(MeleeFighter attacker, FighterCore target, Reaction reaction = null, bool? willBeBlocked = null, float hittingTime = 0)
        {
            var attack = attacker.CurrAttack;
            var hitData = new CombatHitData()
            {
                HitType = HitType.Melee,
                AttackType = attacker.CurrAttackContainer.AttackType,
                IsFinisher = attack.IsFinisher,
                ReactionTag = attack.ReactionTag,
                HittingTime = hittingTime,
                HitDirection = attack.HitDirection,
                WillBeBlocked = willBeBlocked,
                IsUnBlockableHit = attack.IsUnblockableAttack,
                CanHitMultipleTargets = attack.CanHitMultipleTargets,
                Reaction = reaction,
                AttackName = attack.name
            };

            hitData.Damage = CalculateDamage(attacker.Core.CurrentWeapon, attack.Damage, target.Damagable);

            if (!willBeBlocked.GetValueOrDefault())
            {
                if (hitData.Reaction == null && attack.OverrideReaction)
                    hitData.Reaction = attack.Reaction;
            }
            else
            {
                if (hitData.Reaction == null && attack.OverrideBlockedReaction)
                    hitData.Reaction = attack.BlockedReaction;
            }

            return hitData;
        }

        float CalculateDamage(CombatWeapon weapon, float attackDamage, Damagable damagable)
        {
            float damage = DamageCalculator.CalculateDamage(weapon.GetAttributeValue<float>("Damage"), attackDamage, damagable);
            return damage;
        }

        public bool WillBlockAttack(AttackData attack) => core.IsBlocking && !attack.IsUnblockableAttack;
        public bool WillTargetBlockAttack(FighterCore target, AttackData attack) => target.IsBlocking && !attack.IsUnblockableAttack;

        public bool IsInCounterWindow() => AttackState == AttackStates.Cooldown && !doCombo;

        void HandleColliderSweep(FighterCore target)
        {
            if (activeCollider == null) return;

            var activeBoxCollider = activeCollider;
            Vector3 hitPoint = Vector3.zero;
            if (activeBoxCollider)
            {
                Vector3 endPoint = activeBoxCollider.transform.TransformPoint(activeBoxCollider.center);

                Vector3 direction = (endPoint - prevColliderPos).normalized;

                float distance = Vector3.Distance(prevColliderPos, endPoint);


                Vector3 halfExtents = Vector3.Scale(activeBoxCollider.size, activeBoxCollider.transform.localScale) * 0.5f;
                Quaternion orientation = activeBoxCollider.transform.rotation;

                RaycastHit hit;

                int layerMask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
                if (activeCollider != null)
                {
                    layerMask &= ~(1 << activeCollider.gameObject.layer);
                }
                var checkCollision = Physics.OverlapBox(prevColliderPos, halfExtents, orientation, layerMask, QueryTriggerInteraction.Collide);
                //GizmosExtend.drawBoxCastBox(prevColliderPos, halfExtents, orientation, direction, distance);

                if (checkCollision.Length > 0 && prevGameObj != checkCollision[0].gameObject && checkCollision[0].gameObject.GetComponent<Damagable>() != null)
                {
                    var collidedTarget = checkCollision[0].GetComponentInParent<FighterCore>();
                    var damagable = checkCollision[0].gameObject.GetComponent<Damagable>();
                    if (collidedTarget != null)
                    {
                        collidedTarget.OnTriggerEnterAction(activeBoxCollider, weaponCollider, GetHitData(this, collidedTarget));
                        OnAttackHit?.Invoke(CurrAttack, collidedTarget);
                    }
                    else
                    {
                        float damage = CalculateDamage(CurrentMeleeWeapon, CurrAttack.Damage, damagable);
                        damagable.TakeDamage(damage, HitType.Melee);
                        ApplyHitForce(checkCollision[0], checkCollision[0].ClosestPoint(transform.position), direction, damage * 0.5f);
                    }
                    prevGameObj = checkCollision[0].gameObject;
                }
                else
                {
                    bool isHit = Physics.BoxCast(prevColliderPos, halfExtents, direction, out hit, orientation, distance, layerMask, QueryTriggerInteraction.Collide);
                    //GizmosExtend.drawBoxCastBox(prevColliderPos, halfExtents, orientation, direction, distance);
                    if (isHit && prevGameObj != hit.transform.gameObject && hit.transform.GetComponent<Damagable>() != null)
                    {
                        var targetFighter = hit.transform.GetComponentInParent<FighterCore>();
                        var damagable = hit.transform.GetComponent<Damagable>();
                        if (targetFighter != null)
                        {
                            targetFighter.OnTriggerEnterAction(activeBoxCollider, weaponCollider, GetHitData(this, targetFighter));
                            OnAttackHit?.Invoke(CurrAttack, targetFighter);
                        }
                        else
                        {
                            float damage = CalculateDamage(CurrentMeleeWeapon, CurrAttack.Damage, damagable);
                            damagable.TakeDamage(damage, HitType.Melee);
                            ApplyHitForce(hit.collider, hit.point, direction, damage * 0.5f);
                        }
                        prevGameObj = hit.transform.gameObject;
                    }
                }
            }
            if (activeBoxCollider)
                prevColliderPos = activeBoxCollider.transform.TransformPoint(activeBoxCollider.center);
        }

        void ApplyHitForce(Collider hitCollider, Vector3 hitPoint, Vector3 hitDir, float force)
        {
            // Apply force if the collider has a Rigidbody
            var rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForceAtPosition(hitDir.normalized * force, hitPoint, ForceMode.Impulse);
        }


        public Vector3 GetHitPoint(MeleeFighter attacker)
        {
            Vector3 hitPoint = Vector3.zero;
            attacker.EnableActiveCollider(attacker.CurrAttack);
            if (hitPoint == Vector3.zero)
            {
                attacker.activeCollider.enabled = true;
                hitPoint = core.IsBlocking ? attacker.activeCollider.ClosestPoint(weaponCollider != null ? weaponCollider.transform.position : transform.position) : attacker.activeCollider.ClosestPoint(transform.position);
                attacker.activeCollider.enabled = false;
            }
            attacker.DisableActiveCollider();
            return hitPoint;

        }

        void FindMaxAttackRange()
        {
            core.MaxAttackRange = 0f;
            if (CurrentMeleeWeapon.Attacks != null && CurrentMeleeWeapon.Attacks.Count > 0)
            {
                foreach (var combo in CurrentMeleeWeapon.Attacks)
                {
                    if (combo.MaxDistance > core.MaxAttackRange)
                        core.MaxAttackRange = combo.MaxDistance;
                }
            }
            else
            {
                core.MaxAttackRange = 3f;
            }
        }
        void EnableActiveCollider(AttackData attack)
        {
            switch (attack.HitboxToUse)
            {
                case AttackHitbox.LeftHand:
                    activeCollider = leftHandCollider;
                    break;
                case AttackHitbox.RightHand:
                    activeCollider = rightHandCollider;
                    break;
                case AttackHitbox.LeftFoot:
                    activeCollider = leftFootCollider;
                    break;
                case AttackHitbox.RightFoot:
                    activeCollider = rightFootCollider;
                    break;
                case AttackHitbox.Weapon:
                    if (weaponCollider == null)
                        weaponCollider = CurrentMeleeWeaponObject?.GetComponent<BoxCollider>();
                    activeCollider = weaponCollider;
                    CurrentMeleeBoneWeapon = CurrentMeleeWeaponObject;
                    break;
                case AttackHitbox.LeftElbow:
                    activeCollider = leftElbowCollider;
                    break;
                case AttackHitbox.RightElbow:
                    activeCollider = rightElbowCollider;
                    break;
                case AttackHitbox.LeftKnee:
                    activeCollider = leftKneeCollider;
                    break;
                case AttackHitbox.RightKnee:
                    activeCollider = rightKneeCollider;
                    break;
                case AttackHitbox.Head:
                    activeCollider = headCollider;
                    break;
                default:
                    activeCollider = null;
                    break;
            }

            CurrentMeleeBoneWeapon = attack.HitboxToUse == AttackHitbox.Weapon ? CurrentMeleeWeaponObject : activeCollider?.GetComponent<MeleeWeaponObject>();
            OnEnableHit?.Invoke(CurrentMeleeBoneWeapon);

            if (activeCollider == null)
                Debug.LogError($"There is no collider for {attack.HitboxToUse.ToString()}");
        }
        void DisableActiveCollider()
        {
            activeCollider = null;
        }

        #region Equip UnEquip

        public void OnItemEquipped(EquippableItem itemData)
        {
            if (itemData is MeleeWeapon)
            {
                MeleeWeapon weaponData = itemData as MeleeWeapon;
                weaponData.InIt();
                CurrAttacksList = new List<AttackSlot>();
                core.SetAction(FighterAction.SwitchingWeapon);
                core.TriggerOnWeaponEquip(weaponData);
                FindMaxAttackRange();

                if (CurrentMeleeWeaponObject != null)
                {
                    CurrentMeleeWeaponObject.tag = "Hitbox";
                    weaponCollider = CurrentMeleeWeaponObject.GetComponentInChildren<BoxCollider>();
                }
            }
        }

        public void EquipDefaultWeapon(bool playSwitchingAnimation = true)
        {
            if (core.defaultWeapon != null)
            {
                itemEquipper.EquipItem(core.defaultWeapon, playSwitchingAnimation);
            }
        }

        public void OnItemUnEquipped()
        {
            if (CurrentMeleeWeapon != null)
            {
                if (core.IsBusy) return;

                core.SetAction(FighterAction.SwitchingWeapon);
                core.TriggerOnWeaponUnEquip(CurrentMeleeWeapon);
            }
        }
        void OnDrawGizmos()
        {
            if (moveToPos != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(moveToPos, 0.1f);
            }
        }

        #endregion

        public FighterCore Core => core;
    }
}