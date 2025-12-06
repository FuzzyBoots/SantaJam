using FS_ThirdPerson;
using Unity.AI.Navigation;
using UnityEngine;
namespace FS_ParkourAI
{

    [Icon(FolderPath.ParkourAI + "Scripts/Editor/Gizmos/NavMeshSurface Icon.png")]
    [AddComponentMenu("NavMeshSurfaceBaker")]
    public class CustomNavMeshSurface : NavMeshSurface
    {
        public bool bakeParkourLinks = true;

        public float parkourHeight = 3f;

    }
}