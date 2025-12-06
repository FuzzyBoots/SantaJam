#if UNITY_EDITOR
using UnityEditor;
#endif
using FS_CombatCore;
using UnityEngine;
namespace FS_CombatSystem
{

    public class TargetDistanceVisualizer : MonoBehaviour
    {
        CombatAIController combatAI;
        private void Awake()
        {
            combatAI = GetComponent<CombatAIController>();
        }
        private void Update()
        {

        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Handles.color = Color.black;
            if (combatAI?.Fighter?.Target != null)
            {
                Gizmos.DrawLine(combatAI.transform.position, combatAI.Fighter.Target.transform.position); 

                var disp = combatAI.Fighter.Target.transform.position - combatAI.transform.position;
                Handles.Label(combatAI.transform.position + disp / 2, "" + disp.magnitude);
            }
        }
#endif
    }
}