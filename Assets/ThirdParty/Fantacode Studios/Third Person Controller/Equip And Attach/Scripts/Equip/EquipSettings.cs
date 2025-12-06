using FS_Util;
using UnityEngine;

namespace FS_Core
{
    public class EquipSettings : MonoBehaviour
    {
        [SerializeField] EquipHotspot equipHotspot;
        [SerializeField] bool equipItemsBasedOnSlot;
        //[ShowIf("equipItemsBasedOnSlot", true)]
        //[SerializeField] EquipmentSlotsDatabase slotsDatabase;

        public EquipHotspot EquipHotspot => equipHotspot;
        public bool EquipItemsBasedOnSlot => equipItemsBasedOnSlot;
        //public EquipmentSlotsDatabase SlotsDatabase => slotsDatabase;


        public static EquipSettings i { get; private set; }
        private void Awake()
        {
            i = this;
        }


    }
}
