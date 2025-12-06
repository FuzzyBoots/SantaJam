using TMPro;
using UnityEngine;

namespace FS_Core
{
    public class Hotspot : MonoBehaviour
    {
        public bool disableAfterInteraction = true;
        [SerializeField] GameObject interactUIPrefab;
        [SerializeField] float interactUIOffset = 1;

        protected GameObject interactUI;
        protected TMP_Text interactText;

        private void Awake()
        {
            if(interactUIPrefab == null)
            {
                interactUIPrefab = (GameObject)Resources.Load("UI/Interact UI");
            }
        }

        public virtual void OnEnable()
        {
            gameObject.layer = LayerMask.NameToLayer("Hotspot");
            if (transform.Find("FS_InteractUI") != null)
            {
                interactUI = transform.Find("FS_InteractUI").gameObject;
            }
            if (interactUIPrefab != null && interactUI == null)
            {
                interactUI = Instantiate(interactUIPrefab, transform.position + Vector3.up * interactUIOffset, Quaternion.identity);
                var interactUIScale = interactUI.transform.lossyScale;
                interactUI.transform.SetParent(transform);
                SetGlobalScale(interactUI.gameObject.transform, interactUIScale);
                interactUI.name = "FS_InteractUI"; 
            }
            if (interactUI != null)
            {
                interactText = interactUI.GetComponentInChildren<TMP_Text>();
                interactUI.gameObject.SetActive(false);
                
            }
        }

        void LateUpdate()
        {
            if (interactUI != null)
            {
                // Always position the UI above this Hotspot in global Y axis
                Vector3 targetPosition = transform.position + Vector3.up * interactUIOffset;
                interactUI.transform.position = targetPosition;
            }
        }

        public virtual void ShowIndicator(bool state)
        {
            if (interactUI != null)
                interactUI.SetActive(state);
        }

        public virtual void Interact(HotspotDetector detector)
        {
            if (disableAfterInteraction)
                gameObject.SetActive(false);
        }

        void SetGlobalScale(Transform target, Vector3 desiredGlobalScale)
        {
            Vector3 currentGlobalScale = target.lossyScale;

            if (currentGlobalScale == Vector3.zero)
            {
                Debug.LogWarning("Cannot set global scale when current global scale is zero.");
                return;
            }

            Vector3 scaleRatio = new Vector3(
                desiredGlobalScale.x / currentGlobalScale.x,
                desiredGlobalScale.y / currentGlobalScale.y,
                desiredGlobalScale.z / currentGlobalScale.z
            );

            target.localScale = new Vector3(
                target.localScale.x * scaleRatio.x,
                target.localScale.y * scaleRatio.y,
                target.localScale.z * scaleRatio.z
            );
        }
    }
}
