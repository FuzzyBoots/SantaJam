using FS_ThirdPerson;
using System;
using UnityEngine;

namespace FS_ThirdPerson
{
    public static partial class AnimatorParameters
    {
        // public static int CoverMode = Animator.StringToHash("CoverMode");
    }
}

namespace FS_CoverSystem
{
    public class CoverController : SystemBase
    {
        // Cached component references
        private EnvironmentScanner environmentScanner;
        private LocomotionICharacter locomotionICharacter;
        private CharacterController characterController;
        private PlayerController playerController;
        private CoverHandler coverHandler;
        private Animator animator;

        public bool enableCover = true;

        public KeyCode coverKey = KeyCode.G; // Key to toggle cover mode
        public string coverButton;

        [SerializeField]
        private float coverCheckDistance = 4f;
        [SerializeField]
        private float playerHeightForCover = 2.6f;
        [SerializeField]
        private bool OnlyCoverInCoverTags;
        // Public properties to store the last cover position and forward direction
        public Vector3 LastCoverPosition { get; set; }
        public Vector3 LastCoverForward { get; set; }
        public bool WasInCover { get; set; }

        public override SystemState State => SystemState.Cover;
        public override float Priority => 4;

        public bool ChaseMode { get; set; }
        public bool CustomMode { get; set; }

#if inputsystem
        FSSystemsInputAction input;
#endif
        #region input system
        private void OnEnable()
        {
#if inputsystem
            input = new FSSystemsInputAction();
            input.Enable();
#endif
        }

        private void OnDisable()
        {
#if inputsystem
            input.Disable();
#endif
        }

        #endregion


        private void Start()
        {
            environmentScanner = GetComponent<EnvironmentScanner>();
            locomotionICharacter = GetComponent<LocomotionICharacter>();
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            coverHandler = GetComponent<CoverHandler>();
            animator = GetComponent<Animator>();

            LastCoverPosition = transform.position;

            // Subscribe to the state exit event
            OnStateExited += HandleStateExit;
        }

        private void HandleStateExit()
        {
            coverHandler.GoOutOfCover();
            LastCoverPosition = transform.position;
        }

        public override void HandleUpdate()
        {
            if (!enableCover) return;
#if inputsystem
            var coverKeyPressed = input.Combat.Cover.WasPerformedThisFrame();
#else
            var coverKeyPressed = Input.GetKeyDown(coverKey) || (String.IsNullOrEmpty(coverButton) ? false : Input.GetButtonDown(coverButton));
#endif

            if (!coverHandler.InCover && !ChaseMode)
            {
                if (coverKeyPressed)
                {
                    StartChaseToCover();
                }
            }
            else
            {
                if (coverKeyPressed && !ChaseMode)
                {
                    StopCover();
                    return;
                }

                // Try to align to the cover if not yet in proper cover mode
                if (ChaseMode)
                {

                    Vector3 coverSpot = CheckCover();
                    coverSpot.y = transform.position.y;
                    if ((coverSpot - transform.position).magnitude > 0.5f)
                    {
                        locomotionICharacter.ReachDestination(coverSpot, 4f);
                        return;
                    }
                    StartCover();
                    ChaseMode = false;
                }
                else
                {
                    locomotionICharacter.SetCurrentVelocity(0, Vector3.zero);
                }

                // Update the equipped system if available
                if(!animator.IsInTransition(0) && !ChaseMode)
                    playerController.CurrentEquippedSystem?.HandleUpdate();

                // Custom mode bypasses default cover movement updates
                //if (CustomMode)
                //    return; 

                UpdateCoverAnimationAndMovement();
            }
        }
        /// <summary>
        /// Updates the cover animation and adjusts movement while in cover mode.
        /// </summary>
        private void UpdateCoverAnimationAndMovement()
        {
            // Define the starting position for the sphere cast
            Vector3 startPos = transform.position - transform.forward * 0.2f;
            startPos.y += 0.8f;

            int castIterations;
            for (castIterations = 0; castIterations < 10; castIterations++)
            {
                //GizmosExtend.drawSphereCast(startPos, 0.1f, transform.forward, 1f, Color.blue);
                if (Physics.SphereCast(startPos, 0.1f, transform.forward, out RaycastHit sphereHit, 1f, environmentScanner.ObstacleLayer))
                {
                    startPos.y += 0.1f;
                }
                else
                {
                    break;
                }
            }

            // Determine cover type value based on the number of iterations
            float coverTypeValue = (1f - (castIterations / 10f)) * playerHeightForCover;
            locomotionICharacter.Animator.SetFloat("CoverType", coverTypeValue, 0.2f, Time.deltaTime);

            startPos = transform.position;
            startPos.y += 0.8f;
            RaycastHit coverHit;
            if (!Physics.SphereCast(startPos, 0.1f, transform.forward, out coverHit, coverCheckDistance, environmentScanner.ObstacleLayer))
            {
                coverHit.normal = Vector3.zero;
            }

            // Flatten the hit normal to the horizontal plane
            Vector3 adjustedNormal = new Vector3(coverHit.normal.x, 0, coverHit.normal.z);
            Quaternion targetRotation;
            if (adjustedNormal.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.LookRotation(-adjustedNormal);
            }
            else
            {
                targetRotation = transform.rotation; // fallback to current rotation if normal is zero
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * 100f);

            // Calculate the movement direction along the cover
            float movDir = Vector3.Dot(transform.right, locomotionICharacter.MoveDir);
            Vector3 sideCastOrigin = startPos + (movDir * transform.right).normalized * 0.5f;
            //GizmosExtend.drawSphereCast(sideCastOrigin, 0.1f, transform.forward, 1f, Color.blue);

            if (Physics.SphereCast(sideCastOrigin, 0.1f, -adjustedNormal, out RaycastHit sideHit, 1f, environmentScanner.ObstacleLayer))
            {
                locomotionICharacter.Animator.SetFloat("CoverDir", movDir, 0.25f, Time.deltaTime);
                characterController.Move(locomotionICharacter.Animator.deltaPosition + (-adjustedNormal * Time.deltaTime) + Vector3.down);
            }
            else
            {
                locomotionICharacter.Animator.SetFloat("CoverDir", 0f, 0.1f, Time.deltaTime);
                characterController.Move(-adjustedNormal * Time.deltaTime + Vector3.down);
            }
        }
        public Vector3 StartChaseToCover()
        {
            Vector3 coverSpot = CheckCover();
            if (coverSpot != Vector3.zero)
            {
                locomotionICharacter.OnStartSystem(this);
                ChaseMode = true;
            }
            return coverSpot;
        }
        /// <summary>
        /// Checks for a viable cover position ahead of the character.
        /// </summary>
        public Vector3 CheckCover(float checkDistance = 0)
        {
            if(checkDistance == 0)
                checkDistance = coverCheckDistance;
            Vector3 startPos = transform.position - transform.forward * 0.2f;
            startPos.y += 0.3f;
            RaycastHit hit;

            for (int i = 0; i < 10; i++)
            {
                //GizmosExtend.drawSphereCast(startPos, 0.2f, transform.forward, coverCheckDistance, Color.blue);
                if (Physics.SphereCast(startPos, 0.2f, transform.forward, out hit, checkDistance, environmentScanner.ObstacleLayer))
                {
                    bool validCover = true;
                    if (OnlyCoverInCoverTags)
                    {
                        validCover = hit.collider.tag == "Cover";
                    }
                    if (validCover)
                    {
                        startPos.y += 0.3f;
                        if (i > 2)
                        {
                            return hit.point;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            return Vector3.zero;
        }

        bool previousCameraAlignment;
        public void StartCover()
        {
            locomotionICharacter.OnStartSystem(this);
            coverHandler.GoToCover();
            //previousCameraAlignment = playerController.AlignTargetWithCameraForward;
            //playerController.AlignTargetWithCameraForward = false;
        }

        public void StopCover()
        {
            coverHandler.GoOutOfCover();
            locomotionICharacter.OnEndSystem(this);
            //playerController.AlignTargetWithCameraForward = previousCameraAlignment;
        }

        public override void HandleOnAnimatorMove(Animator animator)
        {
            // Implement custom animator movement handling if necessary.
        }
    }

    /// <summary>
    /// Extension method to modify vector components.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Returns a copy of the vector with the y component replaced.
        /// </summary>
        public static Vector3 SetY(this Vector3 v, float newY)
        {
            return new Vector3(v.x, newY, v.z);
        }
    }
}
