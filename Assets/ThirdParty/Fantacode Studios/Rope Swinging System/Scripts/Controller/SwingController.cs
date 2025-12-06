using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


namespace FS_ThirdPerson
{
    public static partial class AnimatorParameters
    {
        public static int SwingLeftHandIk = Animator.StringToHash("SwingLeftHandIk");
        public static int SwingRightHandIk = Animator.StringToHash("SwingRightHandIk");
        public static int SwingIkPriority = Animator.StringToHash("SwingIkPriority");
    }

    public static partial class AnimationNames
    {
        public static string SwingActions = "Swing Actions";
        public static string SwingLand = "Swing Land";
        public static string SwingClimbUp = "Swing Climb Up";
        public static string SwingClimbDown = "Swing Climb Down";
    }
}

namespace FS_SwingSystem
{
    public class SwingController : EquippableSystemBase
    {
        bool enableSwing = false;

        [Tooltip("If true, the character will be able to swing event without equipping the rope")]
        public bool swingWithoutEquip = false;

        [Tooltip("If true, the rope will be unequipped after the swing action is completed.")]
        public bool unEquipAfterSwing = false;

        [Tooltip("The minimum distance required for the swing")]
        public float minDistance = 2f;

        [Tooltip("The animation clip used when holding the rope.")]
        public AnimationClip hookHoldingClip;



        // Rope settings
        [Tooltip("Transform for holding the rope with the right hand.")]
        public Transform ropeHoldTransformRight;

        [Tooltip("Transform for holding the rope with the left hand.")]
        public Transform ropeHoldTransformLeft;

        [Tooltip("Transform for attaching the rope to the body.")]
        public Transform ropeAttachTransformInBody;

        //[Tooltip("Hook Object")]
        //public HookItem hookObject;


        [Tooltip("The radius of the rope.")]
        public float ropeRadius = .025f;

        [Tooltip("Material used for the rope's appearance.")]
        public Material ropeMaterial;

        [Tooltip("Resolution of the rope's segments.")]
        public int ropeResolution = 50;

        [Tooltip("Maximum length of the rope.")]
        public float ropeLength = 10f;

        [Tooltip("Width of the rope.")]
        public float ropeWidth = 0.1f;

        [Tooltip("Amount of dampening applied to the rope's movement.")]
        public float dampening = 0.5f;

        [Tooltip("Height of the rope throw.")]
        public float throwHeight = 1f;

        [Tooltip("Duration of the rope throw.")]
        public float throwDuration = 0.5f;

        // Swing settings
        [Tooltip("The force applied to the player during swinging.")]
        public float swingForce = 4f;

        [Tooltip("The speed of rotation while swinging.")]
        public float swingRotationSpeed = 100f;

        [Tooltip("The speed at which the character climbs up the rope.")]
        public float climbSpeed = 3;

        [Tooltip("Multiplier for the forward force applied upon landing from a swing.")]
        public float forwardLandForceMultiplier = 1f;

        [Tooltip("Multiplier for the upward force applied upon landing from a swing.")]
        public float upwardLandForceMultiplier = 2f;

        [Tooltip("Amount of damping applied while swinging.")]
        public float damping = 0.5f;

        [Tooltip("The gravity force applied during swinging.")]
        public float gravity = -20f;


        [Tooltip("Friction applied during collisions while swinging.")]
        public float collisionFriction = .2f;

        // Crosshair settings
        [Tooltip("Prefab used for the crosshair when aiming the swing hook.")]
        public GameObject crosshairPrefab;

        [Tooltip("Size of the crosshair.")]
        public float crosshairSize = .3f;

        // Camera shake settings
        [Tooltip("Amount of camera shake when the swing start")]
        public float cameraShakeAmount = .2f;

        [Tooltip("Duration of camera shake when the swing start")]
        public float cameraShakeDuration = .3f;

        float totalRopeLength = 10f;

        // This variable will store the reference of the swing hook item, even when it's not equipped
        EquippableItem availableSwingHook;

        // Events
        public UnityEvent RopeReleased;
        public UnityEvent RopeHooked;
        public UnityEvent SwingStarted;
        public UnityEvent ExitedFromSwing;

        public bool debug;

        Animator animator;
        LocomotionICharacter player;
        EnvironmentScanner environmentScanner;
        PlayerController playerController;
        ItemEquipper itemEquipper;
        LocomotionInputManager locomotionInput;
        SwingRope rope;
        SwingData SwingData;
        AnimGraph animGraph;
        GameObject crosshairObj;
        Crosshair crosshair;

        Vector3 pivotPoint;
        float rotateSpeed = 15;


        Vector3 velocity;
        Vector3 prevVelocity;
        Vector3 maxHeightPoint;
        Vector3 swingDirection;


        float maxSwingAngle = 60f;

        private Vector3 ropeVector;
        private CharacterController characterController;


        public bool InSwing { get; private set; }
        public bool InAction { get; private set; }
        public bool IsFalling { get; private set; }

        //bool isHoldingClipPlaying;

        public override SystemState State { get; } = SystemState.Swing;
        public override List<Type> EquippableItems => new List<Type>() { typeof(SwingHookItem) };

        //public override List<SystemState> ExecutionStates => new List<SystemState>() { SystemState.Locomotion, SystemState.Swing, SystemState.Parkour }; 
        bool isClimbing;


        public SwingHookItem currentHookItem
        {
            get
            {
                return (itemEquipper?.EquippedItem is SwingHookItem shooterWeaponData)
                    ? shooterWeaponData
                    : null;
            }
        }
        public SwingHookObject CurrentHookRight => itemEquipper?.EquippedItemRight as SwingHookObject;
        public SwingHookObject CurrentHookLeft => itemEquipper?.EquippedItemLeft as SwingHookObject;
        public SwingHookObject CurrentHookItem => itemEquipper.EquippedItemObject as SwingHookObject;



        private void Start()
        {
            totalRopeLength = ropeLength;
            player = GetComponent<LocomotionICharacter>();
            animator = player.Animator;
            environmentScanner = GetComponent<EnvironmentScanner>();
            locomotionInput = GetComponent<LocomotionInputManager>();
            characterController = GetComponent<CharacterController>();
            itemEquipper = GetComponent<ItemEquipper>();
            animGraph = GetComponent<AnimGraph>();
            playerController = GetComponent<PlayerController>();
            if (ropeHoldTransformRight == null)
                ropeHoldTransformRight = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (ropeHoldTransformLeft == null)
                ropeHoldTransformLeft = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            if (ropeAttachTransformInBody == null)
                ropeAttachTransformInBody = animator.GetBoneTransform(HumanBodyBones.Hips);

            

            itemEquipper.OnEquip += EquipItem;
            itemEquipper.OnUnEquip += UnEquipItem;

            SetCrosshair();

            availableSwingHook = itemEquipper.equippableItems.FirstOrDefault(i => i is SwingHookItem);
            enableSwing = availableSwingHook != null;
        }


        private void LateUpdate()
        {
            if(rope != null)
                rope.RopeUpdate();
        }

        public override void HandleUpdate()
        {
            if (Time.timeScale == 0) return;

            if (InSwing)
            {
                ControlVelecity();
                UpdateSwingPhysics();
                HandleCollisions();
            }
        }

        private void Update()
        {
            HandleSwingInputs();

            if (playerController.CurrentSystemState == SystemState.Locomotion || playerController.IsInAir)
            {
                if ((swingWithoutEquip && availableSwingHook != null) || currentHookItem != null)
                {

                    if (enableSwing && !InAction)
                    {
                        SwingHandler();
                    }
                }
            }
        }

        void SwingHandler()
        {
            crosshair.size = crosshairSize;
            SwingData = environmentScanner.GetSwingLedgeData(totalRopeLength - 1, minDistance, ropeHoldTransformRight, true);
            if (SwingData.hasLedge)
            {
                var dir = (SwingData.hookPosition - playerController.cameraGameObject.transform.position).normalized;
                var offset = dir * crosshair.GetMeshBoundsSize();
                crosshairObj.transform.position = SwingData.hookPosition - offset;
                crosshairObj.SetActive(true);
            }
            else
                crosshairObj.SetActive(false);


            if (HookInputHolding && SwingData.hasLedge)
            {
                StartCoroutine(ControlSwingAction());
            }
        }

        IEnumerator ControlSwingAction()
        {
            bool equippedForAction = false;

            if (currentHookItem == null)
            {
                if (availableSwingHook == null) yield break;

                itemEquipper.EquipItem(availableSwingHook);
                yield return new WaitUntil(() => !itemEquipper.IsChangingItem);

                equippedForAction = true;
            }

            if (InAction || currentHookItem == null) yield break;
            InAction = true;
            var currentFocusedScript = playerController.FocusedScript;
            playerController.PreventRotation = true;
            itemEquipper.PreventItemSwitching = true;
            itemEquipper.PreventItemUnEquip = true;
            yield return SetRotation(SwingData.forwardDirection, rotateSpeed);
            crosshairObj.SetActive(false);
            RopeReleased?.Invoke();

            // Play rope throwing animation
            animGraph.Crossfade(currentHookItem.ropeThrowingClip, currentHookItem.ropeThrowingClip, mask: Mask.Arm);
            yield return new WaitForSeconds(.3f);

            pivotPoint = SwingData.hookPosition;

            // Start rope throwing
            rope.StartThrow(ropeHoldTransformRight, pivotPoint);

            // Wait for the rope to reach the hook point.
            while (!rope.HasReachedTarget())
            {
                var hp = SwingData.hookPosition;
                hp.y = transform.position.y;
                var direction = (hp - transform.position).normalized;
                var dir = Quaternion.LookRotation(direction);
                transform.rotation = dir;

                if (playerController.FocusedScript != null && currentFocusedScript != playerController.FocusedScript && playerController.FocusedScript != this)
                {
                    RetractRope();
                    if (unEquipAfterSwing && equippedForAction)
                        itemEquipper.UnEquipItem();
                    yield break;
                }
                yield return null;
            }
            rope.SetRopeState(RopeState.Normal);

            // Rope hooked
            RopeHooked?.Invoke();

            animGraph.StopLoopingAnimations(false);


            playerController.PreventRotation = false;
            playerController.PreventFallingFromLedge = false;

            float currLengthToHook = SwingData.distance;
            float currLengthToHookWhileIsGrounded = SwingData.distance;
            var prevPos = transform.position;


            var initialVelocity = Vector3.zero;


            itemEquipper.PreventItemUnEquip = true;

            while (true)
            {
                // The initial velocity to apply at the start of the swing.
                initialVelocity = transform.position - prevPos;

                // Continuously updating currLengthToHook as the player moves.
                currLengthToHook = Vector3.Distance(ropeHoldTransformRight.position, pivotPoint);

                if (CheckGround())
                {
                    currLengthToHookWhileIsGrounded = currLengthToHook;

                    // Check if the player is moving away from the hook point by a distance greater than the total rope length.
                    // If so, the rope will retract.
                    if (currLengthToHook < minDistance || currLengthToHook >= rope.totalRopeLength || HookReleaseDown)
                    {
                        //Retract rope
                        RetractRope();
                        yield break;
                    }
                }
                else
                {
                       if (currLengthToHook > currLengthToHookWhileIsGrounded + .1f)
                        break;
                }

                prevPos = transform.position;
                yield return null;
            }


            playerController.PreventFallingFromLedge = true;
            ropeLength = currLengthToHook + .5f;
            rope.SetHookRopeAsStraight();
            rope.SetRopeState(RopeState.Swinging);
            initialVelocity.y = 0;

            //Apply initial velocity
            velocity = initialVelocity.normalized * swingForce;


            // Pause all systems and focus on the swing.
            player.OnStartSystem(this);
            playerController.IsInAir = false;

            // Enable hand holding IK for swinging
            handIk = true;

            // Applying camera shake
            playerController.OnStartCameraShake?.Invoke(cameraShakeAmount, cameraShakeDuration);

            // Play swinging animation blend tree
            animator.CrossFadeInFixedTime(AnimationNames.SwingActions, 0.2f);
            InitializeSwingOriginPosition(ropeLength);
            InSwing = true;

            SwingStarted?.Invoke();

            bool isSwingingAtLowVelocity = false;
            bool isFalling = false;
            // Wait until the player presses the jump key or the character is grounded
            while (!locomotionInput.JumpKeyDown && !CheckGround())
            {
                var curVelocityAngle = Vector3.Angle(Vector3.down, (maxHeightPoint - pivotPoint).normalized);
                var curAngle = Vector3.Angle(Vector3.down, ropeVector);
                isFalling = prevPos.y > transform.position.y;
                isSwingingAtLowVelocity = curAngle < 15 && curVelocityAngle < 15;
                if (isSwingingAtLowVelocity)
                    HandleClimbing();
                prevPos = transform.position;
                yield return null;
            }
            isClimbing = false;
            ExitFromSwing();
            ExitedFromSwing?.Invoke();

            // Disable hand holding IK
            handIk = false;

            // Applying camera shake
            playerController.OnStartCameraShake?.Invoke(cameraShakeAmount, cameraShakeDuration);

            // If the player is grounded, crossfade to the "Landing" animation and exit the swing.
            // Otherwise, calculate the landing momentum and perform the landing with that momentum.
            if (CheckGround())
            {
                StartCoroutine(CrossFadeAsync(AnimationNames.LandAndStepForward, .2f, false));
                player.SetCurrentVelocity(0, Vector3.zero);
            }
            else
            {
                HandleLandingMomentum(!isSwingingAtLowVelocity && !isFalling);
            }
            
            yield return new WaitForSeconds(.2f);
            if (unEquipAfterSwing && equippedForAction)
                itemEquipper.UnEquipItem();
        }

        bool CheckGround()
        {
            return player.CheckIsGrounded();
        }

        public void ExitFromSwing()
        {
            rope.SetRopeState(RopeState.Normal);
            rope.ResetRope();
            InSwing = false;
            playerController.PreventRotation = false;
            animGraph.PlayLoopingAnimation(hookHoldingClip, mask: Mask.RightHand, isActAsAnimatorOutput: true);
            crosshairObj.SetActive(false);
            InAction = false;
            player.OnEndSystem(this);
            itemEquipper.PreventItemUnEquip = false;
            itemEquipper.PreventItemSwitching = false;
        }

        void RetractRope()
        {
            ExitFromSwing();
            enableSwing = false;
            StartCoroutine(AsyncUtil.RunAfterDelay(.5f, () => { enableSwing = true; }));
        }

        IEnumerator SetRotation(Vector3 lookDir, float rotateSpeed)
        {
            var dir = Quaternion.LookRotation(lookDir);
            float rotation = 0;
            var angle = Quaternion.Angle(transform.rotation, dir);
            while (angle > .1f && angle > rotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, dir, rotateSpeed * Time.deltaTime * 50);
                yield return null;
                angle = Quaternion.Angle(transform.rotation, dir);
                rotation += rotateSpeed * Time.deltaTime * 50;
            }
            transform.rotation = dir;
        }

        IEnumerator CrossFadeAsync(string anim, float crossFadeTime = .2f, bool enableRootmotion = false, Action onComplete = null)
        {
            if (enableRootmotion)
                EnableRootMotion();
            animator.CrossFadeInFixedTime(anim, crossFadeTime);
            yield return null;
            //while (animator.IsInTransition(0))
            //{
            //    yield return null;
            //}
            var animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f;

            while (timer <= animState.length)
            {
                timer += Time.deltaTime * animator.speed;
                yield return null;
            }
            if (enableRootmotion)
                ResetRootMotion();
            onComplete?.Invoke();
        }

        void SetCrosshair()
        {
            crosshairObj = Instantiate(crosshairPrefab);
            crosshairObj.name = crosshairPrefab.name;
            crosshair = crosshairObj.AddComponent<Crosshair>();
            crosshairObj.transform.parent = this.transform.parent;
            crosshairObj.transform.localScale = Vector3.zero;
            crosshairObj.SetActive(false);
        }

        #region Landing

        void HandleLandingMomentum(bool playLandAnimation = true)
        {
            // Get the current direction of movement, excluding the y value.
            var currDir = velocity.normalized;
            currDir.y = 0;

            InSwing = false;

            // Calculate the origin point of the swing by adjusting the pivotPoint down by the rope's length.
            var originPoint = (pivotPoint - Vector3.up * rope.hookRopeLength);
            var currentPlayerPos = transform.position;
            currentPlayerPos.y = originPoint.y; 

            // Set the maximum height of the swing and adjust it to match the originPoint's y level.
            var maxHeight = maxHeightPoint;
            maxHeight.y = originPoint.y;

            // Calculate the distance between the player's current position and the max height.
            var dist = Vector3.Distance(maxHeight, currentPlayerPos);

            // Determine the target landing position based on the current swing velocity
            var targetPosition = transform.position + currDir.normalized * dist * forwardLandForceMultiplier;
            targetPosition.y = originPoint.y; 

            // Calculate the height difference for calculate landing velocity
            var height = rope.currentRopeHoldPointTransform.position.y - originPoint.y;

            // Flag to determine whether to play the landing animation.
            


            // If the player is falling, prevent the upward landing jump force
            if (velocity.y < 0 || !playLandAnimation)
            {
                height = 0;
                //playLandAnimation = false;
            }

            // Smoothly rotate the player towards the landing direction.
            StartCoroutine(SetRotation(currDir, 5));

            // If the landing animation should play, crossfade to the land animation and exit from the swing.
            if (playLandAnimation)
            {
                StartCoroutine(CrossFadeAsync(AnimationNames.SwingLand, .2f, false));
                player.SetCurrentVelocity(velocity.y * upwardLandForceMultiplier, velocity * forwardLandForceMultiplier);
            }
            else
            {
                // Otherwise, crossfade to the falling animation and exit from the swing.
                StartCoroutine(CrossFadeAsync(AnimationNames.FallTree, .2f, false));
                player.SetCurrentVelocity(velocity.y * upwardLandForceMultiplier * 0.5f, velocity * forwardLandForceMultiplier * 0.5f);
            }

        }

        #endregion

        #region Swinging Controller

        void InitializeSwingOriginPosition(float length)
        {
            ropeVector = Vector3.down * length;
        }

        void ControlVelecity()
        {
            float inputX = RopeClimbModifierHolding ? 0: locomotionInput.DirectionInput.x;
            float inputY = RopeClimbModifierHolding ? 0 : locomotionInput.DirectionInput.y;


            var tarRot = playerController.cameraGameObject.transform.forward;
            tarRot.y = 0;
            if (!IsFalling)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(tarRot), Time.deltaTime * swingRotationSpeed);
            Vector3 cameraForward = Vector3.ProjectOnPlane(playerController.cameraGameObject.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(playerController.cameraGameObject.transform.right, Vector3.up).normalized;
            swingDirection = Vector3.MoveTowards(swingDirection, (cameraRight * inputX + cameraForward * inputY).normalized, Time.deltaTime);

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float dotProduct = Vector3.Dot(swingDirection, horizontalVelocity.normalized);

            float currentAngle = Vector3.Angle(Vector3.down, ropeVector);
            if (currentAngle < maxSwingAngle && dotProduct >= 0 || horizontalVelocity.magnitude < 0.1f)
            {
                velocity += swingDirection * swingForce * Time.deltaTime;
            }

        }

        void UpdateSwingPhysics()
        {
            velocity += Vector3.up * gravity * Time.deltaTime;
            Vector3 newPosition = transform.position + velocity * Time.deltaTime;
            Vector3 newRopeVector = newPosition - pivotPoint;

            newRopeVector = newRopeVector.normalized * ropeLength;
            newPosition = pivotPoint + newRopeVector;

            velocity = (newPosition - transform.position) / Time.deltaTime;

            float currentAngle = Vector3.Angle(Vector3.down, newRopeVector);


            if ((velocity.y < 0 && swingDirection == Vector3.zero) || (currentAngle > maxSwingAngle * .7f))
            {
                velocity *= (1 - Time.deltaTime * damping);
            }


            Vector3 newPos = transform.position + velocity * Time.deltaTime;
            if (prevVelocity.y > 0 && velocity.y < 0)
            {
                maxHeightPoint = newPos;
            }


            characterController.Move(velocity * Time.deltaTime);
            prevVelocity = velocity;

            ropeVector = newRopeVector;



            var maxSwingLength = Mathf.Clamp(rope.hookRopeLength * ((maxSwingAngle + gravity) * Mathf.Deg2Rad), 0.1f, 3);

            var currentLen = Vector3.Distance(transform.position, pivotPoint + Vector3.down * rope.hookRopeLength);

            var percentage = currentLen / maxSwingLength * .5f;

            var dir = (transform.position - pivotPoint + Vector3.down * rope.hookRopeLength).normalized;
            var v = transform.InverseTransformDirection(dir).normalized * percentage;


            animator.SetFloat("x", v.x, .1f, Time.deltaTime);
            animator.SetFloat("y", v.z, .1f, Time.deltaTime);

        }

        void HandleCollisions()
        {
            if (Physics.SphereCast(animator.GetBoneTransform(HumanBodyBones.Hips).position, characterController.radius, velocity.normalized, out RaycastHit hit, characterController.radius, environmentScanner.ObstacleLayer))
            {
                Vector3 reflection = Vector3.Reflect(velocity.normalized, hit.normal);
                reflection += Vector3.up * 0.5f;
                velocity = reflection.normalized * velocity.magnitude * collisionFriction;
                Vector3 pushDirection = (transform.position - hit.point).normalized;
                characterController.Move(pushDirection * velocity.magnitude * Time.deltaTime);
            }

            //GizmosExtend.drawSphereCast(animator.GetBoneTransform(HumanBodyBones.Hips).position, characterController.radius, velocity.normalized, characterController.radius, Color.red);
        }

        void HandleClimbing()
        {
            float climbMovement = 0;
            if (RopeClimbModifierHolding && !isClimbing)
            {
                var climbInputMultiplier = locomotionInput.DirectionInput.y;
                if (climbInputMultiplier > 0 && ropeLength > minDistance + 1.5f)
                    StartClimb(AnimationNames.SwingClimbUp);
                else if (climbInputMultiplier < 0 && ropeLength < totalRopeLength - .5f)
                    StartClimb(AnimationNames.SwingClimbDown);
            }

            if (isClimbing)
            {
                climbMovement = (rope.hookRopeLength > minDistance && rope.hookRopeLength < totalRopeLength) ? climbSpeed * animator.deltaPosition.y : 0;
            }
            ropeLength = Mathf.Clamp(ropeLength - climbMovement, minDistance, totalRopeLength);
        }

        void StartClimb(string animation)
        {
            rope.SetRopeState(RopeState.Climbing);
            isClimbing = true;
            handIk = false;
            StartCoroutine(CrossFadeAsync(animation, onComplete: () =>
            {
                if (isClimbing)
                {
                    isClimbing = false;
                    handIk = true;
                    rope.SetRopeState(RopeState.Swinging);
                }
            }));
        }

        #endregion

        #region rootmotion

        bool prevRootMotionVal;

        public void EnableRootMotion()
        {
            prevRootMotionVal = player.UseRootMotion;
            player.UseRootMotion = true;
        }
        public void ResetRootMotion()
        {
            player.UseRootMotion = prevRootMotionVal;
        }

        public override void HandleOnAnimatorMove(Animator animator)
        {
            transform.rotation *= animator.deltaRotation;
            characterController.Move(animator.deltaPosition);
        }

        #endregion

        #region IK

        bool handIk;

        private void OnAnimatorIK(int layerIndex)
        {
            if (handIk)
            {
                var dirToHookFromLefthand = (SwingData.hookPosition - ropeHoldTransformLeft.position).normalized;
                var offset = (ropeHoldTransformRight.position - animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                animator.SetIKPosition(AvatarIKGoal.RightHand, ropeHoldTransformLeft.transform.position + dirToHookFromLefthand * .25f - offset);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            }

            if (isClimbing)
            {
                var rightHandIkWeight = animator.GetFloat(AnimatorParameters.SwingRightHandIk);
                var leftHandIkWeight = animator.GetFloat(AnimatorParameters.SwingLeftHandIk);
                var swingIkPriority = animator.GetFloat(AnimatorParameters.SwingIkPriority);


                // If the priority is zero, the left hand is currently holding the rope, meaning the other hand is attempting to reach for a higher point on the rope.
                rope.currentRopeHoldPointTransform = swingIkPriority == 0 ? ropeHoldTransformLeft : ropeHoldTransformRight;


                var dirToHookFromLefthand = (SwingData.hookPosition - ropeHoldTransformLeft.position).normalized;
                var offset = (ropeHoldTransformRight.position - animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                animator.SetIKPosition(AvatarIKGoal.RightHand, ropeHoldTransformLeft.transform.position + dirToHookFromLefthand * .25f - offset);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIkWeight);

                var dirToHookFromRighthand = (SwingData.hookPosition - ropeHoldTransformRight.position).normalized;
                offset = (ropeHoldTransformLeft.position - animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, ropeHoldTransformRight.transform.position + dirToHookFromRighthand * .25f - offset);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIkWeight);
            }
        }

        #endregion

        #region input manager

#if inputsystem
        FSSystemsInputAction input;

        private void OnEnable()
        {
            input = new FSSystemsInputAction();
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }
#endif

        [Tooltip("Key to throw the rope hook.")]
        public KeyCode hookInput = KeyCode.F;

        [Tooltip("Key to release the rope hook.")]
        public KeyCode hookReleaseInput = KeyCode.F;

        [Tooltip("Hold this key to climb up or down the rope.")]
        public KeyCode ropeClimbModifier = KeyCode.LeftShift;

        

        [Tooltip("The button to throw the rope hook.")]
        public string hookInputButton;

        [Tooltip("The button to release the rope hook.")]
        public string hookReleaseInputButton;

        [Tooltip("Hold this button to climb up or down the rope.")]
        public string ropeClimbModifierButton;


        public bool HookInputHolding { get; private set; }
        public bool HookReleaseDown { get; private set; }
        public bool RopeClimbModifierHolding { get; private set; }


        void HandleSwingInputs()
        {

#if inputsystem
            HookInputHolding = input.Swing.Hook.inProgress;
            HookReleaseDown = input.Swing.HookRelease.WasPerformedThisFrame();
            RopeClimbModifierHolding = input.Swing.ClimbModifier.inProgress;
#else
            HookInputHolding = Input.GetKey(hookInput) || (!string.IsNullOrEmpty(hookInputButton) && Input.GetButton(hookInputButton));
            HookReleaseDown = Input.GetKeyDown(hookReleaseInput) || (!string.IsNullOrEmpty(hookReleaseInputButton) && Input.GetButtonDown(hookReleaseInputButton));
            RopeClimbModifierHolding = Input.GetKey(ropeClimbModifier) || (!string.IsNullOrEmpty(ropeClimbModifierButton) && Input.GetButton(ropeClimbModifierButton));
#endif
        }

        #endregion

        #region Equip And UnEquip

        public void EquipItem(EquippableItem equippableItem)
        {
            if (equippableItem is SwingHookItem)
            {
                rope = new SwingRope(ropeRadius, ropeWidth, ropeMaterial, throwDuration, ropeLength, dampening, ropeResolution, throwHeight, this.transform, ropeAttachTransformInBody, ropeHoldTransformLeft, ropeHoldTransformRight, CurrentHookItem.ropeHookPoint, CurrentHookItem.transform);
                //SetCrosshair();
                enableSwing = true;
                //player.OnFocusSystem(this);

                availableSwingHook = equippableItem as SwingHookItem;
            }
        }
        public void UnEquipItem()
        {
            if (currentHookItem is SwingHookItem)
            {
                if (rope != null)
                {
                    rope.DeleteRope();
                    rope = null;
                }
                //Destroy(crosshairObj);
                //player.OnUnFocusSystem(this);

                availableSwingHook = itemEquipper.equippableItems.FirstOrDefault(i => i is SwingHookItem);
                enableSwing = availableSwingHook != null;
                crosshairObj.SetActive(false);
            }
        }


        #endregion
    }
}