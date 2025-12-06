using FS_Core;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class RotateObject : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up * Time.deltaTime * 100f);
        }
    }
}