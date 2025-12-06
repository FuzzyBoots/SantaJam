using FS_Core;
using FS_Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FS_ThirdPerson
{
    public class EquippableItemObject : MonoBehaviour
    {
        [Tooltip("Data associated with this equippable item.")]
        public EquippableItem itemData;

        Rigidbody rb;
        Collider collider;

        [HideInInspector] public Vector3 defaultLocalPos;
        [HideInInspector] public Quaternion defaultLocalRot;

        public bool EnableEquip { get; set; }


        public virtual void OnEnable()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (collider == null)
                collider = GetComponents<Collider>().FirstOrDefault(c => !c.isTrigger);
            HandlePhysics(false, false);
        }

        public void HandlePhysics(bool enable = true, bool enableEquip = true)
        {
            if (rb != null)
                rb.isKinematic = !enable;
            if (collider != null)
                collider.enabled = enable;
            this.EnableEquip = enableEquip;
        }
    }
}
