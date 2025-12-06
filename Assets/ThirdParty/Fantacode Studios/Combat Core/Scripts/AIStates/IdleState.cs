using FS_Core;
using FS_ThirdPerson;
using FS_Util;
using UnityEngine;

namespace FS_CombatCore
{
    public enum IdleStates
    {
        Idle,
        FollowAlly,
        Patrol
    }
    public class IdleState : State<CombatAIController>, IAIState
    {
        [SerializeField]
        [Tooltip("The current idle behavior state for the AI (Idle, FollowAlly, Patrol).")]
        IdleStates idleState = IdleStates.Idle;
        [ShowIf("idleState", 1)]
        [SerializeField]
        [Tooltip("The ally FighterCore to follow")]
        FighterCore followTarget;
        [ShowIf("idleState", 1)]
        [SerializeField]
        [Tooltip("Walking speed when following an ally.")]
        float walkSpeed = 2f;
        [ShowIf("idleState", 1)]
        [SerializeField]
        [Tooltip("Running speed when following an ally.")]
        float runSpeed = 4.5f;

        [ShowIf("idleState", 2)]
        [Tooltip("Root Transform that contains child waypoints")]
        [SerializeField] private Transform wayPoints;

        [ShowIf("idleState", 2)]
        [Tooltip("Speed at which the AI patrols waypoints")]
        [SerializeField] private float patrolSpeed = 2f;

        [ShowIf("idleState", 2)]
        [Tooltip("Distance threshold for switching to next waypoint")]
        [SerializeField] private float waypointArrivalThreshold = 0.5f;

        [ShowIf("idleState", 2)]
        [SerializeField] Vector2 waypointWaitTimeRange = new Vector2(1f, 3f);

        bool isPatrolling = false;

        // Tracks which waypoint-index the AI is targeting currently
        private int currentWayPoint = 0;
        float idleTimer = 0f;

        CombatAIController combatAI;

        public AIStates StateKey => AIStates.Idle;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;

            if(owner.AICombatType == AICombatType.Melee)
                combatAI.Animator?.SetBool(AnimatorParameters.combatMode, false);

            if (idleState == IdleStates.Patrol)
            {
                combatAI.NavAgent.speed = patrolSpeed;

                // Immediately set the first waypoint destination if available:
                if (wayPoints != null && wayPoints.childCount > 0)
                {
                    currentWayPoint = 0;
                    combatAI.NavAgent.SetDestination(wayPoints.GetChild(0).position);
                }

                idleTimer = Random.Range(waypointWaitTimeRange.x, waypointWaitTimeRange.y);
            }
            

            combatAI.Fighter.TargetsInRange.RemoveAll(t => t.IsDead);
        }

        public override void Execute()
        {
            // Try to switch to Combat states
            if (combatAI.Fighter.CurrentWeapon == null) return;

            if (combatAI.Fighter.Target == null)
                combatAI.Fighter.Target = combatAI.FindTarget();
            
            if (combatAI.Fighter.Target != null)
            {
                combatAI.AlertNearbyAllies();

                if (combatAI.AICombatType == AICombatType.Melee)
                    combatAI.ChangeState(AIStates.CombatMovement);
                else
                    combatAI.ChangeState(AIStates.Chase);
            }
            if (idleState == IdleStates.Patrol)
            {
                if (isPatrolling)
                {
                    if (wayPoints == null || wayPoints.childCount == 0)
                        return;

                    // 1) If close enough to current waypoint, advance to next:
                    if (combatAI.NavAgent.remainingDistance <=
                        combatAI.NavAgent.stoppingDistance + waypointArrivalThreshold)
                    {
                        isPatrolling = false;
                        idleTimer = Random.Range(waypointWaitTimeRange.x, waypointWaitTimeRange.y);
                    }
                }
                else
                {
                    idleTimer -= Time.deltaTime;

                    if (idleTimer <= 0f)
                    {
                        isPatrolling = true;
                        currentWayPoint = (currentWayPoint + 1) % wayPoints.childCount;
                        combatAI.NavAgent.SetDestination(wayPoints.GetChild(currentWayPoint).position);
                    }
                }
            }
            else if (idleState == IdleStates.FollowAlly && followTarget != null)
            {
                if (combatAI.CurrentState == AIStates.Idle)
                {
                    if ((followTarget.transform.position - transform.position).magnitude > 8)
                        combatAI.NavAgent.speed = runSpeed;
                    else
                        combatAI.NavAgent.speed = walkSpeed;

                    combatAI.NavAgent.SetDestination(followTarget.transform.position);
                }
            }
        }

        public override void Exit()
        {

        }
    }
}
