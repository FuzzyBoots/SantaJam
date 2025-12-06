using FS_CombatCore;
using FS_Core;
using FS_ShooterSystem;
using FS_ThirdPerson;
using UnityEngine;

namespace FS_Shooter
{
    public enum ShootStates { Aim, Shoot }

    public class ShootState : State<CombatAIController>, IAIState
    {
        private float stateStartTime;
        private float stateDuration;
        private CombatAIController combatAI;

        ShootStates state;

        #region Inspector‐Exposed Fields (Shoot‐Specific)

        [Header("Shooting Settings")]
        [Tooltip("If true, the fighter will try to cover after shooting for some time.")]
        [SerializeField] bool canCover = true;

        [Tooltip("Chance to attempt cover instead of chasing once done shooting")]
        [SerializeField, Range(0f, 1f)] private float coverProbability = 0.8f;

        [Tooltip("Minimum time (seconds) to remain in shooting")]
        [SerializeField] private float minShootTime = 1.5f;

        [Tooltip("Maximum time (seconds) to remain in shooting")]
        [SerializeField] private float maxShootTime = 3f;

        [Header("Movement Speeds for Shooting Logic")]
        [Tooltip("Speed to move when shooting")]
        [SerializeField] private float shootMovementSpeed = 0.6f;

        [Tooltip("Speed when actually “holding still” to shoot")]
        [SerializeField] private float holdStillSpeed = 0f;

        bool isCostFill;

        float aimTimer = 0f;

        #endregion

        ShooterFighter shooter;
        FighterCore fighterCore;

        public AIStates StateKey => AIStates.Shoot;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;

            state = ShootStates.Aim;
            aimTimer = 1f;

            if (shooter == null)
                shooter = combatAI.GetComponent<ShooterFighter>();
            if(fighterCore == null)
                fighterCore = combatAI.GetComponent<FighterCore>();

            // Decide how long we stay in Shoot mode
            stateStartTime = Time.time;
            stateDuration = Random.Range(minShootTime, maxShootTime);

            // Prevent AI from moving if movement range is less than 1
            if (combatAI.GetMovementRange() < 1f)
            {
                shootMovementSpeed = 0f;
                combatAI.NavAgent.autoTraverseOffMeshLink = false;
            }
            shooter.StartAiming();
        }

        public override void Execute()
        {
            if (combatAI.Fighter.Target == null || combatAI.Fighter.Target.IsDead)
            {
                combatAI.ChangeState(AIStates.Idle);
                combatAI.Fighter.Target = null;
                return;
            }

            if (shooter.IsReloading)
            {
                stateStartTime += Time.deltaTime / 2;
            }

            shooter.UpdateAimingTargets();

            Vector3 targetPos = combatAI.Fighter.Target.transform.position;

            combatAI.NavAgent.speed = holdStillSpeed;

            // Maintain preferred range (subtle adjustments)
            if (combatAI.DistanceToTarget > combatAI.Fighter.PreferredFightingRange + 3f && combatAI.DistanceToTarget < combatAI.GetMovementRange())
            {
                Vector3 dir = (combatAI.transform.position - targetPos).normalized;
                Vector3 adjust = targetPos + dir * combatAI.Fighter.PreferredFightingRange;
                combatAI.NavAgent.SetDestination(adjust);
            }
            else
            {
                // Hold position if already at preferred range
                combatAI.NavAgent.SetDestination(combatAI.transform.position);
            }

            // If out of ammo, try to go to cover state
            if (shooter.CurrentShooterWeaponObject.TotalAmmoCount <= 0)
            {
                if (canCover)
                    combatAI.ChangeState(AIStates.Cover);

                return;
            }

            // If line of sight & ammo available → aim & shoot
            Vector3 selfHead = combatAI.GetDetectionRayOrigin();

            Vector3 gunAimReference = shooter.CurrentShooterWeaponObject.ammoSpawnPoint.position;
            Vector3 targetHead = combatAI.GetDetectionRayTarget();
            if (shooter.CurrentWeapon.autoReload && shooter.CurrentShooterWeaponObject.CurrentAmmoCount == 0 && shooter.CurrentShooterWeaponObject.HasAmmo && !shooter.IsShooting && !fighterCore.IsBusy)
            {
                shooter.Reload();
            }
            if (!Physics.SphereCast(selfHead, 0.08f, (targetHead - selfHead).normalized, out RaycastHit hit, Vector3.Distance(selfHead, targetHead), combatAI.obstacleMask))
            {
                state = ShootStates.Shoot;
                shooter.StartAiming();
                shooter.Shoot();
                combatAI.NavAgent.speed = shootMovementSpeed;
            }
            else
            {
                combatAI.NavAgent.speed = shootMovementSpeed;
                if (combatAI.Fighter.Target.animator.GetBool(AnimatorParameters.coverMode))
                {
                    shooter.StartAiming();
                    aimTimer -= Time.deltaTime;
                    if (aimTimer <= 0)
                    {
                        shooter.Shoot();
                    }
                }
                else
                {
                    shooter.StartAiming();
                    combatAI.NavAgent.speed = shootMovementSpeed;
                    combatAI.NavAgent.SetDestination(targetPos);
                    stateStartTime += Time.deltaTime;
                }
            }

            // If our shoot-duration has elapsed → either Cover or Chase
            if (Time.time - stateStartTime >= stateDuration)
            {
                if (canCover && CoverState.i != null && CoverState.i.ShouldCover(combatAI) /*&& dist < enemy.Fighter.PreferredFightingRange + 3f*/)
                {
                    combatAI.ChangeState(AIStates.Cover);
                }
            }
        }

        public override void Exit()
        {
            shooter.StopAiming();

            if (isCostFill)
            {
                var enemyTag = combatAI.GetTagCost();
                enemyTag.currentCost -= combatAI.tagCost.maxCost;
                isCostFill = false;
            }
        }
    }
}
