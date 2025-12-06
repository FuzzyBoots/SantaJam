using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_CombatCore 
{ 
public class CombatInputManger : MonoBehaviour
{

    [SerializeField] KeyCode dodgeKey = KeyCode.LeftAlt;
    [SerializeField] KeyCode rollKey = KeyCode.Space;
    [SerializeField] KeyCode equipKey = KeyCode.Keypad1;
    [SerializeField] KeyCode unEquipKey = KeyCode.Keypad1;


    [SerializeField] string dodgeButton;
    [SerializeField] string rollButton;
    [SerializeField] string equipButton;
    [SerializeField] string unEquipButton;


    public bool Dodge { get; set; }
    public bool Roll { get; set; }
    public bool Equip { get; set; }
    public bool UnEquip { get; set; }


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

        HandleDodge();
        HandleRoll();

        HandleEquip();
        HandleUnEquip();
    }

    void HandleDodge()
    {

#if inputsystem
        Dodge = input.Combat.Dodge.WasPressedThisFrame();
#else
                Dodge = Input.GetKeyDown(dodgeKey) || (!string.IsNullOrEmpty(dodgeButton) && Input.GetButtonDown(dodgeButton));
#endif
    }

    void HandleRoll()
    {
#if inputsystem
        Roll = input.Combat.Roll.WasPressedThisFrame();
#else
            Roll = Input.GetKeyDown(rollKey) || (!string.IsNullOrEmpty(rollButton) && Input.GetButtonDown(rollButton));
#endif
    }
    void HandleEquip()
    {
#if inputsystem
        Equip = input.Combat.Equip.WasPressedThisFrame();
#else
            Equip = Input.GetKeyDown(equipKey) || (!string.IsNullOrEmpty(equipButton) && Input.GetButtonDown(equipButton));
#endif
    }

    void HandleUnEquip()
    {
#if inputsystem
        UnEquip = input.Combat.UnEquip.WasPressedThisFrame();
#else
            UnEquip = Input.GetKeyDown(unEquipKey) || (!string.IsNullOrEmpty(unEquipButton) && Input.GetButtonDown(unEquipButton));
#endif
    }

}

}