using FS_Core;
using System;
using UnityEngine;
namespace FS_CombatSystem
{
    public class MeleeCombatInputManager : MonoBehaviour
    {
        [Header("Keys")]
        [SerializeField] KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] KeyCode blockKey = KeyCode.Mouse1;
        [SerializeField] KeyCode combatModeKey = KeyCode.F;
        [SerializeField] KeyCode heavyAttackKey = KeyCode.R;
        [SerializeField] KeyCode specialAttackKey = KeyCode.X;
        [SerializeField] KeyCode counterKey = KeyCode.Q;


        [Header("Buttons")]
        [SerializeField] string attackButton;
        [SerializeField] string blockButton;
        [SerializeField] string combatModeButton;
        [SerializeField] string counterButton;
        [SerializeField] string heavyAttackButton;
        [SerializeField] string specialAttackButton;

        public event Action<float, bool, bool, bool, bool> OnAttackPressed;

        public bool Block { get; set; }
        public bool CombatMode { get; set; }

        bool attackDown;
        bool heavyAttackDown;
        bool specialAttackDown;


        public float AttackHoldTime { get; private set; } = 0f;
        public float HeavyAttackHoldTime {get; private set;} = 0f;
        public float SpecialAttackHoldTime {get; private set;} = 0f;

        float chargeTime = 0f;
        bool useAttackInputForCounter = false;
        private void Start()
        {
            chargeTime = MeleeCombatSettings.i.HoldTimeForChargedAttacks;
            useAttackInputForCounter = MeleeCombatSettings.i.SameInputForAttackAndCounter;
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
            if (Time.deltaTime == 0) return;

            //Attack
            HandleAttack();

            //HeavyAttack
            HandleHeavyAttack();

            //Special Attack
            HandleSpecialAttack();

            //Counter
            HandleCounter();

            //Block
            HandleBlock();


            //Combat Mode
            HandleCombatMode();
        }

        void HandleAttack()
        {
#if inputsystem

            if (input.MeleeCombat.Attack.WasPressedThisFrame())
            {
                attackDown = true;
            }
            if (attackDown)
            {
                if (AttackHoldTime >= chargeTime || input.MeleeCombat.Attack.WasReleasedThisFrame())
                {
                    OnAttackPressed?.Invoke(AttackHoldTime, false, useAttackInputForCounter, AttackHoldTime >= chargeTime, false);
                    attackDown = false;
                    AttackHoldTime = 0f;
                }
                AttackHoldTime += Time.deltaTime;
            }
#else


            if (Input.GetKeyDown(attackKey) || IsButtonDown(attackButton))
            {
                attackDown = true;
            }
            if (attackDown)
            {
                if (AttackHoldTime >= chargeTime || Input.GetKeyUp(attackKey) || IsButtonUp(attackButton))
                {
                    OnAttackPressed?.Invoke(AttackHoldTime, false, useAttackInputForCounter, AttackHoldTime >= chargeTime, false);
                    attackDown = false;
                    AttackHoldTime = 0f;
                }
                AttackHoldTime += Time.deltaTime;
            }
#endif
        }

        void HandleHeavyAttack()
        {
#if inputsystem
            if (input.MeleeCombat.HeavyAttack.WasPressedThisFrame())
            {
                heavyAttackDown = true;
            }

            if (heavyAttackDown)
            {
                if (HeavyAttackHoldTime >= chargeTime || input.MeleeCombat.HeavyAttack.WasReleasedThisFrame())
                {
                    OnAttackPressed?.Invoke(HeavyAttackHoldTime, true, false, HeavyAttackHoldTime >= chargeTime, false);
                    heavyAttackDown = false;
                    HeavyAttackHoldTime = 0f;
                }

                HeavyAttackHoldTime += Time.deltaTime;
            }
#else
            if (Input.GetKeyDown(heavyAttackKey) || IsButtonDown(heavyAttackButton))
            {
                heavyAttackDown = true;
            }

            if (heavyAttackDown)
            {
                if (HeavyAttackHoldTime >= chargeTime || Input.GetKeyUp(heavyAttackKey) || IsButtonUp(heavyAttackButton))
                {
                    OnAttackPressed?.Invoke(HeavyAttackHoldTime, true, false, HeavyAttackHoldTime >= chargeTime, false);
                    heavyAttackDown = false;
                    HeavyAttackHoldTime = 0f;
                }

                HeavyAttackHoldTime += Time.deltaTime;
            }
#endif
        }


        void HandleSpecialAttack()
        {
#if inputsystem
            if (input.MeleeCombat.SpecialAttack.WasPressedThisFrame())
            {
                specialAttackDown = true;
            }

            if (specialAttackDown)
            {
                if (SpecialAttackHoldTime >= chargeTime || input.MeleeCombat.SpecialAttack.WasReleasedThisFrame())
                {
                    OnAttackPressed?.Invoke(SpecialAttackHoldTime, false, false, SpecialAttackHoldTime >= chargeTime, true);
                    specialAttackDown = false;
                    SpecialAttackHoldTime = 0f;
                }

                SpecialAttackHoldTime += Time.deltaTime;
            }
#else
            if (Input.GetKeyDown(specialAttackKey) || IsButtonDown(specialAttackButton))
            {
                specialAttackDown = true;
            }

            if (specialAttackDown)
            {
                if (SpecialAttackHoldTime >= chargeTime || Input.GetKeyUp(specialAttackKey) || IsButtonUp(specialAttackButton))
                {
                    OnAttackPressed?.Invoke(SpecialAttackHoldTime, false, false, SpecialAttackHoldTime >= chargeTime, true);
                    specialAttackDown = false;
                    SpecialAttackHoldTime = 0f;
                }

                SpecialAttackHoldTime += Time.deltaTime;
            }
#endif
        }

        void HandleCounter()
        {
            if (!useAttackInputForCounter)
            {
#if inputsystem
                if (input.MeleeCombat.Counter.WasPressedThisFrame())
                {
                    OnAttackPressed?.Invoke(0f, false, true, false, false);
                }
#else
                if(Input.GetKeyDown(counterKey) || IsButtonDown(counterButton))
                {
                    OnAttackPressed?.Invoke(0f, false, true, false, false);
                }
#endif
            }
        }

        void HandleBlock()
        {
#if inputsystem
            Block = input.MeleeCombat.Block.inProgress;
#else
            Block = Input.GetKey(blockKey) || (!string.IsNullOrEmpty(blockButton) && Input.GetButton(blockButton));
#endif
        }

        void HandleCombatMode()
        {
#if inputsystem
            CombatMode = input.MeleeCombat.CombatMode.WasPressedThisFrame();
#else
            CombatMode = Input.GetKeyDown(combatModeKey) || (!string.IsNullOrEmpty(combatModeButton) && Input.GetButtonDown(combatModeButton));
#endif
        }


        public bool IsButtonDown(string buttonName)
        {
            if (!String.IsNullOrEmpty(buttonName))
                return Input.GetButtonDown(buttonName);
            else
                return false;
        }

        public bool IsButtonUp(string buttonName)
        {
            if (!String.IsNullOrEmpty(buttonName))
                return Input.GetButtonUp(buttonName);
            else
                return false;
        }
    }
}