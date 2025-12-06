 using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace FS_ShooterSystem
{
    [DefaultExecutionOrder(1)]
    public class BowHelper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Transform representing the default position where the bowstring is held when not drawn")]
        Transform holdTransform;

        [SerializeField]
        [Tooltip("Line renderer component representing the top half of the bowstring")]
        LineRenderer topLine;

        [SerializeField]
        [Tooltip("Line renderer component representing the bottom half of the bowstring")]
        LineRenderer bottomLine;

        [SerializeField]
        [Tooltip("Local offset from the right hand position where the string should be held when drawn")]
        Vector3 stringHoldingOffset;

        [SerializeField]
        [Tooltip("Local offset from the right hand position where the arrow should be positioned")]
        Vector3 arrowHoldingOffset;

        [SerializeField]
        [Tooltip("Rotation offset applied to the arrow during the reload animation")]
        Vector3 arrowHoldingRotationOffsetDuringReload = new Vector3(110, -50, 0);

        [SerializeField]
        Transform arrowForwardReference;

        float stringSmoothing = 30f;

        ShooterWeaponObject bowWeapon;
        Animator animator;

        Transform rightHandBone;
        Vector3 currentStringPosition;

        private void OnEnable()
        {
            bowWeapon = GetComponent<ShooterWeaponObject>();
            if (bowWeapon != null)
            {
                bowWeapon.OnEquip += () =>
                {
                    animator = bowWeapon.ParentShooterFighter.gameObject.GetComponent<Animator>();
                    rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    bowWeapon.OnSetAmmo += () =>
                    {
                        bowWeapon.CurrentLoadedAmmo.transform.parent.rotation = rightHandBone.rotation * Quaternion.Euler(arrowHoldingRotationOffsetDuringReload);
                    };
                };
                bowWeapon.dontBreakAimingWhileReload = true;
            }
        }

        private void LateUpdate()
        {
            if (bowWeapon?.ParentShooterFighter != null)
            {
                var targetStringPos = bowWeapon.ParentShooterFighter.IsAiming && !bowWeapon.ParentShooterFighter.IsLoadingAmmo && !bowWeapon.ParentShooterFighter.IsReloading
                                ? rightHandBone.position + rightHandBone.TransformDirection(stringHoldingOffset)
                                : holdTransform.position;
                if (bowWeapon.ParentShooterFighter.IsReloading)
                    currentStringPosition = Vector3.Lerp(currentStringPosition, targetStringPos, Time.deltaTime * stringSmoothing);
                else
                    currentStringPosition = targetStringPos;

                topLine.SetPosition(0, topLine.transform.position);
                topLine.SetPosition(1, currentStringPosition);
                bottomLine.SetPosition(0, bottomLine.transform.position);
                bottomLine.SetPosition(1, currentStringPosition);

                if (bowWeapon.CurrentLoadedAmmo != null && !bowWeapon.CurrentLoadedAmmo.ReadyToPerform && !bowWeapon.CurrentLoadedAmmo.IsHit)
                {
                    if (bowWeapon.ParentShooterFighter.IsAiming || bowWeapon.ParentShooterFighter.IsReloading)
                    {
                        bowWeapon.CurrentLoadedAmmo.transform.parent.position = rightHandBone.position + rightHandBone.TransformDirection(arrowHoldingOffset);

                        if (bowWeapon.ParentShooterFighter.IsReloading)
                        {
                            // Fix rotation during reload to match hand orientation
                            bowWeapon.CurrentLoadedAmmo.transform.parent.rotation = Quaternion.RotateTowards(bowWeapon.CurrentLoadedAmmo.transform.parent.rotation, Quaternion.LookRotation(bowWeapon.aimController.handAlignment.Helper.position - bowWeapon.CurrentLoadedAmmo.transform.parent.position), Time.deltaTime * 300); // Adjust these values as needed
                        }
                        else if (!bowWeapon.ParentShooterFighter.IsReloading)
                        {
                            bowWeapon.CurrentLoadedAmmo.transform.parent.forward = arrowForwardReference.position - bowWeapon.CurrentLoadedAmmo.transform.parent.position;
                        }
                    }
                    else
                    {
                        bowWeapon.CurrentLoadedAmmo.transform.parent.localPosition = Vector3.zero;
                        bowWeapon.CurrentLoadedAmmo.transform.parent.localRotation = Quaternion.identity;
                    }
                }
            }
            else
            {
                topLine.SetPosition(0, topLine.transform.position);
                topLine.SetPosition(1, holdTransform.position);
                bottomLine.SetPosition(0, bottomLine.transform.position);
                bottomLine.SetPosition(1, holdTransform.position);
            }
        }
    }
}