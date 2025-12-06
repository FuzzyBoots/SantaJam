using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class LayersUtil
    {
        public static bool LayerMaskContainsLayer(LayerMask layerMask, int layer)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
    }
}
