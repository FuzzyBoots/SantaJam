using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace FS_ShooterSystem
{
    [CustomIcon(FolderPath.DataIcons + "Shooter Weapon Icon.png")]
    [Icon(FolderPath.DataIcons + "Shooter Weapon Icon.png")]
    [CreateAssetMenu(menuName = "Shooter System/Create Weapon")]
    public class ShooterWeapon : CombatWeapon
    {
        [Tooltip("The data for the ammo used by this weapon.")]
        public ShooterAmmo ammoData;

        [Tooltip("The maximum effective range of the weapon.")]
        public float range = 100f;

        [Tooltip("Use the length of the reload animation as the reload time.")]
        public bool useReloadAnimationDuration = true;

        [Tooltip("The time required to reload the weapon (in seconds).")]
        public float reloadTime = 2f;

        [Tooltip("If true, the weapon play a ammo load animation after each shot (e.g., bolt-action sniper).")]
        public bool playLoadAnimationPerShot = false;

        [Tooltip("Normalized time (0 to 1) in the reload animation when the ammo (e.g., arrow) becomes available or visible.")]
        [Range(0,1)]
        public float ammoEnableTime = 0f;

        [Tooltip("The radius in which enemies can hear this weapon when fired.")]
        public float soundRange = 50f;

        [Tooltip("Audio clip for the firing sound.")]
        public AudioClip fireAudioClip;

        [Tooltip("Audio clip for the trigger sound when out of ammo.")]
        public AudioClip triggerAudioClip;

        [Tooltip("Automatically reload when out of ammo.")]
        public bool autoReload;

        [Tooltip("If true, the player cannot manually reload this weapon. Useful for weapons that reload automatically or use single-use ammo.")]
        public bool preventManualReload = false;

        [Tooltip("Audio clip for the reload sound.")]
        public AudioClip reloadAudioClip;

        [Tooltip("Audio clip for load ammo.")]
        public AudioClip loadAmmoAudioClip;

        [Tooltip("Indicates if the weapon fires in burst mode.")]
        public bool isBurst;

        [Tooltip("The type of fire mode (Single or Auto).")]
        public FireType fireType;

        [Tooltip("The number of bullets fired in a single burst.")]
        public int burstFireBulletCount = 3;

        [Tooltip("The range of bullet spread angles during firing, defined as minimum and maximum values.")]
        public Vector2 burstSpreadingAngleRange = new Vector2(2.5f, 5f);

        [Tooltip("Information about the weapon's recoil.")]
        public RecoilInfo weaponRecoilInfo;

        [Tooltip("The radius of bullet spread.")]
        public float bulletSpreadRadius = 0.1f;

        [Tooltip("Enable recoil effect.")]
        public bool enableRecoil = true;

        [Tooltip("Allow firing without aiming.")]
        public bool hipFire = false;

        [Tooltip("Allow aiming without ammo.")]
        public bool canAimWithoutAmmo = true;

        [Tooltip("Allow jumping while the character is in aiming.")]
        public bool allowJumpingWhileAiming = true;

        [Tooltip("Enable zoom while aiming.")]
        public bool zoomWhileAim = true;

        [Tooltip("Settings for the camera when aiming the weapon.")]
        public CameraSettings aimCameraSettings = new CameraSettings() { distance = 1f, framingOffset = new Vector3(.4f, 1.5f, 0), followSmoothTime = 0f, sensitivity = .2f };

        // Animations
        [Tooltip("Animation clip for aiming.")]
        public AnimGraphClipInfo aimClip;

        [Tooltip("Animation clip for reloading.")]
        public AnimGraphClipInfo reloadClip;
        [Tooltip("Specifies which body parts the reload animation will affect")]
        public Mask reloadAnimationMask = Mask.Arm;

        [Tooltip("Animation clip for reloading.")]
        public AnimGraphClipInfo loadAmmoClip;

        [Tooltip("Animation clip for shooting.")]
        public AnimGraphClipInfo shootClip;

        [Tooltip("Should the target play a reaction animation hit by this weapon.")]
        public bool playReactionOnTarget = true;

        [Tooltip("Tag used to identify the reaction for this weapon like light, medium, heavy, etc.")]
        public string targetReactionTag;

        [Tooltip("Specifies which body parts the aiming animation will affect")]
        public Mask aimAnimationMask = Mask.Arm;
        public Mask shootAnimationMask = Mask.Arm;


        [Tooltip("Enable pressure based shot strength (hold to draw/release, e.g. bow & arrow).")]
        public bool isChargedWeapon;
        [Tooltip("Animation clip played while the shot is being charged/drawn.")]
        public AnimGraphClipInfo chargeClip;
        public Mask chargeAnimationMask = Mask.RightArm;
        public float chargeForce = 1;


        [Tooltip("Enable IK adjustments during the weapon's idle state for proper hand positioning")]
        public bool UseHandIkForIdle = true;

        public ArmIkData idleIKData;
        public ArmIkData aimIKData;

        public List<ArmIKReference> iKReferences = new List<ArmIKReference>();

        [SerializeField, HideInInspector] private bool isInitialized = false;

        public override void SetCategory()
        {
            category = Resources.Load<ItemCategory>("Category/Shooter Weapon");
        }
    }

    [System.Serializable]
    public class ArmIKReference
    {
        [Tooltip("List of character IDs that this IK configuration applies to.")]
        public List<CharacterReference> characterReferences = new List<CharacterReference>();

        [Tooltip("IK data used when the character is in an idle state.")]
        public ArmIkData idleIKData;

        [Tooltip("IK data used when the character is aiming.")]
        public ArmIkData aimIKData;
    }

    [System.Serializable]
    public class ArmIkData
    {
        [Header("Main Hand (Holding Weapon)")]
        [Tooltip("Local position offset for the main (holding) hand relative to its IK target.")]
        public Vector3 holdHandPosition;

        [Tooltip("Local rotation offset for the main (holding) hand in degrees.")]
        public Vector3 holdHandRotation;

        [Space(10)]
        [Header("Support Hand")]
        [Tooltip("Local position offset for the support hand relative to its IK target.")]
        public Vector3 supportHandPosition;

        [Tooltip("Local rotation offset for the support hand in degrees.")]
        public Vector3 supportHandRotation;
    }

    public enum FireType { Single, Auto }
}