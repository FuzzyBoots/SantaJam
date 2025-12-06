using FS_Core;
using FS_ParkourSystem;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace FS_ParkourAI
{
    

    public class AIController : MonoBehaviour
    {

        IAICharacter character;

        public bool useMultiDirectionalAnimation;
        public float animationRunSpeed = 4.5f; // changeable

        ClimbAI climbAI;
        ClimbController playerClimbController;
        ParkourController playerParkour;

        public FollowType followType;

        public GameObject player;
        public GameObject target;
        public WayPointsNetwork followWayPoints;

        [Space(10)]
        [Tooltip("If turned off the parkour agent will not follow through climb points")]
        public bool followThroughClimbPoints = true;

        [Tooltip("If turned off the parkour agent will try to parkour even if another agent or player is blocking the path")]
        public bool enableSpaceChecking = true;

        public bool dynamicPredictiveJump = true;

        NavMeshAgent navMeshAgent;
        Animator animator;
        float agentRadius;
        float stoppingDistance;
        ParkourAI parkourAI;
        public bool isFalling;
        float ySpeed;

        float hipYOffset;

        void OnEnable()
        {
            character = GetComponent<IAICharacter>();
            animator = character.Animator;
            navMeshAgent = character.NavMeshAgent;
            navMeshAgent.autoTraverseOffMeshLink = false;
            agentRadius = navMeshAgent.radius;
            stoppingDistance = navMeshAgent.stoppingDistance;
            parkourAI = GetComponent<ParkourAI>();
            climbAI = GetComponent<ClimbAI>();
            Pathfinder.neighbourList = new();
            if (player != null)
            {
                playerClimbController = player.GetComponent<ClimbController>();
                playerParkour = player.GetComponent<ParkourController>();
            }

            hipYOffset = Mathf.Abs(navMeshAgent.transform.position.y - this.transform.position.y);
        }
        void FixedUpdate()
        {
            if (parkourAI.ControlledByParkour || isFalling || !navMeshAgent.enabled) return;

            if (followType == FollowType.FollowPlayer)
            {
                if (player == null) { Debug.Log("Player not assigned"); return; }
                if (!navMeshAgent.pathPending)
                    navMeshAgent.SetDestination(player.transform.position);
            }

            else if (followType == FollowType.FollowTarget)
            {
                if (target == null) { Debug.Log("Target not assigned"); return; }
                if (!navMeshAgent.pathPending)
                    navMeshAgent.SetDestination(target.transform.position);
            }

            else if (followType == FollowType.FollowWayPoints)
            {
                if (followWayPoints == null) { Debug.Log("FollowWayPoints not assigned"); return; }
                HandleWayPoints(followWayPoints.transform);
            }

            else if (followType == FollowType.RandomWanderer)
            {
                HandleRandomPoints();
            }

            else if (followType == FollowType.RunFromPlayer)
            {
                if (player == null) { Debug.Log("Player not assigned"); return; }
                HandleRunFromPlayer();
            }

            else if (followType == FollowType.Stop)
            {
                animator.SetFloat("moveAmount", 0, 0.1f, Time.deltaTime);
                navMeshAgent.isStopped = true;
                return;
            }

            HandleUpdate();
        }
        private void HandleMovementUpdate()
        {
            var characterVelocity = navMeshAgent.velocity;
            characterVelocity.y = 0;


            float forwardSpeed = Vector3.Dot(characterVelocity, transform.forward);
            animator.SetFloat(AnimatorParameters.moveAmount, forwardSpeed / animationRunSpeed, 0.2f, Time.deltaTime);

            float strafeSpeed = Vector3.Dot(characterVelocity, transform.right);
            animator.SetFloat(AnimatorParameters.strafeAmount, strafeSpeed / animationRunSpeed, 0.2f, Time.deltaTime);
        }

        void HandleUpdate()
        {
            animator.SetFloat("locomotionType", useMultiDirectionalAnimation ? 1 : 0);

            if (followType != FollowType.None)
                HandleMovementUpdate();
            //animator.SetFloat("moveAmount", navMeshAgent.velocity.magnitude / 4.5f, 0.2f, Time.deltaTime);

            if (parkourAI.enableBalanceWalk) parkourAI.HandleBalanceOnNarrowBeam();

            if (navMeshAgent.isOnOffMeshLink)
            {
                if (navMeshAgent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeDropDown || navMeshAgent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross)
                {
                    if (NavMesh.SamplePosition(navMeshAgent.currentOffMeshLinkData.startPos, out var hit, 1f, 1) && NavMesh.SamplePosition(navMeshAgent.currentOffMeshLinkData.endPos, out hit, 1f, 1))
                    {
                        HandleJumpLink(navMeshAgent.currentOffMeshLinkData.startPos, navMeshAgent.currentOffMeshLinkData.endPos);
                        return;
                    }
                }
                if (navMeshAgent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeManual)
                {
                    CustomNavmeshLink link = (CustomNavmeshLink)navMeshAgent.navMeshOwner;
                    if (link.area == NavMesh.GetAreaFromName("Jump"))
                    {
                        HandleJumpLink(navMeshAgent.currentOffMeshLinkData.startPos, navMeshAgent.currentOffMeshLinkData.endPos);
                        return;
                    }
                    if (link.area == NavMesh.GetAreaFromName("Climbable"))
                    {
                        StartCoroutine(HandleClimbing(link));
                        return;
                    }
                }
            }
            navMeshAgent.isStopped = false;
        }

        public bool HandleParkour(Vector3 startPoint, Vector3 endPoint)
        {
            ObstacleHitData hitData = new();
            hitData.heightHit.point = endPoint;
            hitData.hasSpace = hitData.forwardHitFound = hitData.heightHitFound = true;

            var forward = (hitData.heightHit.point - transform.position);
            forward.y = 0;
            forward.Normalize();
            var right = Vector3.Cross(forward, Vector3.up);

            hitData.hasSpaceToVault = true;

            if (Physics.Raycast(navMeshAgent.currentOffMeshLinkData.startPos + Vector3.up * 0.5f, forward, out RaycastHit navMeshForwardHit, 2f, parkourAI.ObstacleLayer))
            {
                hitData.forwardHit.normal = navMeshForwardHit.normal;
                if (Physics.SphereCast(navMeshForwardHit.point + Vector3.up * 2f, 0.2f, Vector3.down, out RaycastHit HeightHit, 2f, parkourAI.ObstacleLayer))
                {
                    if (HeightHit.point.y - endPoint.y > 0.5f)
                    {
                        hitData.heightHit.point = HeightHit.point;
                        hitData.hasSpaceToVault = false;
                    }
                }
            }
            hitData.forwardHit.normal = Vector3.forward;

            hitData.heightHit.normal = Vector3.up;
            //hitData.heightHit.normal = navMeshHeightHit.normal;

            var forwardOrigin = hitData.heightHit.point;
            forwardOrigin.y = hitData.heightHit.point.y + 0.4f;

            if (enableSpaceChecking)
            {
                var Collider = Physics.OverlapSphere(forwardOrigin + forward * 0.4f, 0.35f, Physics.IgnoreRaycastLayer);
                if (Collider.Any(x => x.gameObject != gameObject))
                {
                    //navMeshAgent.isStopped = true;
                    navMeshAgent.Warp(navMeshAgent.transform.position);
                    return true;
                }
            }

            var action = parkourAI.GetParkourAction(hitData);

            if (action != null)
            {
                if (hitData.hasSpaceToVault)
                    StartCoroutine(LerpToPos(startPoint, () => StartCoroutine(parkourAI.DoParkourAction(action))));
                else
                    StartCoroutine(parkourAI.DoParkourAction(action));
                return true;

            }
            navMeshAgent.Warp(navMeshAgent.transform.position);
            return false;
        }

        public void HandleJumpLink(Vector3 startPoint, Vector3 endPoint)
        {
            if (NavMesh.SamplePosition(startPoint, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                startPoint = hit.position;

            if (NavMesh.SamplePosition(endPoint, out hit, 1f, NavMesh.AllAreas))
                endPoint = hit.position;

            var dis = endPoint - transform.position;
            var disXZ = dis;
            var disY = disXZ.y;
            disXZ.y = 0;

            if (disXZ.magnitude < 2f && disY > -0.2f)
            {
                if (disY < -0.2f)
                {
                    StartCoroutine(HandleFalling("Jump Down", endPoint));
                    return;
                }
                if (HandleParkour(startPoint, endPoint))
                    return;
                if (disXZ.magnitude < 1f && Mathf.Abs(disY) < 0.2f)
                {
                    StartCoroutine(LerpToPos(endPoint, () => character.OnEndAction()));
                    return;
                }
                if (disY > 2.5f)
                {
                    navMeshAgent.Warp(navMeshAgent.transform.position);
                    return;
                }
            }

            if (followType == FollowType.FollowPlayer && dynamicPredictiveJump)
                endPoint = GetPointClosestToTarget(transform.position, endPoint);
            else
                endPoint = GetFurthestJumpablePoint(endPoint);

            HandleJump(endPoint);
        }
        public void HandleJump(Vector3 jumpPoint)
        {
            if (NavMesh.SamplePosition(jumpPoint, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                jumpPoint = hit.position;

            var disXZ = jumpPoint - transform.position;
            var disY = disXZ.y;
            disXZ.y = 0;
            var upOffset = Vector3.up * 0.5f; // head Offset
            var h = parkourAI.getJumpHeight(disY, disXZ);

            var right = Vector3.Cross(disXZ.normalized, Vector3.up);

            var heightestPoint = transform.position + disXZ / 2 + Vector3.up * h + upOffset;

            if (enableSpaceChecking && (Physics.Linecast(transform.position + upOffset, heightestPoint, Physics.IgnoreRaycastLayer)
               || Physics.Linecast(heightestPoint, jumpPoint + upOffset + disXZ.normalized * 0.3f, Physics.IgnoreRaycastLayer)
               || Physics.Linecast(heightestPoint + right * 0.3f, jumpPoint + right * 0.3f + upOffset + disXZ.normalized * 0.3f, Physics.IgnoreRaycastLayer)
               || Physics.Linecast(heightestPoint - right * 0.3f, jumpPoint - right * 0.3f + upOffset + disXZ.normalized * 0.3f, Physics.IgnoreRaycastLayer)))
            {
                navMeshAgent.Warp(navMeshAgent.transform.position);
                return;
            }
            JumpData jumpData = new();
            jumpData.hasSpaceToLand = true;
            jumpData.footPosition = jumpData.rootPosition = jumpPoint;
            StartCoroutine(parkourAI.DoPredictiveJump(jumpData));
        }
        Vector3 GetPointClosestToTarget(Vector3 offMeshStartPos, Vector3 offMeshEndPos)
        {
            float h = parkourAI.maxJumpHeight + (offMeshStartPos.y - player.transform.position.y);

            if (h < 0)
                return GetFurthestJumpablePoint(offMeshEndPos);

            // Don't just drop down directly, try to do a predictive jump
            var vecToTarget = player.transform.position - offMeshStartPos;

            float maxJumpDistance = getMaxJumpDistance(h);

            var jumpVector = Vector3.ClampMagnitude(vecToTarget, Mathf.Min(maxJumpDistance, vecToTarget.magnitude));

            //if (vecToTarget.magnitude - 2f < maxJumpDistance) { 
            //    var right = Vector3.Cross(vecToTarget, Vector3.up);
            //    jumpVector += right * 2f;
            // }

            bool jumpHitFound = NavMesh.SamplePosition(offMeshStartPos + jumpVector, out NavMeshHit jumpHit, 1f, NavMesh.AllAreas);

            var disXZ = jumpHit.position - transform.position;
            var disY = disXZ.y;
            disXZ.y = 0;
            var upOffset = Vector3.up * 0.5f; // head Offset
            var height = parkourAI.getJumpHeight(disY, disXZ);
            var heightestPoint = transform.position + disXZ / 2 + Vector3.up * height + upOffset;

            if (!jumpHitFound || Physics.Linecast(transform.position + upOffset, heightestPoint, parkourAI.ObstacleLayer)
                || Physics.Linecast(heightestPoint, jumpHit.position + upOffset + disXZ.normalized * 0.3f, parkourAI.ObstacleLayer))
            {
                return GetFurthestJumpablePoint(offMeshEndPos);
            }

            return jumpHit.position;
        }
        public Vector3 GetFurthestJumpablePoint(Vector3 jumpPoint)
        {
            float h = parkourAI.maxJumpHeight + (transform.position.y - jumpPoint.y);

            if (h < 0) return jumpPoint;

            float maxJumpDistance = getMaxJumpDistance(h);

            var disToPlayer = jumpPoint - transform.position;
            disToPlayer.y = 0;

            var corners = navMeshAgent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                if ((corners[i] - jumpPoint).sqrMagnitude < 0.2f)
                {
                    var difference = corners[i + 1] - corners[i];
                    //if (corners[i].y - transform.position.y < -0.4f && Mathf.Abs(difference.y) < 0.3f)
                    //{
                    var maxJumpPoint = transform.position + disToPlayer.normalized * maxJumpDistance;
                    return GetClosestPointOnFiniteLine(maxJumpPoint, corners[i], corners[i + 1]);
                    //}
                }
            }
            if (NavMesh.SamplePosition(jumpPoint, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                jumpPoint = hit.position;
            return jumpPoint;
        }
        Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
        {
            Vector3 line_direction = line_end - line_start;
            float line_length = line_direction.magnitude;
            line_direction.Normalize();
            float project_length = Mathf.Clamp(Vector3.Dot(point - line_start, line_direction), 0f, line_length);
            return line_start + line_direction * project_length;
        }

        float getMaxJumpDistance(float h)
        {
            float airTime = Mathf.Sqrt(-2 * parkourAI.maxJumpHeight / character.Gravity) + Mathf.Sqrt(2 * h / -character.Gravity);
            return airTime * parkourAI.forwardJumpSpeed;
        }

        public IEnumerator LerpToPos(Vector3 pos, Action complete = null)
        {
            var dir = pos - transform.position;
            dir.y = 0;
            Quaternion targetRot;
            if (dir != Vector3.zero)
                targetRot = Quaternion.LookRotation(dir);
            else
                targetRot = Quaternion.LookRotation(transform.forward);

            character.OnStartAction();
            parkourAI.InAction = true;

            var dist = (transform.position - pos).sqrMagnitude;
            while (dist > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 500f * Time.deltaTime);

                transform.position = Vector3.MoveTowards(transform.position, pos, 4f * animator.GetFloat("moveAmount") * Time.deltaTime);

                dist = (transform.position - pos).sqrMagnitude;
                animator.SetFloat("moveAmount", 1f, 0.6f, Time.deltaTime);
                yield return null;
            }
            parkourAI.InAction = false;
            //OnEndParkourAction();
            StartCoroutine(TweenVal(animator.GetFloat("moveAmount"), 0, 0.15f, (lerpVal) => { animator.SetFloat("moveAmount", lerpVal); })); ;
            complete?.Invoke();
        }

        public IEnumerator HandleClimbing(CustomNavmeshLink link)
        {
            Collider[] startLedge, endLedge;

            if (link.startLedge != null) startLedge = new Collider[] { link.startLedge };
            else
                startLedge = Physics.OverlapBox(link.startPoint + Vector3.up * 1.4f, new Vector3(0.8f, 1.6f, 0.8f), Quaternion.identity, climbAI.LedgeLayer);

            if (link.endLedge != null) endLedge = new Collider[] { link.endLedge };
            else
                endLedge = Physics.OverlapBox(link.endPoint + Vector3.up * 1.4f, new Vector3(0.8f, 1.6f, 0.8f), Quaternion.identity, climbAI.LedgeLayer);

            if (link.startLedge == null || link.endLedge == null)
            {
                //link.enabled = false;
                navMeshAgent.Warp(navMeshAgent.transform.position);
                Debug.Log(gameObject.name + ": Ledge not detected");
                yield break;
            }

            //yield return new WaitForFixedUpdate();

            var linkStartPoint = link.endPoint;
            var linkEndPoint = link.endPoint;

            if (NavMesh.SamplePosition(link.startPoint, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                linkStartPoint = link.startPoint;

            if (NavMesh.SamplePosition(link.endPoint, out hit, 0.5f, NavMesh.AllAreas))
                linkEndPoint = link.endPoint;

            var startPoint = climbAI.GetNearestPoint(startLedge[0].transform, transform.position);
            var endPoint = climbAI.GetNearestPoint(endLedge[0].transform, link.endPoint);

            if (startPoint == null || endPoint == null)
            {
                //link.enabled = false;
                navMeshAgent.Warp(navMeshAgent.transform.position);
                Debug.Log(gameObject.name + ": point not detected");
                yield break;
            }

            if ((endPoint.position - transform.position).sqrMagnitude < (startPoint.position - transform.position).sqrMagnitude)
            {
                (startLedge, endLedge) = (endLedge, startLedge);
                (startPoint, endPoint) = (endPoint, startPoint);
            }
            if ((link.endPoint - transform.position).sqrMagnitude < (link.startPoint - transform.position).sqrMagnitude)
            {
                (linkStartPoint, linkEndPoint) = (linkEndPoint, linkStartPoint);
            }


            var points = Pathfinder.FindPath(startPoint, endPoint);
            if (points == null && startLedge[0] == endLedge[0])
            {
                endPoint = startPoint;
                points = Pathfinder.FindPath(startPoint, endPoint);
            }

            if (points == null)
            {
                //link.enabled = false;
                navMeshAgent.Warp(navMeshAgent.transform.position);
                Debug.Log(gameObject.name + ": Path not detected");
                yield break;
            }

            parkourAI.InAction = true;
            character.OnStartAction();
            StartCoroutine(TweenVal(animator.GetFloat("moveAmount"), 0, 0.15f, (lerpVal) => { animator.SetFloat("moveAmount", lerpVal); })); ;


            if (followType == FollowType.FollowPlayer)
            {
                yield return HandleClimbingToPlayer(link, linkStartPoint, linkEndPoint, startPoint, endPoint);
            }

            else if (followType == FollowType.FollowTarget)
            {
                yield return HandleClimbingToTarget(link, linkStartPoint, linkEndPoint, startPoint, endPoint, target.transform.position);
            }

            else if (followType == FollowType.FollowWayPoints)
            {
                yield return HandleClimbingToTarget(link, linkStartPoint, linkEndPoint, startPoint, endPoint, wayPoint.position);
            }
            else
            {
                yield return HandleClimbingToTarget(link, linkStartPoint, linkEndPoint, startPoint, endPoint, navMeshAgent.destination);
            }
        }

        IEnumerator HandleClimbingToPlayer(CustomNavmeshLink link, Vector3 linkStartPoint, Vector3 linkEndPoint, ClimbPoint startPoint, ClimbPoint endPoint)
        {
            while (true)
            {
                ClimbPoint nextPoint = null;

                var nextPointToPlayer = followThroughClimbPoints && playerParkour.IsHanging && parkourAI.IsHanging ? Pathfinder.FindPath(climbAI.currentPoint, playerClimbController.currentPoint, true) : null;

                if (!playerParkour.IsHanging || nextPointToPlayer == null || Vector3.Distance(nextPointToPlayer.Last().position, player.transform.position) > 3f) // pathToPlayer.Last() != playerClimbController.currentPoint &&
                {
                    bool pathCheck;
                    if (followThroughClimbPoints)
                        pathCheck = CheckPathWithAgent(link, linkStartPoint, linkEndPoint, player.transform.position);
                    else
                        pathCheck = CheckPath(link, linkStartPoint, linkEndPoint, player.transform.position);
                    if (!parkourAI.IsHanging)
                    {
                        if (!pathCheck)
                        {
                            parkourAI.InAction = false;
                            OnEndClimbAction();
                            yield break;
                        }
                        else
                            nextPoint = startPoint;
                    }
                    else
                    {
                        if (pathCheck)
                        {
                            nextPointToPlayer = Pathfinder.FindPath(climbAI.currentPoint, endPoint);
                            if (nextPointToPlayer == null)
                                nextPointToPlayer = Pathfinder.FindPath(climbAI.currentPoint, startPoint);
                            else if (climbAI.currentPoint.transform.parent == nextPointToPlayer.Last().transform.parent)
                            {
                                climbAI.currentPoint.hasOwner = false;
                                if (climbAI.currentPoint.position.y < linkEndPoint.y + 0.2f)
                                    climbAI.MountPoint();
                                else
                                    ClimbFalling(linkEndPoint);
                                yield break;
                            }
                        }
                        else
                        {
                            nextPointToPlayer = Pathfinder.FindPath(climbAI.currentPoint, startPoint);
                            if (nextPointToPlayer == null)
                                nextPointToPlayer = Pathfinder.FindPath(climbAI.currentPoint, endPoint);
                            else if (climbAI.currentPoint.transform.parent == nextPointToPlayer.Last().transform.parent)
                            {
                                climbAI.currentPoint.hasOwner = false;
                                if (climbAI.currentPoint.position.y < linkStartPoint.y + 0.2f)
                                    climbAI.MountPoint();
                                else
                                    ClimbFalling(linkStartPoint);
                                yield break;
                            }
                        }
                        if (nextPointToPlayer != null)
                            nextPoint = nextPointToPlayer[0];
                    }
                }
                else
                {
                    nextPoint = nextPointToPlayer[0];
                    yield return new WaitForSeconds(0.4f);
                }

                if (nextPoint != null && !nextPoint.hasOwner && (!playerParkour.IsHanging || (playerParkour.IsHanging && nextPoint != playerClimbController.currentPoint))
                    && climbAI.currentPoint != nextPoint)
                {
                    if (nextPoint.transform.parent == endPoint.transform.parent && nextPoint.transform.parent.CompareTag("SwingableLedge"))
                    {
                        DoClimbAction(climbAI.currentPoint, nextPoint, linkEndPoint);
                        yield break;
                    }
                    else
                        DoClimbAction(climbAI.currentPoint, nextPoint, null);

                    yield return new WaitUntil(() => parkourAI.InAction == false);
                }
                yield return new WaitForFixedUpdate();
                yield return new WaitForSeconds(0.2f);
            }
        }
        IEnumerator HandleClimbingToTarget(CustomNavmeshLink link, Vector3 linkStartPoint, Vector3 linkEndPoint, ClimbPoint startPoint, ClimbPoint endPoint, Vector3 targetPosition)
        {
            while (true)
            {
                ClimbPoint nextPoint = null;
                ClimbPoint[] nextPointToTarger = null;

                var pathCheck = CheckPath(link, linkStartPoint, linkEndPoint, targetPosition);
                if (!parkourAI.IsHanging)
                {
                    if (!pathCheck)
                    {
                        parkourAI.InAction = false;
                        OnEndClimbAction();
                        yield break;
                    }
                    else
                        nextPoint = startPoint;
                }
                else
                {
                    if (pathCheck)
                    {
                        nextPointToTarger = Pathfinder.FindPath(climbAI.currentPoint, endPoint);
                        if (nextPointToTarger == null)
                            nextPointToTarger = Pathfinder.FindPath(climbAI.currentPoint, startPoint);
                        else if (climbAI.currentPoint.transform.parent == nextPointToTarger.Last().transform.parent)
                        {
                            climbAI.currentPoint.hasOwner = false;
                            if (climbAI.currentPoint.position.y < linkEndPoint.y + 0.2f)
                                climbAI.MountPoint();
                            else
                                ClimbFalling(linkEndPoint);
                            yield break;
                        }
                    }
                    else
                    {
                        nextPointToTarger = Pathfinder.FindPath(climbAI.currentPoint, startPoint);
                        if (nextPointToTarger == null)
                            nextPointToTarger = Pathfinder.FindPath(climbAI.currentPoint, endPoint);
                        else if (climbAI.currentPoint.transform.parent == nextPointToTarger.Last().transform.parent)
                        {
                            climbAI.currentPoint.hasOwner = false;
                            if (climbAI.currentPoint.position.y < linkStartPoint.y + 0.2f)
                                climbAI.MountPoint();
                            else
                                ClimbFalling(linkStartPoint);
                            yield break;
                        }
                    }
                    if (nextPointToTarger != null)
                        nextPoint = nextPointToTarger[0];
                }

                if (nextPoint != null && !nextPoint.hasOwner && climbAI.currentPoint != nextPoint)
                {
                    DoClimbAction(climbAI.currentPoint, nextPoint);
                    yield return new WaitUntil(() => parkourAI.InAction == false);
                }
                yield return new WaitForFixedUpdate();
                yield return new WaitForSeconds(0.2f);
            }
        }

        public bool CheckPathWithAgent(CustomNavmeshLink link, Vector3 start, Vector3 end, Vector3 dest)
        {
            var path = new NavMeshPath();
            var closestPoint = NavMesh.SamplePosition(dest + Vector3.up * 2f, out NavMeshHit hit, 10f, NavMesh.AllAreas);
            if (!closestPoint) hit.position = dest;
            navMeshAgent.CalculatePath(hit.position, path);

            foreach (var item in path.corners)
            {
                if ((item - end).sqrMagnitude < 0.25f)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckPath(CustomNavmeshLink link, Vector3 start, Vector3 end, Vector3 dest)
        {
            var path = new NavMeshPath();
            var closestPoint = NavMesh.SamplePosition(dest + Vector3.up * 2f, out NavMeshHit hit, 10f, NavMesh.AllAreas);
            if (!closestPoint) hit.position = dest;
            NavMesh.CalculatePath(start, hit.position, NavMesh.AllAreas, path);

            foreach (var item in path.corners)
            {
                if ((item - end).magnitude < 0.5f)
                {
                    return true;
                }
            }
            return false;
        }


        void DoClimbAction(ClimbPoint currentPoint, ClimbPoint newPoint, Vector3? nextJumpPoint = null)
        {
            if (!parkourAI.IsHanging || currentPoint == null)
            {
                navMeshAgent.Warp(navMeshAgent.transform.position);
                if (transform.position.y + 0.1f > newPoint.position.y)
                    climbAI.DropToPoint(newPoint);
                else
                    climbAI.ClimbToPoint(newPoint, nextJumpPoint: nextJumpPoint);
                return;
            }
            var distance = Vector3.Distance(currentPoint.position, newPoint.position);
            var angleDiff = Vector3.Angle(currentPoint.transform.forward, newPoint.transform.forward);

            if (angleDiff < 30f && distance < 0.9f)//|| (neighbour != null && neighbour.connectionType == ConnectionType.Move))
            {
                climbAI.DoShimmyAction(newPoint);
            }
            else
            {
                climbAI.DoClimbJumpAction(newPoint, angleDiff);
            }
        }
        void ClimbFalling(Vector3 landPoint)
        {
            parkourAI.InAction = true;
            isFalling = true;

            var diff = landPoint - transform.position;
            diff.y = 0;

            if (diff.magnitude > 1.2f)
            {
                JumpData jumpData = new();
                jumpData.hasSpaceToLand = true;
                jumpData.footPosition = jumpData.rootPosition = landPoint;

                StartCoroutine(parkourAI.DoPredictiveBackJump(jumpData));
                climbAI.currentPoint = null;
                return;
            }

            StartCoroutine(HandleFalling(animator.GetFloat("freeHang") < 0.5f ? "JumpFromHang" : "JumpFromFreeHang", new Vector3(transform.position.x, landPoint.y, transform.position.z), onComplete: () =>
            {
                parkourAI.IsHanging = false;
                isFalling = true;
                climbAI.currentPoint = null;
            }));
        }


        IEnumerator HandleFalling(string animName, Vector3 landPos, Action onComplete = null)
        {
            if (NavMesh.SamplePosition(landPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                landPos = hit.position;
            var matchParams = new TargetMatchParams()
            {
                pos = landPos,
                startTime = 0.4f,
                endTime = 0.7f,
                target = AvatarTarget.Root,
                posWeight = new Vector3(0, 0, 1)
            };
            var dir = landPos - transform.position;
            dir.y = 0;
            Quaternion targetRot;
            if (dir != Vector3.zero)
                targetRot = Quaternion.LookRotation(dir);
            else
                targetRot = Quaternion.LookRotation(transform.forward);

            character.OnStartAction();
            parkourAI.InAction = true;
            isFalling = true;
            if (navMeshAgent.isOnOffMeshLink)
                yield return LerpToPos(navMeshAgent.currentOffMeshLinkData.startPos);

            animator.SetBool("IsGrounded", false);
            yield return parkourAI.DoAction(animName, rotate: true, targetRot: targetRot, matchParams, onComplete: () =>
            {
                parkourAI.InAction = true;
                onComplete?.Invoke();
            });

            ySpeed = character.Gravity / 4;
            while (transform.position.y > landPos.y)
            {
                ySpeed += character.Gravity * Time.deltaTime;
                transform.position += Vector3.up * ySpeed * Time.deltaTime;
                if ((transform.position.y - landPos.y) < 0.2f)
                {
                    if (ySpeed < character.Gravity / 2)
                        animator.SetFloat("fallAmount", Mathf.Clamp(Mathf.Abs(ySpeed) * 0.06f, 0.6f, 1f));
                    else
                        animator.SetFloat("fallAmount", 0);
                    animator.SetBool("IsGrounded", true);
                }
                yield return null;
            }
            if (ySpeed < character.Gravity / 2)
                animator.SetFloat("fallAmount", Mathf.Clamp(Mathf.Abs(ySpeed) * 0.06f, 0.6f, 1f));
            else
                animator.SetFloat("fallAmount", 0);
            animator.SetBool("IsGrounded", true);
            transform.position = new Vector3(transform.position.x, landPos.y, transform.position.z);
            parkourAI.InAction = false;
            isFalling = false;
            character.OnEndAction();
        }

        public IEnumerator TweenVal(float start, float end, float duration, Action<float> onLerp, Action onComplete = null)
        {
            float timer = 0f;
            float percent = timer / duration;

            while (percent <= 1f)
            {
                timer += Time.deltaTime;
                percent = timer / duration;
                var lerpVal = Mathf.Lerp(start, end, percent);
                onLerp?.Invoke(lerpVal);

                yield return null;
            }
        }

        int currentWayPoint = -1;
        Transform wayPoint;
        Transform currentDest;

        public void HandleWayPoints(Transform wayPoints)
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                var count = wayPoints.childCount;
                currentWayPoint = ++currentWayPoint % count;
                if (count == 0)
                    wayPoint = wayPoints;
                else
                    wayPoint = wayPoints.GetChild(currentWayPoint);

                //Debug.Log(wayPoint.gameObject.name);
                navMeshAgent.SetDestination(wayPoint.position);
            }
        }
        public void HandleRandomPoints()
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                var point = RandomNavSphere(transform.position, 30f, NavMesh.AllAreas);
                navMeshAgent.SetDestination(point);
            }
        }
        public Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
        {
            Vector3 randDirection = Random.insideUnitSphere * dist;

            randDirection += origin;
            NavMeshHit navHit;
            NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

            return navHit.position;
        }

        public void HandleRunFromPlayer()
        {
            var dir = (transform.position - player.transform.position);
            if (dir.magnitude > 10f) return;

            dir.Normalize();
            var right = Vector3.Cross(dir, Vector3.up);
            var forward = player.transform.position + dir * 8f;
            NavMeshHit navHit;
            NavMesh.SamplePosition(forward, out navHit, 10f, NavMesh.AllAreas);
            if ((navHit.position - player.transform.position).magnitude < 6f)
                NavMesh.SamplePosition(forward + right * 3f, out navHit, 10f, NavMesh.AllAreas);

            if ((navHit.position - player.transform.position).magnitude < 6f)
                NavMesh.SamplePosition(forward - right * 3f, out navHit, 10f, NavMesh.AllAreas);

            navMeshAgent.SetDestination(navHit.position);

        }

        public void OnStartClimbAction()
        {
            //navMeshAgent.updatePosition = false;
            //navMeshAgent.updateRotation = false;
            //navMeshAgent.isStopped = true;
            navMeshAgent.ActivateCurrentOffMeshLink(true);
        }

        public void OnEndClimbAction()
        {
            navMeshAgent.Warp(navMeshAgent.transform.position);
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = true;
            navMeshAgent.isStopped = false;
            isFalling = false;
            animator.SetBool("IsGrounded", true);
        }
    }
}
