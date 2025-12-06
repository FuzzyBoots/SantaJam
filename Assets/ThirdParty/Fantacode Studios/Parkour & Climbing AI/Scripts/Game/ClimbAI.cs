using FS_ParkourSystem;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ParkourAI
{
    public class ClimbAI : MonoBehaviour
    {
        [HideInInspector]
        [field: Tooltip("Enables Climbing")]
        public bool enableClimbing = true;

        [Tooltip("Offset between the hand and the climbpoint while bracedhanging. Adjust this value and make sure that the hand is correctly placed on the climbing ledge .")]
        public Vector3 handOffsets = new Vector3(0f, -0.08f, 0.05f);

        [Tooltip("Offset between the foot bone and position on the wall where the foot is place. Adjust this value and make sure that the foot is correctly placed on the wall.")]
        [SerializeField] float footPlacementOffset = 0.15f;

        [Tooltip("Length of the raycast used for finding the foot ik position on the wall during braced climb")]
        [SerializeField] float footIkRayLength = 0.5f;

        //bool turnOnGizmos = false;

        [SerializeField] float hipRayLength = 0.3f;

        Vector3 wallRayOffset = new Vector3(-0.15f, -0.9f, 0.14f);
        Vector3 obstacleRayOffset = new Vector3(0, -0.3f, 0.2f);

        [HideInInspector]
        private ClimbPoint _currentPoint;
        [HideInInspector]
        public ClimbPoint currentPoint
        {
            get { return _currentPoint; }
            set
            {
                if (_currentPoint != null)
                    _currentPoint.hasOwner = false;
                if (value != null)
                    value.hasOwner = true;

                _currentPoint = value;
            }
        }

        [HideInInspector]
        public ClimbPoint previousPoint;

        Transform currentLedge;

        bool isFalling = false;
        bool ikEnabled = false;

        IAICharacter player;
        ParkourAI parkourAI;
        Animator animator;
        public LayerMask LedgeLayer;

        Vector3 lookAtPosition;
        float lookAtWeight;


        private void OnEnable()
        {
            player = GetComponent<IAICharacter>();
            parkourAI = GetComponent<ParkourAI>();
            animator = player.Animator;
            LedgeLayer = LayerMask.GetMask("Ledge");
        }

        [HideInInspector]
        public IKweights rightHand, leftHand, rightFoot, leftFoot, head;

        [Serializable]
        public class IKweights
        {
            public float current;
            public float target;
            public float weight;
            public ClimbPoint currentPoint;
            public ClimbPoint previousPoint;

            public Vector3 currentIkVecPoint;
            public Vector3 previousIkVecPoint;
            public Vector3 currentVecPoint;
            public Vector3 previousVecPoint;

            public bool currentVecPointOn;
            public bool previousVecPointOn;

            public Vector3 boneTransform;
            public Vector3 bodyPartOffset;

            public Vector3 startBodyPartOffset;
            public Vector3 endBodyPartOffset;
            public Vector3 IKPoint;
            public float lerpValue;
        }

        public void OnAnimatorIK(int layerIndex)
        {
            rightHand.boneTransform = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            leftHand.boneTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            rightFoot.boneTransform = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            leftFoot.boneTransform = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

            if (animator && parkourAI.IsHanging)
            {
                if (currentPoint)
                {
                    if (lookAtPosition == Vector3.zero) lookAtPosition = currentPoint.transform.position;

                    lookAtPosition = Vector3.MoveTowards(lookAtPosition, currentPoint.transform.position, 2f * Time.deltaTime);
                    animator.SetLookAtPosition(lookAtPosition);
                    lookAtWeight = Mathf.Clamp01(lookAtWeight + 2f * Time.deltaTime) * 0.8f;
                    animator.SetLookAtWeight(lookAtWeight);
                }
                else
                {
                    lookAtWeight = Mathf.Clamp01(lookAtWeight - 1f * Time.deltaTime) * 0.5f;
                    animator.SetLookAtWeight(lookAtWeight);
                }


                if (ikEnabled)
                {
                    animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand.IKPoint);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand.IKPoint);

                    if (rightFoot.previousIkVecPoint != Vector3.zero && leftFoot.previousIkVecPoint != Vector3.zero && rightFoot.currentIkVecPoint != Vector3.zero && leftFoot.currentIkVecPoint != Vector3.zero && animator.GetFloat("freeHang") < 0.5f)
                    {
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFoot.IKPoint);
                        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFoot.IKPoint);
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFoot.weight);
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFoot.weight);
                    }
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHand.weight);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHand.weight);
                }
                else
                {

                    void setIK(IKweights bodyPart, HumanBodyBones bone, AvatarIKGoal ikGoal)
                    {
                        Vector3 previous = bodyPart.previousVecPoint;
                        Vector3 current = bodyPart.currentVecPoint;

                        var curDis = (animator.GetBoneTransform(bone).transform.position - current).magnitude;
                        var preDis = (animator.GetBoneTransform(bone).transform.position - previous).magnitude;

                        if (currentPoint == previousPoint || previousPoint == null) preDis = 1000f;

                        if (curDis < preDis)
                        {
                            bodyPart.previousIkVecPoint = bodyPart.currentIkVecPoint;

                        }
                        animator.SetIKPosition(ikGoal, curDis < preDis ? bodyPart.currentIkVecPoint : bodyPart.previousIkVecPoint);

                        animator.SetIKPositionWeight(ikGoal, 0.75f - ((curDis < preDis ? curDis : preDis) - 0.1f) / 0.3f); // hop animation smoothness if offsets ,tweak if you want
                    }

                    setIK(rightHand, HumanBodyBones.RightHand, AvatarIKGoal.RightHand);
                    setIK(leftHand, HumanBodyBones.LeftHand, AvatarIKGoal.LeftHand);

                    if (animator.GetFloat("freeHang") < 0.5f)
                    {

                        setIK(rightFoot, HumanBodyBones.RightFoot, AvatarIKGoal.RightFoot);
                        setIK(leftFoot, HumanBodyBones.LeftFoot, AvatarIKGoal.LeftFoot);
                    }
                }
            }

        }


        private Vector3 getFootIK(Vector3 rayStartPos, Vector3 rayDir)
        {
            //Vector3 rayStartPos = animator.GetIKPosition(ikGoal);
            rayStartPos -= rayDir * 0.2f;
            bool isWall = Physics.Raycast(rayStartPos, rayDir, out RaycastHit hitInfo, footIkRayLength, parkourAI.ObstacleLayer);
            //Debug.DrawRay(rayStartPos, rayDir * footIkRayLength, Color.green);

            var point = Vector3.zero;
            if (isWall)
            {
                point = hitInfo.point + hitInfo.normal * footPlacementOffset;
            }
            return point;
        }

        public IEnumerator ClimbWhenFalling()
        {
            parkourAI.DisableRootMotion();
            parkourAI.InAction = true;
            parkourAI.IsHanging = true;

            player.OnStartAction();

            string animationName;
            if (CheckWall(currentPoint).Value.isWall)
            {
                animator.SetFloat("freeHang", 0);
                animationName = "PredictiveToBracedhang";
            }
            else
            {
                animationName = "PredictiveToFreehangOneHanded";
                animator.SetFloat("freeHang", 1);
            }
            animator.CrossFadeInFixedTime(animationName, 0.2f);
            animator.Update(0);

            var right = GetHandPos(currentPoint.transform, AvatarTarget.RightHand);

            var _animator = animatorHelper.sampleAnimation("HangIdles", animator);

            initializeBodyParts();

            animatorHelper.closeSampler(_animator);

            var animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f;
            while (timer <= animState.length)
            {
                timer += Time.deltaTime;

                var handPos = rightHand.boneTransform;
                if (rightHand.boneTransform == Vector3.zero)
                    handPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;

                var offset = right - handPos;
                //transform.position += offset;
                transform.position = Vector3.MoveTowards(transform.position, transform.position + offset, 5f * Time.deltaTime);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(-currentPoint.transform.forward),
                     parkourAI.RotationSpeed * Time.deltaTime);

                yield return null;
            }
            parkourAI.InAction = false;
            parkourAI.EnableRootMotion();
        }

        public void DoClimbJumpAction(ClimbPoint newPoint, float angleDiff = 0)
        {
            previousPoint = currentPoint;
            currentPoint = newPoint;
            var angle = Vector3.SignedAngle(-transform.right, currentPoint.transform.position - previousPoint.transform.position, previousPoint.transform.forward);

            if (angleDiff < 30f && Mathf.Abs(angle) > 60 && Mathf.Abs(angle) < 120)
            {
                animator.SetFloat("x", 0f);
                if (angle > 0 && angle < 180)
                    animator.SetFloat("y", 1f);
                else
                    animator.SetFloat("y", -1f);
            }
            else
            {
                animator.SetFloat("y", 0f);
                if (Mathf.Abs(angle) > 90)
                    animator.SetFloat("x", 1f);
                else
                    animator.SetFloat("x", -1f);
            }

            var match = new Vector3();

            var isWall = CheckWall(currentPoint).Value.isWall;

            var climbTransition = 0;
            if (animator.GetFloat("freeHang") > 0.7f && isWall)
                climbTransition = -1;
            else if (animator.GetFloat("freeHang") < 0.3f && !isWall)
                climbTransition = 1;

            StartCoroutine(JumpToLedge(currentPoint.transform, "ClimbTree", match.x, match.y, matchHand: match.z == 0 ? AvatarTarget.RightHand : AvatarTarget.LeftHand, climbTransition: climbTransition));
        }

        public void DoShimmyAction(ClimbPoint newPoint)
        {
            previousPoint = currentPoint;
            currentPoint = newPoint;

            var match = new Vector3();

            var angle = Vector3.Angle(transform.right, currentPoint.transform.position - previousPoint.transform.position);
            var angleSigned = Vector3.SignedAngle(transform.right, currentPoint.transform.position - previousPoint.transform.position, previousPoint.transform.forward);

            var mirror = false;

            var isWall = CheckWall(currentPoint).Value.isWall;

            var climbTransition = 0;
            if (animator.GetFloat("freeHang") > 0.7f && isWall)
                climbTransition = -1;
            else if (animator.GetFloat("freeHang") < 0.3f && !isWall)
                climbTransition = 1;

            var x = 0.5f * Mathf.Cos(angleSigned * Mathf.Deg2Rad);
            var y = -0.5f * Mathf.Sin(angleSigned * Mathf.Deg2Rad);

            animator.SetFloat("x", Mathf.Abs(x));
            animator.SetFloat("y", y);

            if (angle > 85 && angle < 95) mirror = !animator.GetBool("mirrorAction");
            //else if (angle < 90) mirror = true;
            else if (x < 0)
            {
                mirror = true;
                //match = new Vector3(0.40f, 0.6f, 0);
            }
            //else {
            //    mirror = false;
            //}

            StartCoroutine(JumpToLedge(currentPoint.transform, "ClimbTree", match.x, match.y, matchHand: match.z == 0 ? AvatarTarget.RightHand : AvatarTarget.LeftHand, climbTransition: climbTransition, ikAssist: true, mirror: mirror));
        }

        public bool DropToLedge(Transform currentLedge, Vector3 point, Vector3? checkDirection = null)
        {
            var newPoint = GetNearestPoint(currentLedge, point, checkDirection: checkDirection);
            if (newPoint == null)
                return false;
            DropToPoint(newPoint);
            return true;
        }

        public bool DropToPoint(ClimbPoint newPoint)
        {
            if (newPoint == null) return false;
            currentPoint = newPoint;

            player.OnStartAction();

            if (CheckWall(currentPoint).Value.isWall)
            {
                animator.SetFloat("freeHang", 0);
                StartCoroutine(JumpToLedge(currentPoint.transform, "DropToHang", 0.50f, 0.90f, rotateToLedge: true, matchStart: AvatarTarget.LeftFoot, onComplete: () =>
                {
                    animator.SetFloat("freeHang", 0);

                }));
            }
            else
            {
                animator.SetFloat("freeHang", 1);
                StartCoroutine(JumpToLedge(currentPoint.transform, "DropToFreeHang", 0.50f, 0.89f, rotateToLedge: true, matchStart: AvatarTarget.LeftFoot, onComplete: () =>
                {
                    animator.SetFloat("freeHang", 1);

                }));
            }
            return true;
        }

        public void MountPoint()
        {
            if (animator.GetFloat("freeHang") < 0.5f)
                StartCoroutine(DoClimbingAction("BracedHangClimb",
                    onComplete: () =>
                    {
                        player.OnEndAction();
                        parkourAI.IsHanging = false;
                        currentPoint = null;
                    }));
            else
                StartCoroutine(DoClimbingAction("FreeHangClimb",
                   onComplete: () =>
                   {
                       player.OnEndAction();
                       parkourAI.IsHanging = false;
                       currentPoint = null;
                   }));
        }
        public bool ClimbToLedge(Transform currentLedge, Vector3 point, string animName = null, bool combo = false, Vector3? checkDirection = null)
        {
            var newPoint = GetNearestPoint(currentLedge, point, checkDirection: checkDirection);

            if (newPoint == null)
                return false;

            ClimbToPoint(newPoint, point, animName, combo, checkDirection);
            return true;
        }

        public bool ClimbToPoint(ClimbPoint newPoint, Vector3? point = null, string animName = null, bool combo = false, Vector3? checkDirection = null, Vector3? nextJumpPoint = null)
        {
            if (newPoint == null)
                return false;

            previousPoint = currentPoint;
            currentPoint = newPoint;

            var distance = (new Vector3(currentPoint.transform.position.x - transform.position.x, 0, currentPoint.transform.position.z - transform.position.z)).magnitude;

            if (CheckWall(currentPoint).Value.isWall)
            {
                if (distance > 1.5f || combo)
                {
                    if (parkourAI.IsHanging)
                        StartCoroutine(parkourAI.DoPredictiveBackJump(null, currentPoint, 0, combo: combo));
                    else
                        StartCoroutine(parkourAI.DoPredictiveClimb(currentPoint, 0, nextJumpPoint: nextJumpPoint));
                }
                else if (!parkourAI.IsHanging)
                {
                    animator.SetFloat("freeHang", 0);
                    player.OnStartAction();
                    StartCoroutine(JumpToLedge(currentPoint.transform, "IdleToBracedHang", 0.44f, 0.68f, matchStart: AvatarTarget.RightFoot));
                }
                else currentPoint = previousPoint;
            }
            else
            {
                if (distance > 1.5f || combo)
                {
                    if (parkourAI.IsHanging)
                        StartCoroutine(parkourAI.DoPredictiveBackJump(null, currentPoint, 1, combo: combo));
                    else
                        StartCoroutine(parkourAI.DoPredictiveClimb(currentPoint, 1, nextJumpPoint: nextJumpPoint));
                }
                else if (!parkourAI.IsHanging)
                {
                    animator.SetFloat("freeHang", 1);
                    player.OnStartAction();
                    StartCoroutine(JumpToLedge(currentPoint.transform, "IdleToFreeHang", 0.5f, 0.8f, matchStart: AvatarTarget.RightFoot));
                }
                else currentPoint = previousPoint;
            }
            return true;
        }

        public void initializeBodyParts(TargetMatchParams matchParams = null, AnimatorHelper _animatorHelper = null, float climbTransition = 0, bool mirror = false)
        {
            initializeStartBodyParts(matchParams, _animatorHelper, climbTransition, mirror);
            initializeEndBodyParts(matchParams, _animatorHelper, climbTransition, mirror);
        }

        public float handSpacing;

        public void initializeStartBodyParts(TargetMatchParams matchParams = null, AnimatorHelper _animatorHelper = null, float climbTransition = 0, bool mirror = false)
        {
            Vector3 curHandOffsets = handOffsets;

            if (_animatorHelper == null) _animatorHelper = animatorHelper;
            if (previousPoint)
            {
                var startCenter = (_animatorHelper.getTransformPos(HumanBodyBones.LeftHand, 0) + _animatorHelper.getTransformPos(HumanBodyBones.RightHand, 0)) / 2;

                rightFoot.startBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.RightFoot, 0) - startCenter);
                leftFoot.startBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.LeftFoot, 0) - startCenter);
                rightHand.startBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.RightHand, 0) - startCenter) + curHandOffsets;
                leftHand.startBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.LeftHand, 0) - startCenter) + curHandOffsets;

                rightFoot.previousVecPoint = _animatorHelper.pointTransformWithVectorDown(previousPoint.transform, rightFoot.startBodyPartOffset);
                rightFoot.previousIkVecPoint = getFootIK(rightFoot.previousVecPoint, -previousPoint.transform.forward);
                if (rightFoot.previousIkVecPoint == Vector3.zero) rightFoot.previousIkVecPoint = rightFoot.previousVecPoint;

                leftFoot.previousVecPoint = _animatorHelper.pointTransformWithVectorDown(previousPoint.transform, leftFoot.startBodyPartOffset);
                leftFoot.previousIkVecPoint = getFootIK(leftFoot.previousVecPoint, -previousPoint.transform.forward);
                if (leftFoot.previousIkVecPoint == Vector3.zero) leftFoot.previousIkVecPoint = leftFoot.previousVecPoint;

                rightHand.previousVecPoint = _animatorHelper.pointTransformWithVectorDown(previousPoint.transform, rightHand.startBodyPartOffset);
                rightHand.previousIkVecPoint = _animatorHelper.pointTransformWithVectorDown(previousPoint.transform, rightHand.startBodyPartOffset - handSpacing * Vector3.right);
                //rightHand.previousIkVecPoint = rightHand.previousVecPoint;

                leftHand.previousVecPoint = _animatorHelper.pointTransformWithVectorDown(previousPoint.transform, leftHand.startBodyPartOffset);
                leftHand.previousIkVecPoint = _animatorHelper.pointTransformWithVectorDown(previousPoint.transform, leftHand.startBodyPartOffset + handSpacing * Vector3.right);
                //leftHand.previousIkVecPoint = leftHand.previousVecPoint;
            }
        }

        public void initializeEndBodyParts(TargetMatchParams matchParams = null, AnimatorHelper _animatorHelper = null, float climbTransition = 0, bool mirror = false)
        {
            Vector3 curHandOffsets = handOffsets;

            if (_animatorHelper == null) _animatorHelper = animatorHelper;
            if (currentPoint)
            {
                var endCenter = (_animatorHelper.getTransformPos(HumanBodyBones.LeftHand, 1) + _animatorHelper.getTransformPos(HumanBodyBones.RightHand, 1)) / 2;

                rightFoot.endBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.RightFoot, 1) - endCenter);
                leftFoot.endBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.LeftFoot, 1) - endCenter);
                rightHand.endBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.RightHand, 1) - endCenter) + curHandOffsets;
                leftHand.endBodyPartOffset = (_animatorHelper.getTransformPos(HumanBodyBones.LeftHand, 1) - endCenter) + curHandOffsets;

                rightFoot.currentVecPoint = _animatorHelper.pointTransformWithVectorDown(currentPoint.transform, rightFoot.endBodyPartOffset);
                rightFoot.currentIkVecPoint = getFootIK(rightFoot.currentVecPoint, -currentPoint.transform.forward);
                if (rightFoot.currentIkVecPoint == Vector3.zero) rightFoot.currentIkVecPoint = rightFoot.currentVecPoint;

                leftFoot.currentVecPoint = _animatorHelper.pointTransformWithVectorDown(currentPoint.transform, leftFoot.endBodyPartOffset);
                leftFoot.currentIkVecPoint = getFootIK(leftFoot.currentVecPoint, -currentPoint.transform.forward);
                if (leftFoot.currentIkVecPoint == Vector3.zero) leftFoot.currentIkVecPoint = leftFoot.currentVecPoint;

                rightHand.currentVecPoint = _animatorHelper.pointTransformWithVectorDown(currentPoint.transform, rightHand.endBodyPartOffset);
                rightHand.currentIkVecPoint = _animatorHelper.pointTransformWithVectorDown(currentPoint.transform, rightHand.endBodyPartOffset - handSpacing * Vector3.right);
                //rightHand.currentIkVecPoint = rightHand.currentVecPoint;

                leftHand.currentVecPoint = _animatorHelper.pointTransformWithVectorDown(currentPoint.transform, leftHand.endBodyPartOffset);
                leftHand.currentIkVecPoint = _animatorHelper.pointTransformWithVectorDown(currentPoint.transform, leftHand.endBodyPartOffset + handSpacing * Vector3.right);
                //leftHand.currentIkVecPoint = leftHand.currentVecPoint;
            }

            if (matchParams == null) return;
            if (matchParams.target == AvatarTarget.RightHand)
            {
                matchParams.pos = rightHand.currentVecPoint;
            }
            else if (matchParams.target == AvatarTarget.LeftHand)
            {
                matchParams.pos = leftHand.currentVecPoint;
            }
        }

        public void DoShimmy(float normalTime, bool mirror = false, bool HangTypeChange = false)
        {
            float[] lerpValues = new float[4];


            //lerp value offsets , equation = (normalTime - startTime)/endTime
            lerpValues[0] = (normalTime - 0.1f) / 0.3f; //rightFoot
            lerpValues[1] = (normalTime - 0) / 0.3f; //rightHand
            lerpValues[2] = (normalTime - 0.5f) / 0.5f; //leftHand
            lerpValues[3] = (normalTime - 0.6f) / 0.4f; //leftFoot

            //if (animator.GetFloat("x") < 0f || mirror) Array.Reverse(lerpValues);
            if (mirror) Array.Reverse(lerpValues);

            rightFoot.lerpValue = lerpValues[0];
            rightHand.lerpValue = lerpValues[1];
            leftHand.lerpValue = lerpValues[2];
            leftFoot.lerpValue = lerpValues[3];

            rightHand.IKPoint = Vector3.Slerp(rightHand.previousIkVecPoint, rightHand.currentIkVecPoint, rightHand.lerpValue);
            leftHand.IKPoint = Vector3.Slerp(leftHand.previousIkVecPoint, leftHand.currentIkVecPoint, leftHand.lerpValue);

            rightHand.weight = rightFoot.weight = 1.3f;
            leftHand.weight = leftFoot.weight = 1.3f;

            rightHand.weight = Mathf.Abs(0.5f - rightHand.lerpValue) * 0.5f + 1f;
            rightFoot.weight = Mathf.Abs(0.5f - rightFoot.lerpValue) * 0.5f + 1f;
            leftHand.weight = Mathf.Abs(0.5f - leftHand.lerpValue) * 0.5f + 1f;
            leftFoot.weight = Mathf.Abs(0.5f - leftFoot.lerpValue) * 0.5f + 1f;

            var freeHanglerp = (0.5f - animator.GetFloat("freeHang"));
            rightFoot.IKPoint = Vector3.Slerp(rightFoot.previousIkVecPoint, rightFoot.currentIkVecPoint, rightFoot.lerpValue);
            leftFoot.IKPoint = Vector3.Slerp(leftFoot.previousIkVecPoint, leftFoot.currentIkVecPoint, leftFoot.lerpValue);
            rightFoot.weight *= freeHanglerp;
            leftFoot.weight *= freeHanglerp;

        }

        WallInfo? CheckWall(ClimbPoint point)
        {
            if (point == null) return null;

            WallInfo wallinfo = new WallInfo();

            var rightFootPoint = point.transform.position + wallRayOffset.x * point.transform.right + wallRayOffset.y * point.transform.up + wallRayOffset.z * point.transform.forward;
            var leftFootPoint = point.transform.position - wallRayOffset.x * point.transform.right + wallRayOffset.y * point.transform.up + wallRayOffset.z * point.transform.forward;

            wallinfo.isWall = Physics.SphereCast(rightFootPoint, 0.1f, -point.transform.forward, out wallinfo.rightFootInfo, hipRayLength, parkourAI.ObstacleLayer);
            // Debug.DrawRay(rightFootPoint, -point.transform.forward * hipRay, isWall ? Color.green : Color.red);
            if (wallinfo.isWall)
            {
                wallinfo.isWall = Physics.SphereCast(leftFootPoint, 0.1f, -point.transform.forward, out wallinfo.leftFootInfo, hipRayLength, parkourAI.ObstacleLayer);
            }

            // Debug.DrawRay(leftFootPoint, -point.transform.forward * hipRay, isWall ? Color.green : Color.red);

            return wallinfo;
        }
        public struct WallInfo
        {
            public bool isWall;
            public RaycastHit rightFootInfo;
            public RaycastHit leftFootInfo;
        }

        public bool RayCastCheck(ClimbPoint prevPoint, ClimbPoint currPoint)
        {
            var previousOffset = prevPoint.transform.position + obstacleRayOffset.y * prevPoint.transform.up + obstacleRayOffset.z * prevPoint.transform.forward;

            var currentOffset = currPoint.transform.position + obstacleRayOffset.y * currPoint.transform.up + obstacleRayOffset.z * currPoint.transform.forward;

            var direction = currPoint.transform.position - prevPoint.transform.position;



            var preDir = previousOffset + Vector3.ProjectOnPlane(direction, prevPoint.transform.forward);
            var curDir = currentOffset + Vector3.ProjectOnPlane(-direction, currPoint.transform.forward);


            var middlePoint = (preDir + curDir) / 2;



            if (!Physics.Raycast(previousOffset, middlePoint - previousOffset, out RaycastHit hitInfo1, (middlePoint - previousOffset).magnitude, (parkourAI.ObstacleLayer | LedgeLayer)))
                if (!Physics.Raycast(currentOffset, middlePoint - currentOffset, out RaycastHit hitInfo2, (middlePoint - currentOffset).magnitude, (parkourAI.ObstacleLayer | LedgeLayer)))
                {
                    previousOffset += Vector3.down * 0.5f;
                    currentOffset += Vector3.down * 0.5f;
                    middlePoint += Vector3.down * 0.5f;

                    if (!Physics.Raycast(previousOffset, middlePoint - previousOffset, out RaycastHit hitInfo3, (middlePoint - previousOffset).magnitude, (parkourAI.ObstacleLayer | LedgeLayer)))
                        if (!Physics.Raycast(currentOffset, middlePoint - currentOffset, out RaycastHit hitInfo4, (middlePoint - currentOffset).magnitude, (parkourAI.ObstacleLayer | LedgeLayer)))
                            return false;
                }
            return true;
        }

        public Vector3 GetHandPos(Transform ledge, AvatarTarget hand, Vector3? handOffset = null)
        {
            //initializeBodyParts();
            rightHand.currentIkVecPoint = animatorHelper.pointTransformWithVectorDown(ledge.transform, rightHand.endBodyPartOffset);
            leftHand.currentIkVecPoint = animatorHelper.pointTransformWithVectorDown(ledge.transform, leftHand.endBodyPartOffset);
            var point = (hand == AvatarTarget.RightHand) ? rightHand.currentIkVecPoint : leftHand.currentIkVecPoint;

            return point;
        }

        public IEnumerator JumpToLedge(Transform ledge, string anim, float matchStartTime, float matchEndTime,
            Vector3? offset = null,
            AvatarTarget matchHand = AvatarTarget.RightHand,
            AvatarTarget matchStart = AvatarTarget.RightHand,
            bool rotateToLedge = true,
            Action onComplete = null, int climbTransition = 0, bool ikAssist = false, bool mirror = false, bool autoMatch = true)
        {
            //if (parkourController.InAction) yield break;


            if (mirror) matchHand = matchHand == AvatarTarget.RightHand ? AvatarTarget.LeftHand : AvatarTarget.RightHand;
            if (mirror)
            {
                if (matchStart == AvatarTarget.RightHand)
                    matchStart = AvatarTarget.LeftHand;
                else if (matchStart == AvatarTarget.LeftHand)
                    matchStart = AvatarTarget.RightHand;
            }


            var grabPos = GetHandPos(ledge, matchHand, offset);

            var targetRot = Quaternion.LookRotation(-ledge.forward);
            //var targetRot = Quaternion.LookRotation(-(transform.position - ledge.position).normalized );

            var matchParams = new TargetMatchParams()
            {
                pos = grabPos,
                startTime = matchStartTime,
                endTime = matchEndTime,
                target = matchHand,
                startTarget = matchStart,
                posWeight = Vector3.one
            };

            parkourAI.IsHanging = true;

            yield return DoClimbingAction(anim, rotateToLedge, targetRot, matchParams, onComplete: onComplete, climbTransition: climbTransition, ikAssist: ikAssist, mirror: mirror, autoMatch: autoMatch);
            ikEnabled = false;

        }

        AnimatorHelper animatorHelper = new AnimatorHelper();

        public IEnumerator DoClimbingAction(string anim, bool rotate = false,
              Quaternion targetRot = new Quaternion(), TargetMatchParams matchParams = null, bool mirror = false, Action onComplete = null, int climbTransition = 0, bool ikAssist = false, bool autoMatch = true)
        {
            parkourAI.EnableRootMotion();
            parkourAI.InAction = true;

            if (player.WaitToStartSystem)
                yield return new WaitUntil(() => player.WaitToStartSystem == false);

            animator.SetBool("mirrorAction", mirror);

            AnimatorStateInfo animState;

            if (matchParams != null)
            {
                var _animator = animatorHelper.sampleAnimation(anim, animator);
                matchParams.startTime = animatorHelper.findStartTime(matchParams.startTarget);

                initializeStartBodyParts(matchParams, climbTransition: climbTransition, mirror: mirror);
                matchParams.startPos = transform.position;

                var maxDelta = 0.005f;

                if (climbTransition != 0)
                {
                    _animator.SetFloat("freeHang", climbTransition == 1 ? 1 : -1);
                    maxDelta = 0.001f;
                }
                //if(animator.GetFloat("freeHang") > 0.5f)
                //    maxDelta = 0.001f;
                if (ikAssist)
                    maxDelta = 0.001f;

                matchParams.endTime = animatorHelper.findEndTime(matchParams.target, maxDelta);
                initializeEndBodyParts(matchParams, climbTransition: climbTransition, mirror: mirror);



                if (matchParams.startTime > matchParams.endTime) (matchParams.startTime, matchParams.endTime) = (matchParams.endTime, matchParams.startTime);

                //matchParams.endPos = animatorHelper.getRotatedPos(AvatarTarget.Root, matchParams.endTime) - animatorHelper.getRotatedPos(matchParams.target, matchParams.endTime);

                if (matchParams.target == AvatarTarget.RightHand)
                {
                    matchParams.endPos = AnimatorHelper._animator.transform.position - animatorHelper.getTransformPos(HumanBodyBones.RightHand, matchParams.endTime);
                }
                else if (matchParams.target == AvatarTarget.LeftHand)
                {
                    matchParams.endPos = AnimatorHelper._animator.transform.position - animatorHelper.getTransformPos(HumanBodyBones.LeftHand, matchParams.endTime);
                }
                else
                    matchParams.endPos = animatorHelper.getRotatedPos(AvatarTarget.Root, matchParams.endTime) - animatorHelper.getRotatedPos(matchParams.target, matchParams.endTime);

                matchParams.endPos = animatorHelper.pointTransformWithVectorDown(currentPoint.transform, matchParams.endPos, matchParams.pos);
                var changedPos = animatorHelper.getRotatedPos(AvatarTarget.Root, matchParams.endTime) - animatorHelper.getRotatedPos(matchParams.target, matchParams.endTime);
                changedPos = animatorHelper.pointTransformWithVectorDown(currentPoint.transform, changedPos, matchParams.pos);

                matchParams.pos += (matchParams.endPos - changedPos);


                animatorHelper.closeSampler(_animator);
            }

            animator.CrossFadeInFixedTime(anim, 0.2f);
            yield return null;

            animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f;

            var localPos = transform.position;

            while (timer <= animState.length)
            {
                timer += Time.deltaTime;
                float normalTime = timer / animState.length;

                if (matchParams != null)
                {
                    //autoMatcher(matchParams, normalTime);
                    MatchTarget(matchParams.pos, matchParams.rot, matchParams.target, new MatchTargetWeightMask(matchParams.posWeight, 0), matchParams.startTime, matchParams.endTime);

                    if (rotate && normalTime >= matchParams.startTime)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                             parkourAI.RotationSpeed * Time.deltaTime);

                }

                if (climbTransition != 0)
                {
                    var lerpMiddleTime = (matchParams.startTime + matchParams.endTime) / 2;
                    var lerpingTime = (matchParams.endTime - matchParams.startTime);
                    //var lerpValue = climbTransition < 0 ? (normalTime - lerpMiddleTime) / (lerpingTime / 2) : (normalTime - matchParams.startTime) / (lerpingTime / 2);
                    var lerpValue = ikAssist ? (normalTime - lerpMiddleTime) / (lerpingTime / 2) : (normalTime - matchParams.startTime) / (lerpingTime);
                    animator.Update(-Time.deltaTime);
                    animator.SetFloat("freeHang", Mathf.Lerp((climbTransition - Math.Abs(climbTransition)) / -2, (climbTransition + Math.Abs(climbTransition)) / 2, lerpValue));
                    animator.Update(Time.deltaTime);
                }

                if (ikAssist)
                {
                    ikEnabled = true;
                    DoShimmy(normalTime, mirror, climbTransition != 0 ? true : false);
                }

                //else if (matchParams != null  && normalTime >= Mathf.Max(matchParams.endTime, 0.9f)) break; // for Smoothness

                yield return null;
            }

            previousPoint = currentPoint;
            parkourAI.InAction = false;

            onComplete?.Invoke();
        }
        void MatchTarget(Vector3 matchPos, Quaternion rotation, AvatarTarget target, MatchTargetWeightMask weightMask, float startTime, float endTime)
        {
            if (animator.isMatchingTarget || animator.IsInTransition(0)) return;

            animator.MatchTarget(matchPos, rotation, target, weightMask, startTime, endTime);
        }

        //distance to point
        public float GetDistPointToLine(Vector3 origin, Vector3 direction, Vector3 point)
        {
            direction.Normalize();
            Vector3 point2origin = point - origin;
            Vector3 point2closestPointOnLine = point2origin - Vector3.Dot(point2origin, direction) * direction;
            return point2closestPointOnLine.magnitude;
        }


        public ClimbPoint GetNearestPoint(Transform ledge, Vector3 hitPoint, bool checkAngle = true, Vector3? checkDirection = null, bool obstacleCheck = true, Vector3? predictiveMaxHeight = null)
        {
            var points = ledge.GetComponentsInChildren<ClimbPoint>();
            //checkDirection = checkDirection == null ? player.MoveDir.normalized : checkDirection.Value;
            checkDirection = !checkDirection.HasValue ? player.MoveDir.normalized : checkDirection.Value;

            if (checkDirection.Value == Vector3.zero) checkDirection = transform.forward;
            //checkDirection = transform.forward;


            ClimbPoint nearestPoint = null;
            float minDistance = 100.0f;
            foreach (var point in points)
            {
                var distance = Vector3.Distance(point.transform.position, hitPoint);

                if (distance < minDistance && distance >= 0.03f)
                {
                    minDistance = distance;
                    nearestPoint = point;
                }
            }

            return nearestPoint;
        }

    }

    public class TargetMatchParams
    {
        public Vector3 pos;
        public Quaternion rot;
        public AvatarTarget target;
        public AvatarTarget startTarget;
        public float startTime;
        public float endTime;

        public Vector3 startPos;
        public Vector3 endPos;

        public Vector3 posWeight;
    }
}
