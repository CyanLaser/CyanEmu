// VRCP_SpawnHelper
// Created by CyanLaser

using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_SpawnHelper : MonoBehaviour
    {
        public void ReapObject()
        {
            this.Log("Reaping Object " + name);
            Destroy(gameObject);
        }
    }
}