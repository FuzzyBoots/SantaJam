using UnityEngine;

namespace FS_CombatCore
{
    public class VisionSensor : MonoBehaviour
    {
        FighterCore fighter;

        private void Awake()
        {
            if (fighter == null)
                fighter = GetComponentInParent<FighterCore>();

            if (fighter != null)
                fighter.VisionSensor = this;
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == fighter.gameObject) return;

            if (fighter.IsTarget(other.gameObject))
            {
                var target = other.GetComponent<FighterCore>();
                fighter.TargetsInRange.Add(target);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (fighter.IsTarget(other.gameObject))
            {
                var target = other.GetComponent<FighterCore>();
                fighter.TargetsInRange.Remove(target);
            }
        }
    }
}