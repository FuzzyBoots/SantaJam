using FS_ThirdPerson;
using FS_Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_Core
{

    public class HotspotDetector : MonoBehaviour
    {
        [SerializeField] float detectionRadius = 1f;

        [SerializeField] bool overrideHotspotLayer = false;

        [ShowIf("overrideHotspotLayer", true)]
        [SerializeField] LayerMask hotspotLayer = 1;

        private List<Hotspot> nearbyHotspots = new List<Hotspot>();
        private Hotspot closestHotspot;

        LocomotionInputManager inputManager;

        Damagable damagable;

        private void Awake()
        {
            inputManager = GetComponent<LocomotionInputManager>();
        }

        private void Start()
        {
            if (!overrideHotspotLayer)
                hotspotLayer = 1 << LayerMask.NameToLayer("Hotspot");

            damagable = GetComponent<Damagable>();
        }

        void Update()
        {
            if(damagable?.CurrentHealth <= 0)
                return; // Skip hotspot detection if the character is dead

            DetectHotspots();

            if (inputManager.Interaction && closestHotspot != null && Time.timeScale > 0)
            {
                closestHotspot.Interact(this);
            }
        }

        private Hotspot previousClosest;

        void DetectHotspots() 
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, hotspotLayer, queryTriggerInteraction: QueryTriggerInteraction.Collide);

            nearbyHotspots.Clear();
            foreach (Collider col in hits)
            {
                Hotspot hs = col.GetComponent<Hotspot>();
                if (hs != null)
                    nearbyHotspots.Add(hs);
            }

            var newClosest = GetClosestHotspot();

            // Toggle indicators
            if (previousClosest != newClosest)
            {
                if (previousClosest != null)
                    previousClosest.ShowIndicator(false);

                if (newClosest != null)
                    newClosest.ShowIndicator(true);

                previousClosest = newClosest;
            }

            closestHotspot = newClosest;
        }

        Hotspot GetClosestHotspot()
        {
            Hotspot closest = null;
            float minDist = float.MaxValue;

            foreach (var hs in nearbyHotspots)
            {
                float dist = Vector3.Distance(transform.position, hs.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = hs;
                }
            }

            return closest;
        }
    }

}
