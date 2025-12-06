using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_CombatCore
{
    [CustomIcon(FolderPath.DataIcons + "Death Icon.png")]
    [Icon(FolderPath.DataIcons + "Death Icon.png")]
    [CreateAssetMenu(menuName = "Combat/Create Death Reactions")]
    public class DeathReactionsData : ScriptableObject
    {
        [Tooltip("List of death reactions for handling death animations for different scenarios.")]
        public List<ReactionContainer> reactions = new List<ReactionContainer>();

        public bool HasReactionsForType(HitType hitType)
        {
            return reactions.Any(r => r.hitType == hitType || r.hitType == HitType.Any);
        }
    }
}
