using System;
using System.Collections.Generic;
using UnityEngine;
namespace FS_ThirdPerson
{
    public class SystemBase : MonoBehaviour
    {
        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        public virtual void HandleAwake() { }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        public virtual void HandleStart() { }

        /// <summary>
        /// Called every fixed framerate frame, if the MonoBehaviour is enabled.
        /// </summary>
        public virtual void HandleFixedUpdate() { }

        /// <summary>
        /// Called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public virtual void HandleUpdate() { }

        /// <summary>
        /// Called when the animator moves.
        /// </summary>
        /// <param name="animator">The animator that moved.</param>
        public virtual void HandleOnAnimatorMove(Animator animator)
        {
            if (animator.deltaPosition != Vector3.zero)
                transform.position += animator.deltaPosition;
            transform.rotation *= animator.deltaRotation;
        }

        /// <summary>
        /// Gets or sets the priority of the system. Higher numbers indicate higher priority.
        /// </summary>
        public virtual float Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether this system is in focus.
        /// </summary>
        public bool IsInFocus { get; set; }

        /// <summary>
        /// Gets the state of the system.
        /// </summary>
        [field: SerializeField]
        public virtual SystemState State { get; } = SystemState.Other;

        /// <summary>
        /// Gets the sub-state of the system.
        /// </summary>
        public virtual SubSystemState SubState { get; } = SubSystemState.None;

        /// <summary>
        /// Focuses this script, making it the only one to run. All other updates are discarded.
        /// </summary>
        /// <param name="executeStates">If set to <c>true</c>, execute states.</param>
        public void FocusScript() => IsInFocus = true;

        /// <summary>
        /// Unfocuses this script, allowing other updates to run.
        /// </summary>
        public void UnFocusScript() => IsInFocus = false;

        /// <summary>
        /// Checks if the specified method is overridden in a derived class.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <returns><c>true</c> if the method is overridden; otherwise, <c>false</c>.</returns>
        public bool HasOverrided(string methodName) => SystemBaseType.GetMethod(methodName).DeclaringType != SystemBaseType;

        ///// <summary>
        ///// Gets a value indicating whether execution states are being executed.
        ///// </summary>
        //public bool ExecuteStates { get; private set; }

        ///// <summary>
        ///// Gets the execution states of the system.
        ///// </summary>
        //public virtual List<SystemState> ExecutionStates => new List<SystemState> { State };

        /// <summary>
        /// Called when the system is entered.
        /// </summary>
        public virtual void EnterSystem() { }

        /// <summary>
        /// Called when the system is exited.
        /// </summary>
        public virtual void ExitSystem() { }


        /// <summary>
        /// Called to reset the fighter.
        /// </summary>
        public virtual void OnResetFighter() { }

        /// <summary>
        /// Gets or sets the action to be called when the state is entered.
        /// </summary>
        public Action OnStateEntered { get; set; }

        /// <summary>
        /// Gets or sets the action to be called when the state is exited.
        /// </summary>
        public Action OnStateExited { get; set; }

        private static readonly Type SystemBaseType = typeof(SystemBase);
    }
    public class EquippableSystemBase : SystemBase
    {
        public virtual List<Type> EquippableItems => new List<Type>();
        
        public List<SystemBase> SystemExclusionList => new List<SystemBase>();

        //public bool HasEquippableItem => EquippableItems.Count > 0;
    }
}