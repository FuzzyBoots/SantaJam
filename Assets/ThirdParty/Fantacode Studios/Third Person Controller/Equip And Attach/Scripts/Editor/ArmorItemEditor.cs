using FS_Core;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_ThirdPerson
{
    [CustomEditor(typeof(ArmorItem))]
    public class ArmorItemEditor : ItemDataEditor
    {
        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}