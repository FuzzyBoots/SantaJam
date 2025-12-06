using UnityEngine;
using UnityEngine.EventSystems;
#if inputsystem
using UnityEngine.InputSystem.UI;
#endif
namespace FS_Core
{
    public class FSSettings : MonoBehaviour
    {
        [SerializeField] LayerMask groundLayer = 1;

        public LayerMask GroundLayer => groundLayer;

        public static FSSettings i { get; private set; }

        EventSystem eventSystem;
        private void Awake()
        {
            if (!(groundLayer == (groundLayer | (1 << LayerMask.NameToLayer("Ledge")))))
                groundLayer += 1 << LayerMask.NameToLayer("Ledge");
            if (LayerExists("Hotspot") && !(groundLayer == (groundLayer | (1 << LayerMask.NameToLayer("Hotspot")))))
                groundLayer += 1 << LayerMask.NameToLayer("Hotspot");

            i = this;
            eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem != null && eventSystem.GetComponent<BaseInputModule>() == null)
            {
#if inputsystem
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
                 eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        bool LayerExists(string layerName)
        {
            return LayerMask.NameToLayer(layerName) != -1;
        }
    }

    public enum DirectionAxis
    {
        [InspectorName("X")] PositiveX,
        [InspectorName("-X")] NegativeX,
        [InspectorName("Y")] PositiveY,
        [InspectorName("-Y")] NegativeY,
        [InspectorName("Z")] PositiveZ,
        [InspectorName("-Z")] NegativeZ
    }
}


