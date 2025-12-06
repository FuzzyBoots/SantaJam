using FS_ThirdPerson;
using UnityEngine;
using UnityEngine.Pool;

namespace FS_ShooterSystem
{
    public class ThrowableItemObject : EquippableItemObject
    {
        [Tooltip("Currently loaded ammo.")]
        public ShooterAmmoObject CurrentEquippedAmmo { get; set; }

        [Tooltip("Total number of ammos available.")]
        [field: SerializeField] public int TotalAmmoCount { get; set; } = 5;

        [Tooltip("The position and rotation where throwable ammo will be spawned when equip")]
        public Transform throwableAmmoSpawnPoint;

        [Tooltip("Visual representation of the item to display in the world when dropped.")]
        public GameObject throwableItemModel;


        public ShooterFighter ParentShooterFighter { get; set; }

        public override void OnEnable()
        {
            if (bulletPool == null)
            {
                CreatePooledBullet();
            }
            base.OnEnable();
        }

        public void EquipThrowableAmmo()
        {
            if (TotalAmmoCount > 0)
            {
                CurrentEquippedAmmo = GetBullet();
                CurrentEquippedAmmo.transform.parent.transform.parent = throwableAmmoSpawnPoint;
                CurrentEquippedAmmo.transform.parent.transform.localPosition = Vector3.zero;
                CurrentEquippedAmmo.transform.parent.transform.localRotation = Quaternion.identity;
                CurrentEquippedAmmo.transform.localPosition = Vector3.zero;
                CurrentEquippedAmmo.transform.localRotation = Quaternion.identity;

                CurrentEquippedAmmo.Initialize(throwableAmmoSpawnPoint.position, CurrentEquippedAmmo.transform.forward, 0, ReturnBullet);
                CurrentEquippedAmmo.ParentShooterFighter = ParentShooterFighter;

                if (CurrentEquippedAmmo.rigidBody != null)
                {
                    CurrentEquippedAmmo.rigidBody.excludeLayers = LayerMask.GetMask("HitBone");
                }
            }
        }

        #region Ammo Pooling

        private const int DEFAULT_POOL_CAPACITY = 2;
        private const int MAX_POOL_CAPACITY = 20;
        private ObjectPool<ShooterAmmoObject> bulletPool;

        public void CreatePooledBullet()
        {
            bulletPool = new ObjectPool<ShooterAmmoObject>(
                CreateBullet,
                EnableBullet,
                DisableBullet,
                OnDestroyBullet,
                false,
                DEFAULT_POOL_CAPACITY,
                MAX_POOL_CAPACITY
            );
        }

        private ShooterAmmoObject CreateBullet()
        {
            ShooterAmmo ammo = ParentShooterFighter.IsShooterWeaponEquipped ? ParentShooterFighter.CurrentWeapon.ammoData : ParentShooterFighter.CurrentThrowableItem.ammo;
            GameObject bulletObject = Instantiate(ammo.ammo);
            ShooterAmmoObject ammoObject = bulletObject.GetComponentInChildren<ShooterAmmoObject>();
            ammoObject.Ammo = ammo;
            ammoObject.SetAudioSource();
            ammoObject.gameObject.SetActive(false);
            return ammoObject;
        }

        private void EnableBullet(ShooterAmmoObject ammo)
        {
            ammo.gameObject.SetActive(true);
        }

        private void DisableBullet(ShooterAmmoObject ammo)
        {
            ammo.gameObject.SetActive(false);
            ammo.ReadyToPerform = false;
            ammo.IsHit = false;
        }

        private void OnDestroyBullet(ShooterAmmoObject ammo)
        {
            Destroy(ammo.transform.parent.gameObject);
        }

        public ShooterAmmoObject GetBullet()
        {
            if (bulletPool == null)
            {
                CreatePooledBullet();
            }
            var ammo = bulletPool.Get();

            if (ParentShooterFighter.CurrentWeapon != null)
            {
                while (ParentShooterFighter.CurrentWeapon.ammoData != ammo.Ammo)
                {
                    OnDestroyBullet(ammo);
                    ammo = bulletPool.Get();
                }
            }

            return ammo;
        }

        public void ReturnBullet(ShooterAmmoObject ammo)
        {
            bulletPool.Release(ammo);
        }
        #endregion
    }

}
