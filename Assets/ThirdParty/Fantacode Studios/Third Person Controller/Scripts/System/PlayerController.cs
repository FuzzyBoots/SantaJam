using FS_Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

namespace FS_ThirdPerson
{
    public enum SystemState
    {
        Locomotion, Parkour, Climbing, Combat, GrapplingHook, Swing, Shooter, Cover, Zipline, Gliding, Swim, Other
    }

    public enum SubSystemState
    {
        None, Combat, Climbing, Other
    }
    public enum FSCameraType { FirstPerson, ThirdPerson }

    [DefaultExecutionOrder(-20)]
    public class PlayerController : MonoBehaviour, ISavable
    {
        public List<SystemBase> managedScripts = new List<SystemBase>();
        public List<SystemBase> CoreSystems { get; set; } = new List<SystemBase>();
        public List<EquippableSystemBase> EquippableSystems { get; set; } = new List<EquippableSystemBase>();

        public Dictionary<SystemState, SystemBase> systems = new Dictionary<SystemState, SystemBase>();

        [field: SerializeField] public SystemState CurrentSystemState { get; private set; }
        public FSCameraType CameraType { get; set; } = FSCameraType.ThirdPerson;
        public Action<FSCameraType> OnCameraTypeChanged;

        public SystemState CurrentEquippedSystemState => CurrentEquippedSystem.State;

        [HideInInspector]
        [SerializeField]
        private string _editorSearchString = string.Empty;

        public SystemState PreviousSystemState { get; private set; }
        public SystemState DefaultSystemState => SystemState.Locomotion;
        public SystemState FocusedSystemState => FocusedScript == null ? DefaultSystemState : FocusedScript.State;

        public Quaternion CameraPlanarRotation => Quaternion.LookRotation(Vector3.Scale(cameraGameObject.transform.forward, new Vector3(1, 0, 1)));
        public bool AlignTargetWithCameraForward { get; set; }
        bool prevAlignTargetWithCameraForward;

        public void StopAlignCameraForward()
        {
            prevAlignTargetWithCameraForward = AlignTargetWithCameraForward;
            AlignTargetWithCameraForward = false;
        }
        public void ResetAlignCameraForward()
        {
            AlignTargetWithCameraForward = prevAlignTargetWithCameraForward;
        }

        public GameObject cameraGameObject { get; set; }
        public CameraSettings CurrentCameraSettings { get; set; }


        public Animator animator { get; set; }
        ItemEquipper equippableItemController;
        public ICharacter player { get; set; }
        public Damagable Damagable { get; private set; }

        public Action<float, float> OnStartCameraShake;
        public Action<Vector3> CameraLookAtPoint;
        public Action<CameraSettings> SetCustomCameraState;
        public Action<RecoilInfo> CameraRecoil;
        public Action<float, float> OnLand;

        public bool IsInAir { get; set; }
        public bool PreventRotation { get; set; }
        public bool PreventFallingFromLedge { get; set; } = true;
        public bool PreventCameraShake { get; set; } = false;
        public bool PreventVerticalJump { get; set; } = false;
        public Action OnCameraLateUpdate { get; set; }
        public bool IsDead => Damagable.CurrentHealth <= 0;

        public bool IsItemEquipped => equippableItemController.EquippedItem != null;
        public EquippableSystemBase CurrentEquippedSystem { get; private set; }

        void HandleCurrentEquippedItemUpdate(params SystemState[] systemState)
        {
            if (systemState.Length == 0 || (CurrentEquippedSystem != null && systemState.Contains(CurrentEquippedSystem.State)))
                CurrentEquippedSystem?.HandleUpdate();
        }

        void HandleCurrentEquippedItemFixedUpdate(params SystemState[] systemState)
        {
            if (systemState.Length == 0 || (CurrentEquippedSystem != null && systemState.Contains(CurrentEquippedSystem.State)))
                CurrentEquippedSystem?.HandleFixedUpdate();
        }

        #region Unity Methods

        private void Awake()
        {
            player = GetComponent<ICharacter>();
            Damagable = GetComponent<Damagable>();
            equippableItemController = GetComponent<ItemEquipper>();
            cameraGameObject = Camera.main.gameObject;
            animator = player.Animator;

            EquippableSystems = managedScripts.Where(x => x is EquippableSystemBase).Cast<EquippableSystemBase>().ToList();
            CoreSystems = managedScripts.Where(x => !(x is EquippableSystemBase)).ToList();

            foreach (var script in managedScripts)
            {
                script?.HandleAwake();
            }

            if (equippableItemController != null)
            {
                equippableItemController.OnEquip += (EquippableItem itemData) =>
                {
                    CurrentEquippedSystem = EquippableSystems.FirstOrDefault(m => m.EquippableItems.Contains(itemData?.GetType()));
                };
                equippableItemController.OnUnEquipComplete += () => { CurrentEquippedSystem = null; };
            }

            var playerLayer = LayerMask.NameToLayer("Player");
            if (gameObject.layer != playerLayer)
                gameObject.layer = playerLayer;
        }

        private void Start()
        {
            foreach (var script in managedScripts)
            {
                systems.Add(script.State, script);
            }

            foreach (var script in managedScripts)
            {
                script?.HandleStart();
            }
        }

        private void FixedUpdate()
        {
            if (player.PreventAllSystems || Time.timeScale <= 0) return;
            var focusedScript = FocusedScript;

            if (focusedScript)
            {
                focusedScript.HandleFixedUpdate();

                if (IsInAir && CurrentEquippedSystem != focusedScript)
                    HandleCurrentEquippedItemFixedUpdate();
            }
            else
            {
                foreach (var script in CoreSystems)
                {
                    if (FocusedScript != null) return;
                    if (script.enabled && script is not EquippableSystemBase)
                        script.HandleFixedUpdate();
                }
                if (FocusedScript == null)
                    CurrentEquippedSystem?.HandleFixedUpdate();
            }
        }

        private void Update()
        {
            if (player.PreventAllSystems) return;
            var focusedScript = FocusedScript;

            if (focusedScript)
            {
                focusedScript.HandleUpdate();
                if (IsInAir && CurrentEquippedSystem != focusedScript)
                    HandleCurrentEquippedItemUpdate();
            }
            else
            {
                foreach (var script in CoreSystems)
                {
                    if (FocusedScript != null) return;
                    if (script.enabled && script is not EquippableSystemBase)
                        script.HandleUpdate();
                }
                if(FocusedScript == null)
                    CurrentEquippedSystem?.HandleUpdate();
            }
        }

        private void OnAnimatorMove()
        {
            if (player.UseRootMotion)
            {
                var focusedScript = FocusedScript;
                if (focusedScript)
                {
                    focusedScript.HandleOnAnimatorMove(animator);
                    return;
                }

                if (animator.deltaPosition != Vector3.zero)
                    transform.position += animator.deltaPosition;
                transform.rotation *= animator.deltaRotation;
            }
        }

        #endregion

        #region System Management

        public SystemBase FocusedScript => CoreSystems.FirstOrDefault(x => x.IsInFocus) ?? EquippableSystems.FirstOrDefault(x => x.IsInFocus);

        public void UnfocusAllSystem()
        {
            foreach (var system in managedScripts)
            {
                if (system.IsInFocus)
                    system.UnFocusScript();
            }
        }

        public void SetSystemState(SystemState newState)
        {
            if (CurrentSystemState != newState)
            {
                PreviousSystemState = CurrentSystemState;
                CurrentSystemState = newState;
            }
            if (PreviousSystemState != CurrentSystemState)
            {
                managedScripts.FirstOrDefault(s => s.State == PreviousSystemState)?.OnStateExited?.Invoke();
                managedScripts.FirstOrDefault(s => s.State == CurrentSystemState)?.OnStateEntered?.Invoke();
            }
        }

        public void ResetState()
        {
            //CurrentSystemState = equippableItemController.CurrentEquippableItemRight != null || equippableItemController.CurrentEquippableItemLeft != null ? PreviousSystemState : DefaultSystemState;
            PreviousSystemState = CurrentSystemState; 
            CurrentSystemState = DefaultSystemState; 

            //if (CurrentSystemState != DefaultSystemState)
            //    managedScripts.FirstOrDefault(s => s.State == CurrentSystemState)?.FocusScript();
            //if (PreviousSystemState != CurrentSystemState)
            //{
            //    managedScripts.FirstOrDefault(s => s.State == PreviousSystemState)?.OnStateExited?.Invoke();
            //    managedScripts.FirstOrDefault(s => s.State == CurrentSystemState)?.OnStateEntered?.Invoke();
            //}
        }

        public void PreviousState()
        {
            var prevState = PreviousSystemState;
            prevState = CurrentSystemState;
            CurrentSystemState = prevState;
        }

        #endregion

        #region Register And Unregister Systems

        public void Register(SystemBase script, bool reorderScripts = false)
        {
            if (!managedScripts.Contains(script))
                managedScripts.Add(script);

            if (reorderScripts)
                managedScripts = managedScripts.OrderByDescending(x => x.Priority).ToList();
        }

        public void Unregister(SystemBase script)
        {
            managedScripts.Remove(script);
        }

        #endregion

        public void ResetPlayer()
        {
            foreach (var system in managedScripts)
            {
                system.OnResetFighter();
            }

            Damagable.CurrentHealth = Damagable.MaxHealth;
            equippableItemController.UnEquipItem(false);
        }



        [ContextMenu("Find Systems")]
        public void FindSystems()
        {
            managedScripts = GetComponents<SystemBase>().ToList().OrderBy(x => x.Priority).ToList();
        }

        public object CaptureState()
        {
            var saveData = new PlayerSaveData()
            {
                position = new Vector3SaveData(transform.position),
                rotation = new Vector3SaveData(transform.rotation.eulerAngles)
            };

            return saveData;
        }

        public void RestoreState(object state)
        {
            var savedData = (PlayerSaveData)state;
            transform.position = savedData.position.GetVector();
            transform.rotation = savedData.rotation.GetQuaternion();
            Physics.SyncTransforms();
        }

        public Type GetSavaDataType()
        {
            return typeof(PlayerSaveData);
        }
    }


    [Serializable]
    public class RecoilInfo
    {
        [Tooltip("The amount of camera movement caused by recoil. (X = Horizontal, Y = Vertical)")]
        public Vector2 CameraRecoilAmount = new Vector2(0.3f, 2);

        [Tooltip("The total duration of the camera recoil effect, in seconds.")]
        public float CameraRecoilDuration = 0.3f;

        [Tooltip("The percentage of the recoil duration spent reaching the maximum recoil position.")]
        [Range(0.001f, 1)]
        public float recoilPhasePercentage = 0.2f;

        [Tooltip("The minimum possible recoil movement. (X = Horizontal, Y = Vertical)")]
        public Vector2 minRecoilAmount = new Vector2(0.2f, 1);

    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3SaveData position;
        public Vector3SaveData rotation;
    }

    [Serializable]
    public class Vector3SaveData
    {
        public float x;
        public float y;
        public float z;

        public Vector3SaveData(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public Vector3 GetVector()
        {
            return new Vector3(x, y, z);
        }

        public Quaternion GetQuaternion()
        {
            return Quaternion.Euler(x, y, z);
        }
    }
}