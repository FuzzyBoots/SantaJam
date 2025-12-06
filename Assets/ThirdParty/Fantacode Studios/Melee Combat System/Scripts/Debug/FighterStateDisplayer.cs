using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FS_CombatCore;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FS_CombatSystem
{
    public class FighterStateDisplayer : MonoBehaviour
    {
        FighterCore fighter;
        private void Start()
        {
            fighter = GetComponent<FighterCore>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (fighter == null) return;

            var style = new GUIStyle() { fontSize = 20 };
            Handles.Label(transform.position + Vector3.up * 2, fighter.Action.ToString(), style);
        }
#endif
    }
}
