// VRCP_SDKManager
// Created by CyanLaser

using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public interface VRCP_SDKManager
    {
        void OnNetworkReady();
        void OnPlayerJoined(VRCPlayerApi player);
        void OnPlayerLeft(VRCPlayerApi player);
        void OnSpawnedObject(GameObject spawnedObject);
    }
}