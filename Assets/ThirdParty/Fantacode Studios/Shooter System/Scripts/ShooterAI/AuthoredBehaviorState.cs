using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ShooterSystem
{

    public class AuthoredBehaviorState : State<CombatAIController>, IAIState
    {
        [Header("Waypoints")]
        [Tooltip("Root Transform that contains child waypoints")]
        [SerializeField] Transform wayPointNetwork;
        [SerializeField] float waypointArrivalThreshold = 0.5f;

        [SerializeField] float movementSpeed = 4f;

        [Tooltip("State to go to after the authored movement is over")]
        [HideInInspectorEnum(3,5,9,10,11)]
        [SerializeField] AIStates endState;

        [SerializeField] bool aimWhileMoving = false;

        CombatAIController combatAI;

        List<Transform> wayPoints;
        int currentWayPoint = 0;

        public AIStates StateKey => AIStates.AuthoredBehavior;

        ShooterFighter shooter;

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;
            if (shooter == null)
                shooter = combatAI.GetComponent<ShooterFighter>();

            combatAI.NavAgent.speed = movementSpeed;

            // Immediately set the first waypoint destination if available:
            if (wayPointNetwork != null && wayPointNetwork.childCount > 0)
            {
                wayPoints = new List<Transform>();
                foreach (Transform child in wayPointNetwork.transform)
                    wayPoints.Add(child);

                currentWayPoint = 0;
                combatAI.NavAgent.SetDestination(wayPoints[currentWayPoint].position);

                if (aimWhileMoving)
                    shooter.StartAiming();
            }
        }

        public override void Execute()
        {
            if (wayPoints == null || wayPoints.Count == 0)
            {
                combatAI.ChangeState(endState);
                return;
            }

            if (aimWhileMoving)
                shooter.UpdateAimingTargets();

            if (combatAI.NavAgent.remainingDistance <=
                    combatAI.NavAgent.stoppingDistance + waypointArrivalThreshold)
            {
                if (currentWayPoint < wayPoints.Count - 1)
                {
                    currentWayPoint++;
                    combatAI.NavAgent.SetDestination(wayPoints[currentWayPoint].position);
                }
                else
                {
                    // Reached final waypoint
                    combatAI.ChangeState(endState);
                    return;
                }
            }
        }

        public override void Exit()
        {
            
        }
    }
}
