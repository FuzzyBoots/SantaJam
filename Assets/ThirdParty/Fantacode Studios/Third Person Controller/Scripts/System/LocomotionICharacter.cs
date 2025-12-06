using System;
using System.Collections;
using UnityEngine;

namespace FS_ThirdPerson
{
    public interface LocomotionICharacter : ICharacter
    {
        /// <summary>
        /// Gravity for playing acitons
        /// </summary>
        public void ChangeMoveSpeed(float walk, float run, float sprint);

        public void ResetMoveSpeed();

        public (Vector3,Vector3) LedgeMovement(Vector3 currMoveDir, Vector3 currVelocity);

        public void HandleTurningAnimation(bool reset);

        public bool CheckIsGrounded();

        public bool IsOnLedge { get; set; }

        public void SetMaxSpeed(float maxSpeed = 2, bool reset = false);

        public void ReachDestination(Vector3 dest, float speed = 3f);

        /// <summary>
        /// This function handles the player's falling behavior.
        /// It takes the character's downward speed and previous velocity to determine the impact or necessary adjustments.
        /// </summary>
        void SetCurrentVelocity(float ySpeed, Vector3 previousCharacterVelocity);
    }
}
