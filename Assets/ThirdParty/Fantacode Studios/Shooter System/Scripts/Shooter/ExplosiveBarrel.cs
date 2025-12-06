using FS_Core;
using FS_ShooterSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace FS_ShooterSystem
{
    public class ExplosiveBarrel : MonoBehaviour
    {
        [SerializeField] ShooterAmmo explosiveAmmo;
        [SerializeField] UnityEvent onExplode;

        Damagable damagable;
        private void Awake()
        {
            damagable = GetComponent<Damagable>();
        }

        private void Start()
        {
            damagable.OnDead += Explode;
        }

        void Explode()
        {
            var obj = Instantiate(explosiveAmmo.ammo, transform.position, Quaternion.identity);
            var ammoObj = obj.GetComponentInChildren<ShooterAmmoObject>();
            ammoObj.Ammo = explosiveAmmo;
            ammoObj.ReadyToPerform = true;

            onExplode?.Invoke();

            Destroy(gameObject);
        }
    }
}
