// VRCP_UdonManager
// Created by CyanLaser

#if UDON

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_UdonManager : MonoBehaviour, VRCP_SDKManager
    {
        private static VRCP_UdonManager instance_;

        private HashSet<UdonBehaviour> allUdonBehaviours_ = new HashSet<UdonBehaviour>();

        private void Awake()
        {
            if (instance_ != null)
            {
                this.LogError("Already have an instance of VRCP_UdonManager!");
                DestroyImmediate(this);
                return;
            }

            instance_ = this;
        }

        private void Start()
        {
            AddObjectAndChildrenToBlackList(transform, UdonManager.Instance);
        }

        private void AddObjectAndChildrenToBlackList(Transform obj, UdonManager udonManager)
        {
            udonManager.Blacklist(obj.gameObject);

            for (int child = 0; child < obj.childCount; ++child)
            {
                AddObjectAndChildrenToBlackList(obj.GetChild(child), udonManager);
            }
        }

        #region VRCP_SDKManager

        public void OnNetworkReady()
        {
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                udonBehavior.GetComponent<VRCP_UdonHelper>().OnNetworkReady();
            }
        }

        public void OnPlayerJoined(VRCPlayerApi player)
        {
            AddObjectAndChildrenToBlackList(player.gameObject.transform, UdonManager.Instance);

            foreach (var udonBehavior in allUdonBehaviours_)
            {
                udonBehavior.RunEvent("_onPlayerJoined", ("player", player));
            }
        }

        public void OnPlayerLeft(VRCPlayerApi player)
        {
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                udonBehavior.RunEvent("_onPlayerLeft", ("player", player));
            }
        }

        public void OnSpawnedObject(GameObject spawnedObject)
        {
            UdonBehaviour[] udonBehaviours = spawnedObject.GetComponentsInChildren<UdonBehaviour>();
            foreach (var udonBehaviour in udonBehaviours)
            {
                udonBehaviour.RunEvent("_onSpawn");
            }
        }

        #endregion

        public static void AddUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allUdonBehaviours_.Add(udonBehaviour);
        }

        public static void RemoveUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allUdonBehaviours_.Remove(udonBehaviour);
        }
    }
}
#endif