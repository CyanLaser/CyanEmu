#if UDON

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuUdonManager : MonoBehaviour, ICyanEmuSDKManager
    {
        private static CyanEmuUdonManager instance_;

        private bool shouldVerifyUdonBehaviours_;
        private readonly Queue<UdonBehaviour> toBeAddedUdon_ = new Queue<UdonBehaviour>();
        private readonly Queue<UdonBehaviour> toBeRemovedUdon_ = new Queue<UdonBehaviour>();
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

        private void LateUpdate()
        {
            ProcessAddedAndRemovedUdonBehaviours();

            enabled = false;
        }

        #region ICyanEmuSDKManager

        public void OnNetworkReady()
        {
            HashSet<GameObject> objs = new HashSet<GameObject>();
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                if (udonBehavior == null || objs.Contains(udonBehavior.gameObject))
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
                if (udonBehavior == null)
                {
                    continue;
                }
                udonBehavior.RunEvent("_onPlayerJoined", ("player", player));
            }
        }

        public void OnPlayerLeft(VRCPlayerApi player)
        {
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                if (udonBehavior == null)
                {
                    continue;
                }
                udonBehavior.RunEvent("_onPlayerLeft", ("player", player));
            }
        }

        public void OnPlayerRespawn(VRCPlayerApi player)
        {
            foreach (var udonBehavior in allUdonBehaviours_)
            {
                if (udonBehavior == null)
                {
                    continue;
                }
                udonBehavior.RunEvent("_onPlayerRespawn", ("player", player));
            }
        }

        public void OnSpawnedObject(GameObject spawnedObject)
        {
            // TODO fix this
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

            instance_.QueueAddUdonBehaviour(udonBehaviour);
        }

        public static void RemoveUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.QueueRemoveUdonBehaviour(udonBehaviour);
        }
        
        private void QueueAddUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            enabled = true;
            toBeAddedUdon_.Enqueue(udonBehaviour);
        }
        
        private void QueueRemoveUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            enabled = true;
            shouldVerifyUdonBehaviours_ = true;
            toBeRemovedUdon_.Enqueue(udonBehaviour);
        }

        private void ProcessAddedAndRemovedUdonBehaviours()
        {
            if (toBeAddedUdon_.Count > 0)
            {
                foreach (var udon in toBeAddedUdon_)
                {
                    if (udon == null)
                    {
                        shouldVerifyUdonBehaviours_ = true;
                        continue;
                    }
                    allUdonBehaviours_.Add(udon);
                }
                toBeAddedUdon_.Clear();
            }
            if (toBeRemovedUdon_.Count > 0)
            {
                foreach (var udon in toBeRemovedUdon_)
                {
                    if (udon == null)
                    {
                        shouldVerifyUdonBehaviours_ = true;
                        continue;
                    }
                    allUdonBehaviours_.Remove(udon);
                }
                toBeRemovedUdon_.Clear();
            }

            if (shouldVerifyUdonBehaviours_)
            {
                HashSet<UdonBehaviour> allUdon = new HashSet<UdonBehaviour>();
                foreach (var udon in allUdonBehaviours_)
                {
                    if (udon == null)
                    {
                        continue;
                    }
                    allUdon.Add(udon);
                }

                allUdonBehaviours_ = allUdon;
            }
        }
    }
}
#endif