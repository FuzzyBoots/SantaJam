using UnityEngine;

namespace FS_Core
{
    public class MessageHotspot : Hotspot
    {
        [SerializeField] string message;

        public override void ShowIndicator(bool state)
        {
            base.ShowIndicator(state);

            if (interactText != null)
                interactText.text = message;
        }

        public override void Interact(HotspotDetector detector)
        {
            // Do nothing, this is just a message hotspot
        }
    }
}
