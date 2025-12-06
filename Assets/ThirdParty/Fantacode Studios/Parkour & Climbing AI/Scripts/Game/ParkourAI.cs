using FS_ParkourSystem;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ParkourAI
{
    public class ParkourAI : MonoBehaviour
    {
        //[field: Header("Predictive Jumping")]
        [HideInInspector]
        [field: Tooltip("Enables Predictive Jumping")]
        public bool enablePredictiveJump = true;

        [Tooltip("Enables balance walking on narrow beams")]
        public bool enableBalanceWalk = false;

        [Tooltip("Controls the initial jump velocity")]
        public float forwardJumpSpeed = 4.5f;

        [Tooltip("Controls the initial jump velocity")]
        public float maxJumpHeight = 1.5f;

        [Tooltip("List of Parkour Actions the player can perform while standing close to the obstacle")]
        public List<ParkourAction> parkourActions = new List<ParkourAction>();


        [field: Tooltip("The speed at which the player will rotate while performing parkour or climbing actions")]
        [field: SerializeField] public float RotationSpeed { get; private set; } = 500f;

        public LayerMask ObstacleLayer = 1;

        public bool InAction { get; set; }
        public bool IsHanging { get; set; }
        public bool IsOnLedge => isOnLedge;

        public bool ControlledByParkour => InAction || IsHanging;


        // Predictive jump states
        bool mirrorJump = false;
        bool inPredictiveJump = false;
        bool inAirOfPredictiveJump = false;
        bool isGrounded = false;
        bool isOnLedge = false;
        bool prevControlVal;


        float ySpeed = 0f;
        JumpData _jumpPoint;
        Animator animator;
        IAICharacter player;

        [HideInInspector]
        public ClimbAI climbController;


        private void OnEnable()
        {
            player = GetComponent<IAICharacter>();
            climbController = GetComponent<ClimbAI>();
            animator = player.Animator;
            animator.SetFloat("fallAmount", 1);
        }

        public bool HandleParkourAction(ObstacleHitData hitData)
        {
            if (!InAction && hitData.forwardHitFound && hitData.heightHitFound
                && hitData.hasSpace
                && Vector3.Angle(Vector3.up, hitData.forwardHit.normal) > 45f
                && Vector3.Angle(Vector3.up, hitData.heightHit.normal) <= 45f
                )
            {
                List<ParkourAction> selectedActions = new();
                foreach (var action in parkourActions)
                {
                    if (action.CheckIfPossible(hitData, transform))
                    {
                        selectedActions.Add(action);
                    }
                }
                if (selectedActions.Count > 0)
                {
                    StartCoroutine(DoParkourAction(selectedActions[UnityEngine.Random.Range(0, selectedActions.Count)]));
                    return true;
                }
            }
            return false;
        }
        public ParkourAction GetParkourAction(ObstacleHitData hitData)
        {
            if (!InAction && hitData.forwardHitFound && hitData.heightHitFound
                && hitData.hasSpace
                && Vector3.Angle(Vector3.up, hitData.forwardHit.normal) > 45f
                && Vector3.Angle(Vector3.up, hitData.heightHit.normal) <= 45f
                )
            {
                List<ParkourAction> selectedActions = new();
                foreach (var action in parkourActions)
                {
                    if (action.CheckIfPossible(hitData, transform))
                    {
                        selectedActions.Add(action);
                    }
                }
                if (selectedActions.Count > 0)
                {
                    return selectedActions[UnityEngine.Random.Range(0, selectedActions.Count)];
                }
            }
            return null;
        }

        private void OnAnimatorMove()
        {
            if (player.UseRootMotion)
            {
                if (animator.deltaPosition != Vector3.zero)
                    transform.position += animator.deltaPosition;
                transform.rotation *= animator.deltaRotation;
            }
        }

        Transform jumpTarget;

        bool matchFootToTarget;
        float footMatchWeight = 0f;
        public IEnumerator DoPredictiveJump(JumpData jumpPoint, string animName = null, float crossFadeTime = 0.2f)
        {
            DisableRootMotion();

            player.OnStartAction();

            InAction = true;
            if (player.WaitToStartSystem)
                yield return new WaitUntil(() => player.WaitToStartSystem == false);

            _jumpPoint = jumpPoint;

            inAirOfPredictiveJump = true;
            inPredictiveJump = true;

            matchFootToTarget = false;
            isGrounded = false;
            bool isFalling = false;

            footMatchWeight = 0f;

            animator.SetBool("IsGrounded", false);

            var dispVec = jumpPoint.rootPosition - transform.position;
            dispVec.y = 0f;
            var targetRot = Quaternion.LookRotation(dispVec);
            var anim = animName;

            Vector3 deltaRootPos = Vector3.zero;
            if (animName == null)
            {
                mirrorJump = !mirrorJump;
                animName = (mirrorJump) ? "Predictive JumpM" : "Predictive Jump";
                animator.SetBool("mirrorJump", mirrorJump);
            }

            animator.CrossFadeInFixedTime(animName, crossFadeTime);
            yield return null;
            var t = 0f;
            if (anim == null)
            {
                EnableRootMotion();
                while (true)
                {
                    t += Time.deltaTime;
                    if (t > .15f && Quaternion.Angle(transform.rotation, targetRot) < .5f)
                    {
                        t = 0;
                        break;
                    }
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * 2 * Time.deltaTime);
                    yield return null;
                }
                DisableRootMotion();
            }
            Vector3 jumpVel;
            float jumpTime;

            (jumpVel, jumpTime) = CalculateJumpVelocity(transform.position, jumpPoint.rootPosition + targetRot * deltaRootPos);

            matchPosGlobal = jumpPoint.rootPosition;

            Vector3 startPos = transform.position;

            ySpeed = jumpVel.y;

            //bool playLand = true;
            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime;

                Vector3 velocity = jumpVel;
                ySpeed += player.Gravity * Time.deltaTime;
                velocity.y = ySpeed;

                transform.position += velocity * Time.deltaTime;

                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);

                if (timer > 0.02f)
                {
                    //JumpGroundCheck();
                    if (timer >= jumpTime)
                        isGrounded = true;
                    if (isGrounded)
                    {
                        animator.SetBool("IsGrounded", isGrounded);
                        transform.position = new Vector3(transform.position.x, jumpPoint.rootPosition.y, transform.position.z);

                        if (isFalling)
                        {
                            var halfExtends = new Vector3(.3f, .9f, 0.01f);
                            var hasSpaceForRoll = Physics.BoxCast(transform.position + Vector3.up, halfExtends, transform.forward, Quaternion.LookRotation(transform.forward), 2.5f, ObstacleLayer);

                            halfExtends = new Vector3(.1f, .1f, 0.01f);
                            var heightHiting = true;
                            for (int i = 0; i < 6 && heightHiting; i++)
                                heightHiting = Physics.BoxCast(transform.position + Vector3.up * 1.8f + transform.forward * (i * .5f + .5f), halfExtends, Vector3.down, Quaternion.LookRotation(Vector3.down), 2.2f + i * .2f, ObstacleLayer);
                            if (jumpPoint.hasSpaceToLand)
                            {
                                matchFootToTarget = false;

                                EnableRootMotion();

                                if (!hasSpaceForRoll && heightHiting)
                                {
                                    yield return DoAction("FallingToRoll");
                                }
                                else
                                {
                                    animator.SetFloat(AnimatorParameters.fallAmount, 1);
                                    yield return DoAction("Landing");
                                }
                                DisableRootMotion();
                            }
                            else
                            {
                                animator.SetFloat(AnimatorParameters.fallAmount, 0);
                                animator.CrossFadeInFixedTime("Landing", .13f);
                            }

                            isFalling = false;
                            animator.SetBool("isFalling", isFalling);
                        }
                        else
                        {
                            if (jumpPoint.hasSpaceToLand)
                                animator.SetFloat(AnimatorParameters.fallAmount, 0);
                            animator.CrossFadeInFixedTime((jumpPoint.hasSpaceToLand) ? "LandAndStepForward" : "Landing", .13f);
                        }
                        break;
                    }
                }

                if (!isFalling && timer > .6f && startPos.y - jumpPoint.rootPosition.y > 3f)
                {
                    isFalling = true;
                    animator.SetBool("isFalling", isFalling);
                }

                yield return null;
            }

            transform.rotation = targetRot;

            inAirOfPredictiveJump = false;
            IsHanging = false;

            //yield return new WaitForSeconds(0.5f);

            player.OnEndAction();
            DisableRootMotion();
            InAction = false;
            inPredictiveJump = false;

        }

        public float getJumpHeight(float displacementY, Vector3 displacementXZ)
        {
            var h = Mathf.Max(displacementY, 0.07f);
            h += (displacementXZ.magnitude * 0.08f);
            return h;
        }

        (Vector3, float) CalculateJumpVelocity(Vector3 startPos, Vector3 targetPos)
        {
            float gravity = player.Gravity;

            float displacementY = targetPos.y - startPos.y;
            var displacementXZ = new Vector3(targetPos.x - startPos.x, 0, targetPos.z - startPos.z);

            var h = getJumpHeight(displacementY, displacementXZ);

            float ty = Mathf.Sqrt(-2 * h / gravity);
            float tx = Mathf.Sqrt(2 * (displacementY - h) / gravity);
            float t = tx + ty;

            var velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * h);
            var velocityXZ = displacementXZ / t;

            return (velocityY + velocityXZ, t);
        }

        public IEnumerator DoPredictiveBackJump(JumpData jumpPoint = null, ClimbPoint climbPoint = null, int climbTransition = 0, bool combo = false)
        {
            Vector3 dir;
            if (jumpPoint != null)
                dir = transform.position - jumpPoint.rootPosition;
            else
                dir = transform.position - climbPoint.transform.position;

            var angleBWvec = Vector3.SignedAngle(transform.forward, new Vector3(dir.x, 0, dir.z), Vector3.down);
            var value = angleBWvec / 90;

            //animator.SetFloat("jumpBackDirection", Mathf.Sign(value) * 0.1f + value);

            var currentPoint = climbPoint != null ? climbController.previousPoint : climbController.currentPoint;
            if (animator.GetFloat("freeHang") > 0.5f && !currentPoint.transform.parent.CompareTag("SwingableLedge") && Mathf.Abs(value) > 1f)
            {
                if (climbController.currentPoint && climbPoint != null)
                    climbController.currentPoint = climbController.previousPoint;
                inAirOfPredictiveJump = inPredictiveJump = InAction = false;
                IsHanging = true;
                ResetRootMotion();
                yield break;
            }

            InAction = inAirOfPredictiveJump = inPredictiveJump = true;

            DisableRootMotion();
            player.OnStartAction();
            if (player.WaitToStartSystem)
                yield return new WaitUntil(() => player.WaitToStartSystem == false);

            matchFootToTarget = isGrounded = false;
            animator.SetBool("IsGrounded", false);



            var height = getJumpHeight(dir.z, new Vector3(dir.x, 0, dir.z));

            mirrorJump = !mirrorJump;
            var animName = (mirrorJump) ? "Jump Idle M" : "Jump Idle";
            float stopPerc = 0.5f;


            if (animator.GetFloat("freeHang") < 0.5f)
            {
                combo = false;
                animator.SetFloat("jumpBackDirection", Mathf.Sign(value) * Mathf.Clamp(Mathf.Abs(value), 0.1f, 1));
                animator.CrossFade("predictiveJumpBack", 0.2f);

            }
            else
            {
                animator.SetFloat("jumpBackDirection", Mathf.Sign(value) * Mathf.Clamp(Mathf.Abs(value), 0.1f, 2));
                animator.CrossFadeInFixedTime("predictiveJumpBackFreeHang", 0.2f);
                if (combo)
                    animator.CrossFadeInFixedTime("Freehang Forward Jump", 0.2f);

                if (Mathf.Abs(value) > 1.2f) // changeable
                {
                    //stopPerc = 0.7f;
                    stopPerc = Mathf.Clamp(0, 0.7f, height);
                }
                else
                {
                    stopPerc = 0.6f;
                    if (combo)
                    {
                        if (Mathf.Abs(value) < 0.8f)
                        {
                            animator.CrossFade("predictiveJumpBackFreeHang", 0.2f, 0, 0.4f); // 0.4f is peak time
                            stopPerc = 0.1f;
                        }
                        else stopPerc = 0.2f;

                        combo = false;  // enable combo only for forward jumps , otherwise disable
                    }
                }

            }

            yield return null;

            var animState = animator.GetNextAnimatorStateInfo(0);
            var prevHandPos = value > 0 ? climbController.leftHand.boneTransform : climbController.rightHand.boneTransform;

            float startTimer = 0f;
            if (!combo)
            {
                animator.speed = Mathf.Clamp(dir.magnitude * 0.12f, 1f, 1.5f);
                while (startTimer <= animState.length * stopPerc)
                {
                    transform.position += animator.deltaPosition;
                    transform.rotation *= animator.deltaRotation;
                    if (animator.GetFloat("freeHang") > 0.5f)
                    {
                        //var handPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                        var handPos = value > 0 ? climbController.leftHand.boneTransform : climbController.rightHand.boneTransform;
                        //var offset = climbController.rightHand.currentVecPoint - handPos;
                        var offset = prevHandPos - handPos;
                        transform.position = Vector3.MoveTowards(transform.position, transform.position + offset, 10f * Time.deltaTime);
                    }
                    startTimer += Time.deltaTime;
                    yield return null;
                }
                animator.speed = 1;
            }
            else if (animator.GetFloat("freeHang") > 0.5f)
            {
                //while (startTimer <= animState.length * 0.6f)
                while (startTimer <= animState.length * Mathf.Clamp(0, 0.6f, height))
                {
                    transform.position += animator.deltaPosition;
                    transform.rotation *= animator.deltaRotation;
                    if (animator.GetFloat("freeHang") > 0.5f)
                    {
                        //var handPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                        var handPos = value > 0 ? climbController.leftHand.boneTransform : climbController.rightHand.boneTransform;
                        //var offset = climbController.rightHand.currentVecPoint - handPos;
                        var offset = prevHandPos - handPos;
                        transform.position = Vector3.MoveTowards(transform.position, transform.position + offset, 10f * Time.deltaTime);
                    }
                    startTimer += Time.deltaTime;
                    yield return null;
                }
            }

            if (jumpPoint != null)
                StartCoroutine(DoPredictiveJump(jumpPoint, animName, 0.3f));
            else if (climbPoint != null)
                StartCoroutine(DoPredictiveClimb(climbPoint, climbTransition, animName, 0.8f));
            else
            {
                inAirOfPredictiveJump = inPredictiveJump = InAction = false;
                player.OnEndAction();
                ResetRootMotion();
            }
        }

        public IEnumerator DoPredictiveClimb(ClimbPoint point, int climbTransition, string animName = null, float crossFadeTime = 0.2f, Vector3? nextJumpPoint = null)
        {

            DisableRootMotion();
            player.OnStartAction();
            InAction = true;
            if (player.WaitToStartSystem)
                yield return new WaitUntil(() => player.WaitToStartSystem == false);


            inAirOfPredictiveJump = true;
            inPredictiveJump = true;

            matchFootToTarget = false;
            isGrounded = false;


            animator.SetBool("IsGrounded", false);
            Vector3 jumpVel;
            float jumpTime;

            var hangTypeName = climbTransition == 0 ? "PredictiveToBracedhang" : "PredictiveToFreehang";


            //(jumpVel, jumpTime) = CalculateJumpVelocity(transform.position, point.transform.position + -point.transform.up * 1.8f + point.transform.forward * 0.1f);
            AnimatorHelper animatorHelper = new AnimatorHelper();
            var _animator = animatorHelper.sampleAnimation(hangTypeName, animator);

            var endTime = animatorHelper.findEndTime(AvatarTarget.RightHand);

            var pos = animatorHelper.getTransformRootPos(endTime) - animatorHelper.getTransformPos(HumanBodyBones.RightHand, endTime);

            var endCenter = (animatorHelper.getTransformPos(HumanBodyBones.LeftHand, 1) + animatorHelper.getTransformPos(HumanBodyBones.RightHand, 1)) / 2;
            var right = (animatorHelper.getTransformPos(HumanBodyBones.RightHand, 1) - endCenter) + climbController.handOffsets;
            right = animatorHelper.pointTransformWithVectorDown(point.transform, right);

            //GizmosExtend.AddByName("right", () => GizmosExtend.drawSphere(right, 0.01f, Color.grey));

            var rotatedPos = animatorHelper.pointTransformWithVectorDown(point.transform, pos, right);

            climbController.initializeBodyParts(null, animatorHelper);

            matchPosGlobal = right;

            animatorHelper.closeSampler(_animator);

            var dispVec = point.transform.position - transform.position;
            dispVec.y = 0f;
            var targetRot = Quaternion.LookRotation(dispVec);
            var anim = animName;
            if (animName == null)
            {
                mirrorJump = !mirrorJump;
                animName = (mirrorJump) ? "Predictive JumpM" : "Predictive Jump";
                animator.SetBool("mirrorJump", mirrorJump);
            }

            animator.CrossFadeInFixedTime(animName, crossFadeTime);
            animator.Update(0);

            var t = 0f;
            if (anim == null)
            {
                EnableRootMotion();
                while (true)
                {
                    t += Time.deltaTime;
                    if (t > .15f)
                    {
                        t = 0;
                        break;
                    }
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * 5 * Time.deltaTime);
                    yield return null;
                }
                DisableRootMotion();
            }
            (jumpVel, jumpTime) = CalculateJumpVelocity(transform.position, rotatedPos);

            //hangChange
            animator.SetFloat("freeHang", climbTransition);

            Vector3 startPos = transform.position;

            ySpeed = jumpVel.y;

            AnimatorStateInfo animState = animator.GetNextAnimatorStateInfo(0);

            var start = true;
            var idleStart = true;
            float timer = 0f;

            var temp = jumpTime;
            var transitionTime = jumpTime * 0.3f;  // changeble
            var idleTransitionTime = jumpTime * 0.7f;  // changeble


            bool DoCombo = false;
            bool DoQuickCombo = false;
            float comboTime = 0f;

            while (timer <= temp)
            {

                if (timer <= jumpTime)
                {
                    Vector3 velocity = jumpVel;
                    velocity.y = ySpeed;
                    transform.position += velocity * Time.deltaTime;
                    ySpeed += player.Gravity * Time.deltaTime;

                    if (timer >= jumpTime - transitionTime && start)
                    {
                        animator.CrossFade(hangTypeName, 0.3f);
                        animator.Update(0);
                        animState = animator.GetNextAnimatorStateInfo(0);
                        start = false;
                        targetRot = Quaternion.LookRotation(-point.transform.forward);
                        temp += animState.length; // small delay, should be endTime here
                        animator.speed = endTime * (animState.length) / transitionTime;
                    }
                    else if (timer >= jumpTime - idleTransitionTime && idleStart)
                    {
                        IsHanging = false;
                        animator.CrossFade("FallIdle", 0.6f);
                        idleStart = false;
                    }
                }
                else
                {

                    IsHanging = true;

                    animator.speed = 1;
                    var handPos = climbController.rightHand.boneTransform;

                    if (climbController.rightHand.boneTransform == Vector3.zero)
                        handPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;

                    var offset = right - handPos;
                    //transform.position += offset;
                    transform.position = Vector3.MoveTowards(transform.position, transform.position + offset, 10f * Time.deltaTime);

                    //freeHang Jump
                    comboTime += Time.deltaTime;

                    if (nextJumpPoint != null && animator.GetFloat("freeHang") > 0.5f && climbController.currentPoint.transform.parent.CompareTag("SwingableLedge") && comboTime >= (animState.length - transitionTime) * (0.3f)) // peak Time percentage
                    {
                        JumpData jumpData = new();
                        jumpData.hasSpaceToLand = true;
                        jumpData.footPosition = jumpData.rootPosition = nextJumpPoint.Value;
                        StartCoroutine(DoPredictiveBackJump(jumpData, combo: true));
                        climbController.currentPoint.hasOwner = false;

                        yield break;
                    }

                }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);

                timer += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRot;

            inAirOfPredictiveJump = false;

            ResetRootMotion();
            inPredictiveJump = false;
            InAction = false;

        }

        public IEnumerator DoParkourAction(ParkourAction action)
        {
            TargetMatchParams matchParams = null;

            if (action.EnableTargetMatching)
            {
                matchParams = new TargetMatchParams()
                {
                    pos = action.MatchPos,
                    startTime = action.MatchStartTime,
                    endTime = action.MatchTargetTime,
                    target = action.MatchBodyPart,
                    posWeight = action.MatchPosWeight + new Vector3(0, 0, 1)
                };
            }

            matchPosGlobal = action.MatchPos;

            player.OnStartAction();

            yield return DoAction(action.AnimName, action.RotateToObstacle, action.TargetRotation,
                matchParams, action.PostActionDelay, action.Mirror);
            player.OnEndAction();
        }

        public IEnumerator DoAction(string anim, bool rotate = false,
        Quaternion targetRot = new Quaternion(), TargetMatchParams matchParams = null,
        float postDelay = 0f, bool mirror = false, Action onComplete = null)
        {
            InAction = true;

            if (player.WaitToStartSystem)
                yield return new WaitUntil(() => player.WaitToStartSystem == false);

            EnableRootMotion();
            animator.SetBool("mirrorAction", mirror);

            //animator.CrossFadeInFixedTime(anim, 0.2f

            var crossFadeTime = 0.2f;

            if (matchParams != null) Mathf.Min(crossFadeTime, matchParams.startTime + 0.02f);

            animator.CrossFade(anim, crossFadeTime);

            yield return null;
            //animator.Update(0);

            var animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f;
            while (timer <= animState.length)
            {
                timer += Time.deltaTime;

                if (matchParams != null)
                {
                    MatchTarget(matchParams.pos, matchParams.rot, matchParams.target, new MatchTargetWeightMask(matchParams.posWeight, 0),
                        matchParams.startTime, matchParams.endTime);
                }
                if (rotate)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);

                yield return null;
            }

            yield return new WaitForSeconds(postDelay);

            InAction = false;
            DisableRootMotion();

            onComplete?.Invoke();
        }

        public void HandleBalanceOnNarrowBeam()
        {
            bool leftFootHit, rightFootHit;
            int hitCount = 0;

            Vector3 right = transform.right * 0.3f, forward = transform.forward * 0.3f, up = Vector3.up * 0.2f;
            hitCount += Physics.CheckCapsule(transform.position - right + up, transform.position - right - up, 0.1f, ObstacleLayer) ? 1 : 0;
            hitCount += Physics.CheckCapsule(transform.position + right + up, transform.position + right - up, 0.1f, ObstacleLayer) ? 1 : 0;
            hitCount += (rightFootHit = Physics.CheckCapsule(transform.position + forward + up, transform.position + forward - up, 0.1f, ObstacleLayer)) ? 1 : 0;
            hitCount += (leftFootHit = Physics.CheckCapsule(transform.position - forward + up, transform.position - forward - up, 0.1f, ObstacleLayer)) ? 1 : 0;


            if ((rightFootHit || leftFootHit) && !Physics.Linecast(transform.position + up, transform.position - up, ObstacleLayer)) // for predictive jump cases
                hitCount -= 1;
            var crouchVal = hitCount > 2 ? 0f : 1f;
            animator.SetFloat("idleType", crouchVal, 0.2f, Time.deltaTime);
            if (animator.GetFloat("idleType") > .2f)
            {
                var hasSpace = leftFootHit && rightFootHit;
                animator.SetFloat("crouchType", hasSpace ? 0 : 1, 0.2f, Time.deltaTime);
            }
        }

        Vector3 matchPosGlobal;
        void MatchTarget(Vector3 matchPos, Quaternion rotation, AvatarTarget target, MatchTargetWeightMask weightMask, float startTime, float endTime)
        {
            if (animator.isMatchingTarget || animator.IsInTransition(0)) return;

            //matchPosGlobal = matchPos;
            animator.MatchTarget(matchPos, rotation, target, weightMask, startTime, endTime);
        }

        bool prevRootMotionVal;

        public void EnableRootMotion()
        {
            prevRootMotionVal = player.UseRootMotion;
            player.UseRootMotion = true;
        }

        public void DisableRootMotion()
        {
            prevRootMotionVal = player.UseRootMotion;
            player.UseRootMotion = false;
        }

        public void ResetRootMotion()
        {
            player.UseRootMotion = prevRootMotionVal;
        }
    }
}
