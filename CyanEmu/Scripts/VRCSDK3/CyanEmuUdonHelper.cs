#if UDON

using System.Reflection;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuUdonHelper : CyanEmuSyncedObjectHelper, ICyanEmuInteractable, ICyanEmuPickupable, ICyanEmuStationHandler, ICyanEmuSyncableHandler
    {
        // VRCSDK3-2021.01.28.19.07 modified the name of the _isNetworkReady variable to _isReady.
        // Check both for backwards compatibility so I don't require users to update their old sdks.
        private static FieldInfo isNetworkReady_ = 
            typeof(UdonBehaviour).GetField("_isNetworkReady", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static FieldInfo isReady_ = 
            typeof(UdonBehaviour).GetField("_isReady", (BindingFlags.Instance | BindingFlags.NonPublic));

        private static FieldInfo NetworkReadyFieldInfo_ => isNetworkReady_ ?? isReady_;
        
        private UdonBehaviour udonBehaviour_;

        public static void OnInit(UdonBehaviour behaviour, IUdonProgram program)
        {
            CyanEmuUdonHelper helper = behaviour.gameObject.AddComponent<CyanEmuUdonHelper>();
            helper.SetUdonBehaviour(behaviour);

            NetworkReadyFieldInfo_.SetValue(behaviour, CyanEmuMain.IsNetworkReady());
        }

        public void OnNetworkReady()
        {
            NetworkReadyFieldInfo_.SetValue(udonBehaviour_, true);
        }

        private void Start()
        {
            if (udonBehaviour_ == null)
            {
                DestroyImmediate(this);
            }
        }

        public static void SendCustomNetworkEventHook(UdonBehaviour behaviour, NetworkEventTarget target, string eventName)
        {
            if (target == NetworkEventTarget.All || (target == NetworkEventTarget.Owner && Networking.IsOwner(behaviour.gameObject)))
            {
                behaviour.Log("Sending Network Event! eventName:" + eventName +", obj:" +VRC.Tools.GetGameObjectPath(behaviour.gameObject));
                behaviour.SendCustomEvent(eventName);
            }
            else
            {
                behaviour.Log("Did not send custom network event " +eventName +" for object at "+ VRC.Tools.GetGameObjectPath(behaviour.gameObject));
            }
        }

        private void SetUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            if (udonBehaviour == null)
            {
                this.LogError("UdonBehaviour is null. Destroying helper.");
                DestroyImmediate(this);
                return;
            }
            udonBehaviour_ = udonBehaviour;
            SyncPosition = udonBehaviour_.SynchronizePosition;

            CyanEmuUdonManager.AddUdonBehaviour(udonBehaviour_);
        }

        public UdonBehaviour GetUdonBehaviour()
        {
            return udonBehaviour_;
        }

        private void OnDestroy()
        {
            CyanEmuUdonManager.RemoveUdonBehaviour(udonBehaviour_);
        }

        #region ICyanEmuSyncableHandler

        public void OnOwnershipTransferred(int ownerID)
        {
            udonBehaviour_.RunEvent("_onOwnershipTransferred", ("Player", VRCPlayerApi.GetPlayerById(ownerID)));
        }

        #endregion

        #region ICyanEmuInteractable

        public float GetProximity()
        {
            return udonBehaviour_.proximity;
        }

        public bool CanInteract()
        {
            return udonBehaviour_.IsInteractive;
        }

        public string GetInteractText()
        {
            return udonBehaviour_.interactText;
        }

        public void Interact()
        {
            udonBehaviour_.Interact();
        }

        #endregion

        #region ICyanEmuPickupable

        public void OnPickup()
        {
            udonBehaviour_.OnPickup();
        }

        public void OnDrop()
        {
            udonBehaviour_.OnDrop();
        }

        public void OnPickupUseDown()
        {
            udonBehaviour_.OnPickupUseDown();
        }

        public void OnPickupUseUp()
        {
            udonBehaviour_.OnPickupUseUp();
        }

        #endregion

        #region ICyanEmuStationHandler

        public void OnStationEnter(VRCStation station)
        {
            VRC.SDK3.Components.VRCStation sdk3Station = station as VRC.SDK3.Components.VRCStation;
            udonBehaviour_.RunEvent(sdk3Station.OnLocalPlayerEnterStation, ("Player", Networking.LocalPlayer));
        }

        public void OnStationExit(VRCStation station)
        {
            VRC.SDK3.Components.VRCStation sdk3Station = station as VRC.SDK3.Components.VRCStation;
            udonBehaviour_.RunEvent(sdk3Station.OnLocalPlayerExitStation, ("Player", Networking.LocalPlayer));
        }

        #endregion
    }
}
#endif