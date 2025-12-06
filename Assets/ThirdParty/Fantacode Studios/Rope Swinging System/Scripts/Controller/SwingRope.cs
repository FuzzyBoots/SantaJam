using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace FS_SwingSystem
{
    public enum RopeState { Normal, Swinging, Throwing, Climbing}

    public class SwingRope
    {
        public float ropeRadius = 0.025f;
        public Material ropeMaterial;
        public int hookRopeResolution = 50;
        public float totalRopeLength = 5f;
        public float ropeWidth = 0.1f;
        public float dampening = 0.8f;
        public float smoothingFactor = 0.1f;
        public float throwHeight = 1f;
        public float throwDuration = 2f;

        public Transform ropeHoldTransformLeft;
        public Transform ropeHoldTransformRight;
        public Transform ropeAttachPointToHook;
        public Transform ropeAttachTransformInBody;
        public Transform hookObject;

        public float hookRopeLength = 5f;

        private int holdRopeResolution = 50;
        private float holdRopelength = .1f;

        public RopeState ropeState = RopeState.Normal;

        private Vector3[] hookRopePositions;
        private Vector3[] hookRopePreviousPositions;
        private Vector3[] hookRopeVelocities;

        private Vector3[] holdRopePositions;
        private Vector3[] holdRopePreviousPositions;
        private Vector3[] holdRopeVelocities;

        private LineRenderer hookRope;
        private LineRenderer holdRope;

        private Vector3 hookPoint;
        private float initialRopeLength;
        private float targetRopeLength;
        private int constraintIterations = 50;
        private float colliderRadius = 0.05f;
        private Transform playerTransform;



        public Transform currentRopeHoldPointTransform;
        Vector3 defaultHookPos = Vector3.zero;
        Quaternion defaultHookRot = Quaternion.identity;
        Transform defaultHookObjectParent;
        bool hooked;

        public SwingRope(float ropeRadius, float ropeWidth, Material ropeMaterial, float throwDuration, float ropeLength, float dampening, int ropeResolution, float throwHeight, Transform transform, Transform ropeAttachTransformInBody, Transform ropeHoldTransformLeft, Transform ropeHoldTransformRight, Transform ropeAttachPointToHook, Transform hookObject)
        {
            this.totalRopeLength = ropeLength;
            this.ropeWidth = ropeWidth;
            this.dampening = dampening;
            this.ropeRadius = ropeRadius;
            this.ropeMaterial = ropeMaterial;
            this.throwDuration = throwDuration;
            this.hookRopeResolution = ropeResolution;
            this.throwHeight = throwHeight;
            this.playerTransform = transform;
            this.ropeAttachTransformInBody = ropeAttachTransformInBody;
            this.ropeHoldTransformRight = ropeHoldTransformRight;
            this.ropeHoldTransformLeft = ropeHoldTransformLeft;
            this.ropeAttachPointToHook = ropeAttachPointToHook;
            this.hookObject = hookObject;
            currentRopeHoldPointTransform = ropeHoldTransformRight;
            if (hookObject != null)
            {
                defaultHookObjectParent = hookObject.transform.parent;
                defaultHookPos = hookObject.transform.localPosition;
                defaultHookRot = hookObject.transform.localRotation;
            }
            InitLineRenderer(transform);
        }

        public void InitLineRenderer(Transform transform)
        {
            var lineRendererObject = new GameObject("Rope");
            lineRendererObject.transform.parent = transform.parent;
            hookRope = lineRendererObject.AddComponent<LineRenderer>();
            hookRope.material = ropeMaterial;
            hookRope.numCapVertices = 1;
            hookRope.startWidth = ropeRadius;
            hookRope.endWidth = ropeRadius;
            hookRope.positionCount = 0;

            var physicsLineObject = new GameObject("PhysicsRope");
            physicsLineObject.transform.parent = transform.parent;
            holdRope = physicsLineObject.AddComponent<LineRenderer>();
            holdRope.material = ropeMaterial;
            holdRope.numCapVertices = 1;
            holdRope.startWidth = ropeRadius;
            holdRope.endWidth = ropeRadius;
            holdRope.positionCount = 0;

            InitializeHoldRope();
        }

        public void DeleteRope()
        {
            GameObject.Destroy(holdRope.gameObject);
            GameObject.Destroy(hookRope.gameObject);
        }

        public void RopeUpdate()
        {
            if (ropeState != RopeState.Climbing)
                currentRopeHoldPointTransform = ropeState == RopeState.Swinging ? ropeHoldTransformLeft : ropeHoldTransformRight;


            if (hookRope.positionCount > 0)
            {
                hookRopeLength = Vector3.Distance(currentRopeHoldPointTransform.position, hookPoint);
                if (ropeState == RopeState.Swinging || ropeState == RopeState.Climbing)
                {
                    hookRopePositions[0] = currentRopeHoldPointTransform.position;
                    hookRopePositions[1] = ropeAttachPointToHook.position;
                }
                else
                {
                    SimulateHookRopePhysics();
                }

                UpdateHookRopeLineRenderer();
            }

            if (holdRope.positionCount > 0)
            {
                SimulateHoldRopePhysics();
                UpdateHoldRopeLineRenderer();
            }
        }

        void UpdateHookRopeLineRenderer()
        {
            hookRope.SetPositions(hookRopePositions);
        }
        public void SetHookRopeAsStraight()
        {
            hookRope.positionCount = 2;
            hookRopePositions = new Vector3[2];
        }
        void SimulateHookRopePhysics()
        {
            float segmentLength = hookRopeLength / (hookRopeResolution - 1);
            System.Array.Copy(hookRopePositions, hookRopePreviousPositions, hookRopeResolution);

            for (int i = 1; i < hookRopeResolution - 1; i++)
            {
                Vector3 velocity = hookRopeVelocities[i];
                velocity += Physics.gravity * Time.unscaledDeltaTime;

                Collider[] hitColliders = Physics.OverlapSphere(hookRopePositions[i], colliderRadius);
                bool collision = false;
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.gameObject != playerTransform.gameObject)
                    {
                        if (hitCollider is MeshCollider)
                        {
                            var c = hitCollider as MeshCollider;
                            if (!c.convex)
                                continue;
                        }
                        Vector3 collisionPoint = hitCollider.ClosestPoint(hookRopePositions[i]);
                        Vector3 normal = (hookRopePositions[i] - collisionPoint).normalized;

                        velocity = Vector3.Reflect(velocity, normal) * (1f - dampening);
                        hookRopePositions[i] = collisionPoint + normal * colliderRadius;
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                {
                    hookRopePositions[i] += velocity * Time.unscaledDeltaTime;
                }

                velocity *= 1f - dampening;
                hookRopeVelocities[i] = velocity;
            }

            for (int iteration = 0; iteration < constraintIterations; iteration++)
            {
                for (int i = 0; i < hookRopeResolution - 1; i++)
                {
                    Vector3 delta = hookRopePositions[i + 1] - hookRopePositions[i];
                    float currentDistance = delta.magnitude;
                    float correction = (currentDistance - segmentLength) / currentDistance;
                    Vector3 correctionVector = delta * correction;

                    if (i > 0)
                    {
                        hookRopePositions[i] += correctionVector;
                        hookRopePositions[i + 1] -= correctionVector;
                    }
                    else
                    {
                        hookRopePositions[i + 1] -= correctionVector * 2;
                    }
                }

                hookRopePositions[0] = currentRopeHoldPointTransform.position;
                hookRopePositions[hookRopeResolution - 1] = ropeAttachPointToHook.position;
            }

            for (int i = 1; i < hookRopeResolution - 1; i++)
            {
                hookRopePositions[i] = Vector3.Lerp(hookRopePositions[i], (hookRopePositions[i - 1] + hookRopePositions[i + 1]) * 0.5f, smoothingFactor);
            }

            for (int i = 1; i < hookRopeResolution - 1; i++)
            {
                hookRopeVelocities[i] = (hookRopePositions[i] - hookRopePreviousPositions[i]) / Time.unscaledDeltaTime;
            }
        }



        void UpdateHoldRopeLineRenderer()
        {
            holdRope.SetPositions(holdRopePositions);
        }
        void SimulateHoldRopePhysics()
        {
            holdRopelength = Vector3.Distance(currentRopeHoldPointTransform.position, ropeAttachTransformInBody.position) + .1f;
            float segmentLength = holdRopelength / (holdRopeResolution - 1);
            System.Array.Copy(holdRopePositions, holdRopePreviousPositions, holdRopeResolution);

            for (int i = 1; i < holdRopeResolution - 1; i++)
            {
                Vector3 velocity = holdRopeVelocities[i];
                velocity += Physics.gravity * Time.unscaledDeltaTime;

                holdRopePositions[i] += velocity * Time.unscaledDeltaTime;

                velocity *= 1f - dampening;
                holdRopeVelocities[i] = velocity;
            }

            for (int iteration = 0; iteration < constraintIterations; iteration++)
            {
                for (int i = 0; i < holdRopeResolution - 1; i++)
                {
                    Vector3 delta = holdRopePositions[i + 1] - holdRopePositions[i];
                    float currentDistance = delta.magnitude;
                    float correction = (currentDistance - segmentLength) / currentDistance;
                    Vector3 correctionVector = delta * correction * .9f;

                    if (i > 0)
                    {
                        holdRopePositions[i] += correctionVector;
                        holdRopePositions[i + 1] -= correctionVector;
                    }
                    else
                    {
                        holdRopePositions[i + 1] -= correctionVector * 2;
                    }
                }

                holdRopePositions[0] = ropeAttachTransformInBody.position;
                holdRopePositions[holdRopeResolution - 1] = hooked ? currentRopeHoldPointTransform.position : ropeAttachPointToHook.position;
            }

            for (int i = 1; i < holdRopeResolution - 1; i++)
            {
                holdRopePositions[i] = Vector3.Lerp(holdRopePositions[i], (holdRopePositions[i - 1] + holdRopePositions[i + 1]) * 0.5f, smoothingFactor);
            }

            for (int i = 1; i < holdRopeResolution - 1; i++)
            {
                holdRopeVelocities[i] = (holdRopePositions[i] - holdRopePreviousPositions[i]) / Time.unscaledDeltaTime;
            }
        }
        private void InitializeHoldRope()
        {
            holdRopePositions = new Vector3[holdRopeResolution];
            holdRopePreviousPositions = new Vector3[holdRopeResolution];
            holdRopeVelocities = new Vector3[holdRopeResolution];

            for (int i = 0; i < holdRopeResolution; i++)
            {
                float t = (float)i / (holdRopeResolution - 1);
                holdRopePositions[i] = Vector3.Lerp(ropeAttachTransformInBody.position, currentRopeHoldPointTransform.position, t);
                holdRopePreviousPositions[i] = holdRopePositions[i];
                holdRopeVelocities[i] = Vector3.zero;
            }

            holdRope.positionCount = holdRopeResolution;
        }



        public void StartThrow(Transform startPoint, Vector3 endPoint)
        {
            currentRopeHoldPointTransform = startPoint;
            hookRopeLength = Vector3.Distance(startPoint.position, endPoint);
            hookPoint = endPoint;
            InitializeRope();
            initialRopeLength = 0;
            targetRopeLength = hookRopeLength;
            ThrowRopeCoroutine();
        }

        private void InitializeRope()
        {
            hookRopePositions = new Vector3[hookRopeResolution];
            hookRopePreviousPositions = new Vector3[hookRopeResolution];
            hookRopeVelocities = new Vector3[hookRopeResolution];

            for (int i = 0; i < hookRopeResolution; i++)
            {
                float t = (float)i / (hookRopeResolution - 1);
                hookRopePositions[i] = Vector3.Lerp(currentRopeHoldPointTransform.position, hookPoint, t);
                hookRopePreviousPositions[i] = hookRopePositions[i];
                hookRopeVelocities[i] = Vector3.zero;
            }
            hookRope.positionCount = hookRopeResolution;
        }

        private async void ThrowRopeCoroutine()
        {
            hooked = true;
            float timeElapsed = 0f;
            SetRopeState(RopeState.Throwing);
            Vector3 startPos = currentRopeHoldPointTransform.position;
            var hookPos = hookPoint;
            if (hookObject != null)
            {
                hookObject.parent = null;
            }
            while (timeElapsed < throwDuration && !HasReachedTarget())
            {
                timeElapsed += Time.unscaledDeltaTime;
                float t = timeElapsed / throwDuration;
                Vector3 currentPos = CalculateParabolicPath(startPos, hookPos, throwHeight, t);
                hookPoint = currentPos;
                if (hookObject != null)
                    hookObject.position = hookPoint;

                AdjustRopeLength(t);
                await Task.Yield();
            }
            if(ropeState == RopeState.Throwing)
                SetRopeState(RopeState.Normal);
            hookPoint = hookPos;
        }

        Vector3 CalculateParabolicPath(Vector3 start, Vector3 end, float height, float t)
        {
            t = Mathf.Clamp01(t);

            Vector3 linearPos = Vector3.Lerp(start, end, t);

            float arc = height * 4 * (t - t * t);

            return new Vector3(linearPos.x, linearPos.y + arc, linearPos.z);
        }

        private void AdjustRopeLength(float t)
        {
            hookRopeLength = Mathf.Lerp(initialRopeLength, targetRopeLength, t);
        }




        public void ResetRope()
        {
            hooked = false;
            if (hookObject != null)
            {
                hookObject.parent = defaultHookObjectParent;
                hookObject.localPosition = defaultHookPos;
                hookObject.localRotation = defaultHookRot;
            }
            hookRope.positionCount = 0;
            hookRopePositions = new Vector3[0];

            hookRopePositions = new Vector3[0];
            hookRopePreviousPositions = new Vector3[0];
            hookRopeVelocities = new Vector3[0];
            SetRopeState(RopeState.Normal);
        }

        public bool HasReachedTarget()
        {
            return ropeState != RopeState.Throwing;
        }

        public void SetRopeState(RopeState newState)
        {
            ropeState = newState;
        }
    }

}