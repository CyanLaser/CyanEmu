#if UDON

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuUdonManager : MonoBehaviour, ICyanEmuSDKManager
    {
        private static CyanEmuUdonManager instance_;

        private HashSet<UdonBehaviour> allUdonBehaviours_ = new HashSet<UdonBehaviour>();

        private void Awake()
        {
            if (instance_ != null)
            {
                this.LogError("Already have an instance of CyanEmuUdonManager!");
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

        #region ICyanEmuSDKManager

        public void OnNetworkReady()
        {
            HashSet<GameObject> objs = new HashSet<GameObject>();
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                if (objs.Contains(udonBehavior.gameObject))
                {
                    continue;
                }
                objs.Add(udonBehavior.gameObject);

                foreach (var helper in udonBehavior.GetComponents<CyanEmuUdonHelper>())
                {
                    try
                    {
                        helper.OnNetworkReady();
                    }
                    catch (Exception e)
                    {
                        this.LogError(e.Message + "\n" +e.StackTrace);
                        this.LogWarning("Failed to send network ready for object: " +VRC.Tools.GetGameObjectPath(helper.gameObject));
                    }
                }
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

        public void OnPlayerRespawn(VRCPlayerApi player)
        {
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                udonBehavior.RunEvent("_onPlayerRespawn", ("player", player));
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