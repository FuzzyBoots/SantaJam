using FS_CombatCore;
using FS_Core;
using FS_CoverSystem;
using FS_ThirdPerson;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ShooterSystem
{
    public class CoverState : State<CombatAIController>, IAIState
    {
        private float stateStartTime;
        private float stateDuration;
        private CombatAIController combatAI;
        private CoverHandler coverHandler;
        private ShooterFighter shooter;

        [Header("Cover & Obstacle")]
        private LayerMask coverMask;

        [Tooltip("Maximum number of AIs per single cover collider")]
        [SerializeField] private int maxPerCover = 3;

        [Header("Cover Settings")]
        [Tooltip("Minimum seconds to remain in cover")]
        [SerializeField] private float minCoverTime = 1f;

        [Tooltip("Maximum seconds to remain in cover")]
        [SerializeField] private float maxCoverTime = 3f;

        [Header("Movement Speed in Cover")]
        [Tooltip("Speed at which the AI approaches its cover point")]
        [SerializeField] private float coverApproachSpeed = 3.5f;

        [SerializeField] private float maxCoverHeightMultiplier = 3f;

        [SerializeField] float distanceWeight = 1f;
        [SerializeField] float distanceToTargetWeight = 1f;
        [SerializeField] float occupantWeight = 10f;

        [Tooltip("Avoid taking cover when too close to the target")]
        [SerializeField] float coverAvoidDistance = 2;

        [Tooltip("Covers that are below this distance won't be choosen")]
        [SerializeField] float minCoverDistance = 2;

        public float coverRange = 10f; // Range to search for cover colliders

        private static readonly Collider[] _coverOverlapBuffer = new Collider[32]; // Adjust size as needed
        private float lastCoverCheckTime = 0f;

        public class CoverPoint
        {
            public Vector3 Position;
            public Vector3 PositionOffset;
            public Collider CoverCollider;
            public CombatAIController combatAI;
            public CoverPoint(Vector3 p, Collider c, CombatAIController e, Vector3 positionOffset)
            {
                Position = p;
                CoverCollider = c;
                combatAI = e;
                PositionOffset = positionOffset;
            }
        }

        // Updated to store CoverPoint instead of CombatAIController
        private static Dictionary<Collider, List<CoverPoint>> CoverPointAlleys = new Dictionary<Collider, List<CoverPoint>>();

        private CoverPoint _currentCoverPoint;

        public CoverPoint CurrentCoverPoint
        {
            get => _currentCoverPoint;
            set
            {
                // Remove the current cover point from the list if it's being unset
                if (_currentCoverPoint != null && CoverPointAlleys.TryGetValue(_currentCoverPoint.CoverCollider, out var currentList))
                {
                    currentList.Remove(_currentCoverPoint);
                    if (currentList.Count == 0)
                        CoverPointAlleys.Remove(_currentCoverPoint.CoverCollider);
                }

                // Update the current cover point
                _currentCoverPoint = value;

                // Add the new cover point to the list if it's being set
                if (_currentCoverPoint != null)
                {
                    if (!CoverPointAlleys.TryGetValue(_currentCoverPoint.CoverCollider, out var newList))
                    {
                        newList = new List<CoverPoint>();
                        CoverPointAlleys[_currentCoverPoint.CoverCollider] = newList;
                    }
                    newList.Add(_currentCoverPoint);
                }
            }
        }

        public AIStates StateKey => AIStates.Cover;

        public static CoverState i { get; private set; }
        private void Awake()
        {
            i = this;
            CoverPointAlleys = new Dictionary<Collider, List<CoverPoint>>();
            coverMask = LayerMask.GetMask("Cover");
        }

        public override void Enter(CombatAIController owner)
        {
            combatAI = owner;
            if (coverHandler == null)
                coverHandler = GetComponent<CoverHandler>();
            if (shooter == null)
                shooter = GetComponent<ShooterFighter>();

            // Decide how long to stay in cover
            stateStartTime = Time.time;
            stateDuration = Random.Range(minCoverTime, maxCoverTime);

            HandleCoverMode();
            if (CurrentCoverPoint?.CoverCollider == null)
            {
                combatAI.ChangeState(AIStates.Shoot);
                return;
            }

            // Set agent speed to the serialized coverApproachSpeed
            combatAI.NavAgent.speed = coverApproachSpeed;
        }

        public override void Execute()
        {
            if (CurrentCoverPoint?.CoverCollider == null)
            {
                combatAI.ChangeState(AIStates.Shoot);
                return;
            }

            combatAI.NavAgent.SetDestination(CurrentCoverPoint.Position);

            if (coverHandler.InCover && combatAI.NavAgent.remainingDistance >
                    combatAI.NavAgent.stoppingDistance + 0.1f)
            {
                coverHandler.GoOutOfCover();
            }

            if (!coverHandler.InCover)
            {

                if (combatAI.NavAgent.remainingDistance <=
                    combatAI.NavAgent.stoppingDistance + 0.1f && !combatAI.NavAgent.pathPending)
                {
                    if (CurrentCoverPoint.PositionOffset != Vector3.zero)
                    {
                        CurrentCoverPoint.PositionOffset = Vector3.zero; // Reset offset after reaching cover
                                                                         // return;
                    }

                    coverHandler.GoToCover();
                }
            }
            else
            {
                Vector3 coverDir = -CurrentCoverPoint.CoverCollider.transform.forward;
                coverDir.y = 0;
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(coverDir), Time.deltaTime * 400f);

                Vector3 startPos = transform.position;
                startPos.y = 0.8f;
                int iterations = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (Physics.SphereCast(startPos, 0.1f, transform.forward, out _, 1f, combatAI.obstacleMask))
                    {
                        startPos.y += 0.1f;
                        iterations++;
                    }
                    else
                        break;
                }
                combatAI.Animator.SetFloat("CoverType", (1 - (iterations / 10f)) * maxCoverHeightMultiplier, 0.2f, Time.deltaTime);
            }

            Vector3 headPos = CurrentCoverPoint.Position;
            Vector3 targetPos = combatAI.Fighter.Target.transform.position + Vector3.up * 2f;


            if (Time.time - lastCoverCheckTime >= 0.5f)
            {
                lastCoverCheckTime = Time.time;
                if (!Physics.Raycast(headPos, targetPos - headPos, out RaycastHit hit, (targetPos - headPos).magnitude, combatAI.obstacleMask))
                    HandleCoverMode();
            }

            if (Time.time - stateStartTime >= stateDuration)
            {
                if (/*combatAI.CanAttack &&*/ shooter.CurrentShooterWeaponObject.TotalAmmoCount > 0)
                    combatAI.ChangeState(AIStates.Shoot);
            }
        }

        public override void Exit()
        {
            CurrentCoverPoint = null;
            coverHandler.GoOutOfCover();
        }

        private void HandleCoverMode()
        {
            CoverPoint newCoverPoint = null;

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                combatAI.GetMovementRange() < coverRange? combatAI.GetMovementRange() : coverRange,
                _coverOverlapBuffer,
                coverMask,
                QueryTriggerInteraction.Collide
            );

            List<CoverPoint> coverPoints = new List<CoverPoint>();

            for (int i = 0; i < hitCount; i++)
            {
                var col = _coverOverlapBuffer[i];
                if (col == null) continue;

                // Only choose covers that don't face the target
                var vecToTarget = transform.position - combatAI.Fighter.Target.transform.position;
                //if (Vector3.Angle(col.transform.forward, vecToTarget) > 90f)
                //    continue;

                // If the target is in cover, then don't choose a close cover point to him
                bool targetInCover = combatAI.Fighter.Target?.animator.GetBool(AnimatorParameters.coverMode) ?? false;
                float distanceToCover = Vector3.Distance(col.transform.position, combatAI.Fighter.Target.transform.position);
                if (distanceToCover < minCoverDistance)
                    continue;

                Vector3? foundPoint = GetBestCoverPoint(col, transform.position);
                if (!foundPoint.HasValue)
                    continue;
                Vector3 coverPos = foundPoint.Value;
                //Vector3 targetPos = enemy.Fighter.Target.transform.position + Vector3.up * 2f;
                //if (Physics.Raycast(coverPos, targetPos - coverPos, out RaycastHit hit, (targetPos - coverPos).magnitude, enemy.obstacleMask))
                coverPoints.Add(new CoverPoint(coverPos, col, combatAI, col.transform.forward * 0.5f));
            }

            if (coverPoints.Count > 0)
            {
                coverPoints.Sort((a, b) =>
                {
                    // 1) Compute how many enemies are already on each collider:
                    int countA = 0, countB = 0;

                    if (occupantWeight != 0)
                    {
                        if (CoverPointAlleys.TryGetValue(a.CoverCollider, out var listA))
                            countA = listA.Count;
                        if (CoverPointAlleys.TryGetValue(b.CoverCollider, out var listB))
                            countB = listB.Count;
                    }

                    float distA, distB, disToTargetA, disToTargetB;
                    distA = distB = disToTargetA = disToTargetB = 0;

                    // 2) Compute squared distance from this AI to each cover‐point:
                    if (distanceWeight != 0)
                    {
                        distA = Vector3.SqrMagnitude(transform.position - a.Position);
                        distB = Vector3.SqrMagnitude(transform.position - b.Position);
                    }

                    if (distanceToTargetWeight != 0)
                    {
                        disToTargetA = Vector3.SqrMagnitude(combatAI.Fighter.Target.transform.position - a.Position);
                        disToTargetB = Vector3.SqrMagnitude(combatAI.Fighter.Target.transform.position - b.Position);
                    }

                    // 3) Build a “weight” that is (distance + occupant‐penalty):
                    float weightA = distA * distanceWeight + disToTargetA * distanceToTargetWeight + countA * occupantWeight;
                    float weightB = distB * distanceWeight + disToTargetB * distanceToTargetWeight + countB * occupantWeight;

                    return weightA.CompareTo(weightB);
                });

                CoverPoint chosenCover = coverPoints[0];
                newCoverPoint = chosenCover;
            }

            if (newCoverPoint != CurrentCoverPoint)
                CurrentCoverPoint = newCoverPoint;
        }

        public Vector3? GetBestCoverPoint(Collider collider, Vector3 position)
        {
            // Try right and left independently
            Vector3? rightPoint = getCoverPointFromCollider(collider, position);
            Vector3? leftPoint = getCoverPointFromCollider(collider, position, true);

            if (rightPoint == null && leftPoint == null)
                return null;
            if (rightPoint == null)
                return leftPoint;
            if (leftPoint == null)
                return rightPoint;

            // Both non-null: return whichever is closer to the AI's position
            float distRightSq = Vector3.SqrMagnitude(rightPoint.Value - position);
            float distLeftSq = Vector3.SqrMagnitude(leftPoint.Value - position);

            return (distRightSq <= distLeftSq) ? rightPoint : leftPoint;
        }

        public Vector3? getCoverPointFromCollider(Collider collider, Vector3 position, bool checkToLeft = false)
        {
            Vector3 closestCoverPoint = collider.ClosestPoint(position);
            Vector3 coverPos = closestCoverPoint;
            Vector3 targetPos = combatAI.Fighter.Target.transform.position + Vector3.up * 2f;

            if (CoverPointAlleys.ContainsKey(collider))
            {
                var direction = checkToLeft ? -collider.transform.right : collider.transform.right;
                direction *= 1f;
                List<CoverPoint> alleys = CoverPointAlleys[collider];

                while (true)
                {
                    //if (isCloseToPoint(alleys, closestCoverPoint, 1f))
                    if(Physics.CheckSphere(closestCoverPoint,0.4f,gameObject.layer) )//|| isCloseToPoint(alleys, closestCoverPoint, 0.5f))
                    {
                        var newCoverPoint = collider.ClosestPoint(closestCoverPoint + direction);
                        if (Vector3.Magnitude(closestCoverPoint.SetY(0) - newCoverPoint.SetY(0)) < 0.6f)
                        {
                            return null;
                        }
                        closestCoverPoint = newCoverPoint;
                    }
                    else
                    {
                        if (Physics.Raycast(coverPos, targetPos - coverPos, out RaycastHit hit, 2f, combatAI.obstacleMask))
                            return closestCoverPoint;
                        else 
                            return null;
                    }
                }
            }
            else
            {
                if (Physics.Raycast(coverPos, targetPos - coverPos, out RaycastHit hit, 2f, combatAI.obstacleMask))
                    return closestCoverPoint;
                else
                    return null;
            }
        }

        public bool isCloseToPoint(List<CoverPoint> alleys, Vector3 point, float maxDistance)
        {
            //return !alleys.All(cp => Vector3.Magnitude(cp.Position.SetY(0) - point.SetY(0)) >= maxDistance);
            foreach (var cp in alleys)
            {
                if (Vector3.Magnitude(cp.Position.SetY(0) - point.SetY(0)) < maxDistance )
                    return false;
            }
            return true;
        }

        private int CalculateTotalAlleys(BoxCollider collider, float alleyWidth)
        {
            float scaledWidth = collider.size.x * collider.transform.localScale.x * collider.transform.parent.localScale.x;
            return Mathf.FloorToInt(scaledWidth / alleyWidth);
        }

        public bool ShouldCover(CombatAIController owner)
        {
            // If too close, don't cover
            if (owner.DistanceToTarget < coverAvoidDistance)
                return false;

            // If the enemy has clear line of sight, then don't cover
            var origin = transform.position + Vector3.up;
            var targetPos = owner.Fighter.Target.transform.position + Vector3.up * 0.3f;

            if (!Physics.SphereCast(origin, 0.08f, (targetPos - origin).normalized, out RaycastHit hit, Vector3.Distance(targetPos, origin), owner.obstacleMask))
                return false;

            return true;
        }
    }
}
