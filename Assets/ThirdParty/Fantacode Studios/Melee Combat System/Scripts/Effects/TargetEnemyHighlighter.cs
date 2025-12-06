using FS_CombatCore;
using System.Collections.Generic;
using UnityEngine;

namespace FS_CombatSystem
{
    public class TargetEnemyHighlighter : MonoBehaviour
    {
        [SerializeField] List<MeshData> meshesToHiglight;

        CombatAIController enemy;
        private void Awake()
        {
            enemy = GetComponent<CombatAIController>();
        }

        private void Start()
        {
            enemy.OnSelectedAsTarget += () => HighlightMesh(true);
            enemy.OnRemovedAsTarget += () => HighlightMesh(false);
        }

        void HighlightMesh(bool higlight)
        {
            if (meshesToHiglight == null || meshesToHiglight.Count == 0) return;

            foreach (var meshData in meshesToHiglight)
            {
                if (meshData.highlightedMaterials.Length == 0 || meshData.originalMaterials.Length == 0) return;
                meshData.mesh.materials = (higlight) ? meshData.highlightedMaterials : meshData.originalMaterials;
            }
        }
    }

    [System.Serializable]
    public class MeshData
    {
        public SkinnedMeshRenderer mesh;
        public Material[] originalMaterials;
        public Material[] highlightedMaterials;
    }
}
