using FS_SwingSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_ThirdPerson
{
    public partial class EnvironmentScanner : MonoBehaviour
    {
        [field: SerializeField] public LayerMask SwingLedgeLayer { get; set; } = 1;

        Vector3 s_pathCheckExtend = new Vector3(0.05f, .1f, 0.01f);

        float s_capsuleRadius = 1.5f;
        float s_minimumHookAngle = 5;

        public SwingData GetSwingLedgeData(float maxDistance, float minDistance, Transform ropeHoldPoint, bool debug)
        {
            List<Vector3> detectedHookPoints = new List<Vector3>();
            Vector3 direction = Camera.main.transform.forward + transform.up * .25f;
            var checkOrigin = transform.position + transform.up * .75f + direction.normalized * s_capsuleRadius * 2;
            bool hasLedge = false;
            float radius = s_capsuleRadius;
            float coveredDistance = 0f;
            float totalCastLength = 0f;
            float singleCastDist = 5f;


            Vector3 hookPos = Vector3.zero;
            Vector3 dir = Vector3.zero;
            float distancToHookPos = 0;
            var disToOrigin = Vector3.Distance(checkOrigin, ropeHoldPoint.position);

            for (float i = 0; totalCastLength < maxDistance - disToOrigin; i++)
            {
                var castLength = (i + 1) * singleCastDist + radius * 2;


                var hits = Physics.SphereCastAll(
                   checkOrigin + direction.normalized * coveredDistance,
                   radius,
                   direction,
                   castLength,
                   SwingLedgeLayer);

                //GizmosExtend.drawSphereCast(
                //   checkOrigin + direction.normalized * coveredDistance,
                //   radius,
                //   direction,
                //   castLength,
                //   Color.red);

                hasLedge = hits.Length > 0;

                if (hasLedge)
                {
                    foreach (var hit in hits)
                    {
                        var swingLedge = hit.transform.gameObject.GetComponent<SwingLedge>();
                        if (swingLedge != null)
                        {
                            hookPos = swingLedge.transform.TransformPoint(swingLedge.hookPoint);
                            var o = transform.position + transform.up * .6f;
                            var distance = (o - hookPos).magnitude;
                            var hasPath = !Physics.BoxCast(
                                    o,
                                    s_pathCheckExtend,
                                    (hookPos - o).normalized,
                                    out RaycastHit pathHit,
                                    Quaternion.LookRotation(hookPos),
                                    distance - .5f,
                                    ObstacleLayer
                                );
                            distancToHookPos = (ropeHoldPoint.position - hookPos).magnitude;
                            hasLedge = hasPath && distancToHookPos > minDistance && distancToHookPos <= maxDistance;

                            if (hasLedge)
                                detectedHookPoints.Add(hookPos);

                            if (debug)
                            {
                                BoxCastDebug.DrawBoxCastBox(
                                       o,
                                       s_pathCheckExtend,
                                       Quaternion.LookRotation(hookPos),
                                       (hookPos - o).normalized,
                                       distance - .5f,
                                       Color.green
                                   );
                            } 
                        }
                    }
                }
                
                coveredDistance += castLength - radius;
                totalCastLength += castLength + 2 * radius;

                radius += s_capsuleRadius * 1.5f;
            }

            if (detectedHookPoints.Count > 0)
            {
                hookPos = s_GetAccurateHookPosition(checkOrigin, direction, detectedHookPoints);
                var hp = hookPos;
                distancToHookPos = (ropeHoldPoint.position - hookPos).magnitude;
                hp.y = transform.position.y;
                dir = (hp - transform.position).normalized;
            }


            SwingData data = new SwingData()
            {
                hasLedge = detectedHookPoints.Count > 0,
                hookPosition = hookPos,
                forwardDirection = dir,
                directionToHook = (hookPos - transform.position).normalized,
                distance = distancToHookPos
            };


            return data;
        }


        Vector3 s_GetAccurateHookPosition(Vector3 origin, Vector3 direction, List<Vector3> hookPositions)
        {
            Vector3 hookPos = hookPositions.First();
            float previousAngle = 400;
            Vector3 bestPos = hookPos;
            foreach (var hp in hookPositions)
            {
                var dir = (hp - origin).normalized;
                var angle = Vector3.Angle(direction.normalized, dir);
                if (angle < s_minimumHookAngle)
                    return hp;
                else if (angle < previousAngle)
                    bestPos = hp;
                previousAngle = angle;
            }

            return bestPos;
        }
    }

    public class SwingData
    {
        public Vector3 hookPosition;
        public bool hasLedge;
        public Vector3 directionToHook;
        public Vector3 forwardDirection;
        public float distance;
    }
}