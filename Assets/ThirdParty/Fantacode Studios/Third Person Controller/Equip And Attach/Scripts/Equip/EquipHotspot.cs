using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FS_Core
{
    public class EquipHotspot : Hotspot
    {
        public EquippableItem item;
        [Space(5)]
        public UnityEvent OnEquipEvent;

        [HideInInspector] public EquippableItemObject itemObject; // Reference of item object incase we want to disable it after interaction

        public override void ShowIndicator(bool state)
        {
            base.ShowIndicator(state);

            if (interactText != null)
                interactText.text = "[E] Equip " + item.Name;
        }

        public override void Interact(HotspotDetector detector)
        {
            var itemEquipper = detector.GetComponent<ItemEquipper>();
            if (!itemEquipper.PreventItemSwitching && !itemEquipper.IsChangingItem)
            {
                //var itemObject = GetComponent<EquippableItemObject>();
                itemEquipper.EquipItem(item, false, itemObject: itemObject);
                OnEquipEvent?.Invoke();

                base.Interact(detector);
                if (itemObject != null)
                    itemObject.gameObject.SetActive(false);
            }
        }
    }
}