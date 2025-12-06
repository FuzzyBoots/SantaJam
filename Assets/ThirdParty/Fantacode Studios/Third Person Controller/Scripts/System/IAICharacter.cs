using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FS_ThirdPerson
{
    public interface IAICharacter
    {
        /// <summary>
        /// Gravity for playing parkour acitons
        /// </summary>
        public float Gravity { get; }

        /// <summary>
        /// Animator of the controller
        /// </summary>
        public Animator Animator { get; set; }

        public bool WaitToStartSystem { get; set; }


        /// <summary>
        /// NavmeshAgent of the character
        /// </summary>
        public NavMeshAgent NavMeshAgent { get; set; }

        /// <summary>
        /// While true, the root motion of the animation will be applied to the character
        /// </summary>
        public bool UseRootMotion { get; set; }

        /// <summary>
        /// Return the user's input direction in this property. This is used to determine the direction in which the player should jump/perform parkour action. 
        /// </summary>
        public Vector3 MoveDir { get; }

        /// <summary>
        /// Return true in the property if the player's feet is touching the ground. The parkour actions will only be performed if the player is grounded.
        /// </summary>
        public bool IsGrounded { get; }

        /// <summary>
        /// Return true if you want to prevent the character from performing parkour actions.
        /// You can use this property and prevent parkour actions when the character is performing some other actions like attacking, shooting, reloading, etc.
        /// </summary>
        public bool PreventParkourAction { get; }

        /// <summary>
        /// This function will be called while starting a Parkour/Climbing action. 
        /// From this function you can do things like disabling the collider of the player, resetting the walking or running animation paramater, etc.
        /// </summary>
        void OnStartAction(bool unEquip = true, bool stopMovement = true, bool itemBecomeUnUsable = false);

        /// <summary>
        /// This function will be called once a Parkour/Climbing action is completed.
        /// From this function you can do things like enabling the collider of the player.
        /// </summary>
        void OnEndAction();

        bool IsBusy { get; set; }

    }

    public enum FollowType
    {
        FollowPlayer,
        FollowTarget,
        FollowWayPoints,
        RandomWanderer,
        RunFromPlayer,
        Stop,
        None
    }
}