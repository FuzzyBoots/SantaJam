#if gameCreator2
using GameCreator.Runtime.Characters;
#endif
using UnityEngine;


namespace FS_ParkourAI
{
    public class AIRootmotionController : MonoBehaviour
    {
#if gameCreator2
        Animator animator;

        public Character character;

        GC2AIManager aiCharacter;
        ParkourAI parkourAI;
        ClimbAI climbAI;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (character == null)
                character = GetComponentInParent<Character>();

            aiCharacter = character.GetComponentInChildren<GC2AIManager>();
            parkourAI = character.GetComponentInChildren<ParkourAI>();
            climbAI = character.GetComponentInChildren<ClimbAI>();
            
        }
        private void OnAnimatorMove()
        {
            if (aiCharacter.UseRootMotion && parkourAI.ControlledByParkour)
            {
                if (animator.deltaPosition != Vector3.zero)
                {
                    aiCharacter.transform.position += animator.deltaPosition;
                }
                aiCharacter.transform.rotation *= animator.deltaRotation;
            }
        }
        private void OnAnimatorIK(int layerIndex)
        {
            if (parkourAI.ControlledByParkour)
            {
                climbAI.OnAnimatorIK(layerIndex);
            }
        }
#endif
    }
}
