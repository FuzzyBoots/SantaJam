using FS_ThirdPerson;
using System;
using UnityEngine;

namespace FS_ShooterSystem
{
    public class ShooterInputManger : MonoBehaviour
    {
        [Header("Keys")]
        [SerializeField] KeyCode fireKey = KeyCode.Mouse0;
        [SerializeField] KeyCode aimKey = KeyCode.Mouse1;
        [SerializeField] KeyCode scopeKey = KeyCode.Mouse1;
        [SerializeField] KeyCode reloadKey = KeyCode.R;
        [SerializeField] KeyCode throwCancelKey = KeyCode.X;


        [Header("Buttons")]
        [SerializeField] string fireButton;
        [SerializeField] string aimButton;
        [SerializeField] string scopeButton;
        [SerializeField] string reloadButton;
        [SerializeField] string throwCancelButton;



        [Space(10)]
        [SerializeField] bool useDoubleClickForScope = true;


        public bool Fire { get; set; }
        public bool FireDown { get; set; }
        public bool FireUp { get; set; }
        public bool Aim { get; set; }
        public bool AimDown { get; set; }
        public bool AimUp { get; set; }
        public bool Scope { get; set; }
        public bool Reload { get; set; }
        public bool ThrowCancel { get; set; }

        PlayerController playerController;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
        }

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

        private void Update()
        {
            HandleAim();

            HandleFire();

            HandleReload();

            //Cancel Throw
            HandleThrowCancel();
        }

        

        void HandleFire()
        {
#if inputsystem
            Fire = input.Shooter.Fire.inProgress;
            FireDown = input.Shooter.Fire.WasPerformedThisFrame();
            FireUp = input.Shooter.Fire.WasReleasedThisFrame();

            if (playerController.CameraType == FSCameraType.FirstPerson)
            {
                Scope = input.Shooter.Scope.inProgress;
            }
            else if (playerController.CameraType == FSCameraType.ThirdPerson)
            {
                if (CheckForMultiTap(input.Shooter.Scope.WasPressedThisFrame()) || (!useDoubleClickForScope && input.Shooter.Scope.WasPressedThisFrame()))
                    Scope = true;
                else if (input.Shooter.Scope.WasReleasedThisFrame())
                    Scope = false;
            }
#else
            Fire = Input.GetKey(fireKey) || (String.IsNullOrEmpty(fireButton) ? false : Input.GetButton(fireButton));
            FireDown = Input.GetKeyDown(fireKey) || (String.IsNullOrEmpty(fireButton) ? false : Input.GetButtonDown(fireButton));
            FireUp = Input.GetKeyUp(fireKey) || (String.IsNullOrEmpty(fireButton) ? false : Input.GetButtonUp(fireButton));

            if (playerController.CameraType == FSCameraType.FirstPerson)
            {
                Scope = Input.GetKey(scopeKey) || (String.IsNullOrEmpty(scopeButton) ? false : Input.GetButton(scopeButton));
            }
            else if (playerController.CameraType == FSCameraType.ThirdPerson)
            {
                var scopePressed = Input.GetKeyDown(scopeKey) || (String.IsNullOrEmpty(scopeButton) ? false : Input.GetButtonDown(scopeButton));
                var scopeReleased = Input.GetKeyUp(scopeKey) || (String.IsNullOrEmpty(scopeButton) ? false : Input.GetButtonUp(scopeButton));
                if (CheckForMultiTap(scopePressed))
                    Scope = true;
                else if (scopeReleased)
                    Scope = false;
            }
#endif
        }

        public event Action OnAimPressed;
        public event Action OnAimReleased;

        void HandleAim()
        {
#if inputsystem
            bool prevAim = Aim;
            Aim = input.Shooter.Aim.inProgress;
            AimDown = input.Shooter.Aim.WasPerformedThisFrame();
            AimUp = input.Shooter.Aim.WasReleasedThisFrame();
#else
            bool prevAim = Aim;
            Aim = Input.GetKey(aimKey) || (String.IsNullOrEmpty(aimButton) ? false : Input.GetButton(aimButton));
            AimDown = Input.GetKeyDown(aimKey) || (String.IsNullOrEmpty(aimButton) ? false : Input.GetButtonDown(aimButton));
            AimUp = Input.GetKeyUp(aimKey) || (String.IsNullOrEmpty(aimButton) ? false : Input.GetButtonUp(aimButton));
#endif

            if (!prevAim && Aim)
            {
                OnAimPressed?.Invoke();
            }
            if (prevAim && !Aim)
            {
                OnAimReleased?.Invoke();
            }
        }

        void HandleReload()
        {
#if inputsystem
            Reload = input.Shooter.Reload.WasPerformedThisFrame();
#else
            Reload = Input.GetKeyDown(reloadKey) || (String.IsNullOrEmpty(reloadButton) ? false : Input.GetButtonDown(reloadButton));
#endif
        }

        void HandleThrowCancel()
        {
#if inputsystem
            ThrowCancel = input.Shooter.CancelThrow.WasPerformedThisFrame();
#else
            ThrowCancel = Input.GetKeyDown(throwCancelKey) || (String.IsNullOrEmpty(throwCancelButton) ? false : Input.GetButtonDown(throwCancelButton));
#endif
        }

        private int requiredTapCount = 2;     // Number of taps required
        private float maxTimeBetweenTaps = 0.5f;  // Time allowed between taps

        private int currentTapCount = 0;
        private float lastTapTime = 0f;

        public bool CheckForMultiTap(bool inputPressed)
        {
            bool isMultiTapped = false;
            float currentTime = Time.time;

            if (inputPressed)
            {
                if (currentTime - lastTapTime <= maxTimeBetweenTaps)
                    currentTapCount++;
                else
                    currentTapCount = 1;

                lastTapTime = currentTime;

                if (currentTapCount == requiredTapCount)
                {
                    isMultiTapped = true;
                    currentTapCount = 0; // Reset after success
                }
            }

            return isMultiTapped;
        }
    }

}