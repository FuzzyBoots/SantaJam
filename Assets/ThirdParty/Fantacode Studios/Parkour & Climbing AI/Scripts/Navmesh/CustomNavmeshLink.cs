using FS_ThirdPerson;
using UnityEngine;
namespace FS_ParkourAI
{

    [Icon(FolderPath.ParkourAI + "Scripts/Editor/Gizmos/NavMeshLink Icon.png")]
    [AddComponentMenu("NavMeshLink")]
    public class CustomNavmeshLink : Unity.AI.Navigation.NavMeshLink
    {
        [HideInInspector]
        public Collider startLedge;
        [HideInInspector]
        public Collider endLedge;

        [HideInInspector]
        public bool manualLedge;

        [HideInInspector]
        public bool validPoint = false;
    }
}
