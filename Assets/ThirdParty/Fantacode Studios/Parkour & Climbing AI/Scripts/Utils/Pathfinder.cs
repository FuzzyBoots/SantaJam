using FS_ParkourSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_ParkourSystem
{
    public partial class ClimbPoint : MonoBehaviour
    {
        public Vector3 position { get { return transform.position; } }

        [HideInInspector]
        public ClimbPoint parent = null;
        [HideInInspector]
        public float gCost = float.PositiveInfinity;

        [HideInInspector]
        public float hCost = 0;
        public float fCost { get { return gCost + hCost; } }

        public bool CheckSpaceForClimb()
        {
            var point = this;
            var halfExtends = new Vector3(.3f, .5f, .25f);
            var down = Vector3.down * .65f;

            var hasSpaceForClimb = !Physics.CheckBox(point.transform.position + point.transform.forward * .6f + down, halfExtends, Quaternion.LookRotation(Vector3.right));
            
            return hasSpaceForClimb;
        }
        public bool isValid(ClimbPoint newPoint)
        {
            var angleDiff = Vector3.Angle(transform.forward, newPoint.transform.forward);
            //var distance = Vector3.Distance(position, newPoint.transform.position);
            var distance = (position - newPoint.transform.position).sqrMagnitude;
            if (distance < 4f) // 2 square
            {
                if ((angleDiff > 40f && distance > 1f) || angleDiff > 130)
                    return false;
                return true;
            }
            return false;
        }
    }
}
namespace FS_ParkourAI
{
    public class Pathfinder
    {

        static LayerMask ledgeMask = LayerMask.GetMask("Ledge");

        public static ClimbPoint[] FindPath(ClimbPoint startPoint, ClimbPoint targetPoint, bool partialPath = false, bool checkOwner = false)
        {
            if (startPoint == targetPoint) return new[] { startPoint };

            List<ClimbPoint> openList = new List<ClimbPoint>();
            HashSet<ClimbPoint> closedList = new HashSet<ClimbPoint>();

            ClimbPoint startNode = startPoint;
            ClimbPoint targetNode = targetPoint;

            openList.Add(startNode);

            startNode.gCost = 0;

            ClimbPoint currentNode = openList[0];

            while (openList.Count > 0)
            {
                currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].fCost < currentNode.fCost)// || (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                    {
                        currentNode = openList[i];
                    }
                }

                //if (partialPath && Vector3.Distance(currentNode.position, targetNode.position) <= checkDistance)
                //{
                //    return RetracePath(startNode, currentNode);
                //}
                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, currentNode);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                var neighbours = GetNeighbours(currentNode);
                foreach (ClimbPoint neighbour in neighbours)
                {
                    //if (checkOwner && neighbour.hasOwner) continue;
                    if (!neighbour.CheckSpaceForClimb()) continue;

                    if (closedList.Contains(neighbour))//|| Vector3.Distance(neighbour, currentNode.position) > maxJumpDistance)
                    {
                        continue;
                    }
                    ClimbPoint neighbourNode = neighbour;
                    float newMovementCostToNeighbour = currentNode.gCost + (currentNode.position - neighbour.position).sqrMagnitude;
                    //float newMovementCostToNeighbour = currentNode.gCost + Vector3.Distance(currentNode.position, neighbour.position);
                    //var hCost = Vector3.Distance(neighbourNode.position, targetNode.position);

                    if (newMovementCostToNeighbour < neighbourNode.gCost || !openList.Contains(neighbour))
                    {
                        neighbourNode.gCost = newMovementCostToNeighbour * (neighbour.hasOwner ? 2 : 1);
                        neighbourNode.hCost = (neighbourNode.position - targetNode.position).sqrMagnitude;
                        neighbourNode.parent = currentNode;

                        if (!openList.Contains(neighbourNode))
                        {
                            openList.Add(neighbourNode);
                        }
                    }
                }
            }

            if (partialPath)
                return RetracePath(startNode, currentNode);

            return null;
        }

        public ClimbPoint nextPoint(ClimbPoint startPoint, ClimbPoint targetPoint, bool partialPath = false, float checkDistance = 0)
        {
            var point = FindPath(startPoint, targetPoint, partialPath);

            return point == null ? null : point[0];
        }

        public static ClimbPoint[] RetracePath(ClimbPoint startNode, ClimbPoint endNode)
        {
            List<ClimbPoint> path = new List<ClimbPoint>();
            ClimbPoint currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            return path.ToArray();
        }

        static int bufferSize = 30;
        public static Dictionary<ClimbPoint, ClimbPoint[]> neighbourList = new Dictionary<ClimbPoint, ClimbPoint[]>();
        static Queue<ClimbPoint> climbpointQueue = new Queue<ClimbPoint>();
        public static ClimbPoint[] GetNeighbours(ClimbPoint climbPoint)
        {
            if (neighbourList.ContainsKey(climbPoint) && neighbourList[climbPoint] != null)
            {
                return neighbourList[climbPoint];
            }
            var ledges = Physics.OverlapSphere(climbPoint.position, 2f, ledgeMask);

            List<ClimbPoint> points = new List<ClimbPoint>();
            foreach (var hitCollider in ledges)
            {
                points.AddRange(hitCollider.gameObject.GetComponentsInChildren<ClimbPoint>());
            }
            var pointA = points.Where(x => climbPoint.isValid(x)).ToArray();

            neighbourList.Add(climbPoint, pointA);
            climbpointQueue.Enqueue(climbPoint);
            if (climbpointQueue.Count > bufferSize)
            {
                var dequeue = climbpointQueue.Dequeue();
                neighbourList.Remove(dequeue);
            }

            return pointA;
        }

        public void StartPathfinding()
        {
            //List<PathNode> path = FindPath(player.position, target.position);

        }
    }
}