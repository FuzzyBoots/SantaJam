using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FS_CombatCore;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FS_CombatSystem
{
    public class EnemyStateDisplayer : MonoBehaviour
    {
        CombatAIController enemy;
        private void Start()
        {
            enemy = GetComponent<CombatAIController>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (enemy == null) return;

            var style = new GUIStyle() { fontSize = 20 };
            Handles.Label(transform.position + Vector3.up * 2, enemy.CurrentState.ToString(), style);
        }
#endif
    }
}
