using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using UnityEngine;

namespace FS_ShooterSystem
{
    [CreateAssetMenu(menuName = "Shooter System/Create Throwable Item")]
    public class ThrowableItem : CombatWeapon
    {
        public ShooterAmmo ammo;
        public bool zoomWhileAim = true;

        [Tooltip("Settings for the camera when aiming the throwable item.")]
        public CameraSettings aimCameraSettings = new CameraSettings() { distance = .4f, framingOffset = new Vector3(.4f, 1.5f, 0), followSmoothTime = 0.001f };

        [Tooltip("Maximum distance the throwable item can be thrown")]
        public float maxThrowDistance = 5;
        [Tooltip("Base force applied when throwing the throwable item")]
        public float throwForce = 10;
        [Tooltip("Controls the height of the throwable item's throwing arc - higher values create a more curved trajectory")]
        public float arcHeight = 0.5f;
        [Tooltip("Multiplier that adjusts throw force based on target distance - affects how the force scales with distance")]
        public float distanceMultiplier = 0.8f;

        public AnimGraphClipInfo aimAnimation;
        public AnimGraphClipInfo throwAnimation;

        public override void SetCategory()
        {
            category = Resources.Load<ItemCategory>("Category/Throwable");
        }
    }
}