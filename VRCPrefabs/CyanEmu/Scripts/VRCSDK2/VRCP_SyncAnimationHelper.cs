// VRCP_SyncAnimationHelper
// Created by CyanLaser

#if VRC_SDK_VRCSDK2

using UnityEngine;
using VRCSDK2;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_SyncAnimationHelper : MonoBehaviour
    {
        public static void InitializationDelegate(VRC_SyncAnimation obj)
        {
            obj.gameObject.AddComponent<VRCP_SyncAnimationHelper>().SetSyncAnimation(obj);
        }

        private VRC_SyncAnimation sync_;

        private void SetSyncAnimation(VRC_SyncAnimation sync)
        {
            sync_ = sync;

            this.Log("Syncing animations on object. " + VRC.Tools.GetGameObjectPath(gameObject));
        }
    }
}

#endif
