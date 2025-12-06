using FS_CombatCore;
using FS_Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_CombatCore
{
    public enum TriggerType { OnPlayerEnter, OnPlayerExit, OnStart}

    public class TriggerAIState : MonoBehaviour
    {
        [SerializeField] TriggerType triggerType;
        [SerializeField] List<CombatAIController> enemies;
        [SerializeField] AIStates stateToTrigger = AIStates.AuthoredBehavior;
        [SerializeField] bool setTarget = true;
        [SerializeField] FighterCore target;

        [Tooltip("If specified, triggering new state will only happen when the AI is in one of the given states")]
        [SerializeField] List<AIStates> requiredStatesForTriggering = new List<AIStates>();

        private void Start()
        {
            if (triggerType == TriggerType.OnStart)
                StartCoroutine(TriggerState(target));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType != TriggerType.OnPlayerEnter) return;

            if (other.tag == "Player")
                StartCoroutine(TriggerState(setTarget? other.GetComponent<FighterCore>() : null));
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerType != TriggerType.OnPlayerExit) return;

            if (other.tag == "Player")
                StartCoroutine(TriggerState(setTarget ? other.GetComponent<FighterCore>() : null));
        }

        IEnumerator TriggerState(FighterCore target=null)
        {
            foreach (var combatAI in enemies)
            {
                if (!combatAI.gameObject.activeSelf)
                {
                    combatAI.gameObject.SetActive(true);
                    yield return null;
                }

                if (combatAI.IsInState(AIStates.Dead)) continue;

                if (requiredStatesForTriggering.Count > 0)
                {
                    if (requiredStatesForTriggering.All(s => !combatAI.IsInState(s))) continue;
                }

                combatAI.ChangeState(stateToTrigger);

                if (setTarget && target != null)
                    combatAI.Fighter.Target = target;
            }

            Destroy(gameObject);

            yield break;
        }
    }
}
