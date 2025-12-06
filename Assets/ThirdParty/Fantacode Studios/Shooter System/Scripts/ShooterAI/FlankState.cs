using FS_CombatCore;
using FS_Core;
using FS_ShooterSystem;
using UnityEngine;

namespace FS_Shooter
{
    public class FlankState : State<CombatAIController>, IAIState
    {
        private CombatAIController combatAI;

        #region Inspector‐Exposed Fields (Chase‐Specific)

        [Header("Chase Settings")]
        [Tooltip("Speed at which the AI chases the player")]
        [SerializeField] private float flankSpeed = 2f;
        [SerializeField] private float shootMovementSpeed = 1f;
        ShooterFighter shooter;


        #endregion

        float moveBackTimer = 2f;
        Vector3 moveBackPos;

        public AIStates StateKey => AIStates.Flank;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;

            if (shooter == null)
                shooter = combatAI.GetComponent<ShooterFighter>();
            // Set agent speed to the serialized chaseSpeed
            combatAI.NavAgent.speed = flankSpeed;
            shooter.StartAiming();

            //moveBackTimer = 2f;
            //moveBackPos = transform.position - transform.forward;
        }

        public override void Execute()
        {
            if (shooter.IsAiming)
                combatAI.NavAgent.speed = shootMovementSpeed;
            else
            {
                combatAI.NavAgent.speed = flankSpeed;
                shooter.StartAiming();
            }

            if (moveBackTimer > 0)
            {
                moveBackTimer -= Time.deltaTime;
                combatAI.NavAgent.SetDestination(moveBackPos);
            }
            else
            {
                // 1) Always set nav destination to player’s current position
                combatAI.NavAgent.SetDestination(combatAI.Fighter.Target.transform.position);
            }

            Vector3 selfHead = combatAI.GetDetectionRayOrigin();

            Vector3 gunAimReference = shooter.CurrentShooterWeaponObject.ammoSpawnPoint.position;
            Vector3 targetHead = combatAI.GetDetectionRayTarget();

            if (!Physics.SphereCast(selfHead, 0.08f, (targetHead - selfHead).normalized, out RaycastHit hit, Vector3.Distance(selfHead, targetHead), combatAI.obstacleMask)
                )
            {
                shooter.StartAiming();
                if (shooter.CurrentShooterWeaponObject.TotalAmmoCount > 0)
                    shooter.Shoot();
                combatAI.NavAgent.SetDestination(transform.position);
            }
            if ((combatAI.Fighter.transform.position - transform.position).magnitude > combatAI.Fighter.PreferredFightingRange)
                combatAI.ChangeState(AIStates.Cover);

            shooter.UpdateAimingTargets();
        }
        public override void Exit()
        {
            // Nothing specific to clean up for Chase
        }
    }
}
