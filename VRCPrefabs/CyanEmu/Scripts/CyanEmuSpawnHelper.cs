using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuSpawnHelper : MonoBehaviour
    {
        public void ReapObject()
        {
            this.Log("Reaping Object " + name);
            Destroy(gameObject);
        }
    }
}