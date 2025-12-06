using FS_CombatCore;
using System.Linq;
using UnityEngine;

namespace FS_CombatSystem
{
    public class DemoSceneDoorManager : MonoBehaviour
    {
        GameObject currentObjectInTheRoom;
        public MeleeWeapon defaultWeapon;
        public FighterCore player;
        public Material openedDoorMat;
        public Material closedDoorMat;

        private void Awake()
        {
            player.gameObject.layer = LayerMask.NameToLayer("Player");
            player.targetLayer = LayerMask.GetMask("Enemy");
        }


        public void SetRoomForTutorial(MeleeWeapon weaponData, GameObject _object)
        {
            if (currentObjectInTheRoom != null)
                Destroy(currentObjectInTheRoom);

            if (_object != null)
            {
                currentObjectInTheRoom = Instantiate(_object);
                var enemies = currentObjectInTheRoom.GetComponentsInChildren<CombatAIController>().ToList();
                var visionsensors = currentObjectInTheRoom.GetComponentsInChildren<VisionSensor>().ToList();
                enemies.ForEach(e => 
                {
                    e.gameObject.layer = LayerMask.NameToLayer("Enemy");
                    e.GetComponent<FighterCore>().targetLayer = LayerMask.GetMask("Player");
                });
                visionsensors.ForEach(e => e.gameObject.layer = LayerMask.NameToLayer("VisionSensor"));
            }
            player.QuickSwitchWeapon(weaponData);
        }
    }
}