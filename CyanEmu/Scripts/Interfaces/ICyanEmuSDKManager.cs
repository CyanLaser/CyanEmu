using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public interface ICyanEmuSDKManager
    {
        void OnNetworkReady();
        void OnPlayerJoined(VRCPlayerApi player);
        void OnPlayerLeft(VRCPlayerApi player);
        void OnPlayerRespawn(VRCPlayerApi player);
        void OnSpawnedObject(GameObject spawnedObject);
    }
}