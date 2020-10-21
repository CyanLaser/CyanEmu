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
        private UdonBehaviour udonbehaviour_;

        private static FieldInfo isNetworkReady = typeof(UdonBehaviour).GetField("_isNetworkReady", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static FieldInfo programFieldInfo = typeof(UdonBehaviour).GetField("program", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static FieldInfo udonVMFieldInfo = typeof(UdonBehaviour).GetField("_udonVM", (BindingFlags.Instance | BindingFlags.NonPublic));

        public static void OnInit(UdonBehaviour behaviour, IUdonProgram program)
        {
            CyanEmuUdonHelper helper = behaviour.gameObject.AddComponent<CyanEmuUdonHelper>();
            helper.SetUdonbehaviour(behaviour);
            
            isNetworkReady.SetValue(behaviour, CyanEmuMain.IsNetworkReady());
        }

        public void OnNetworkReady()
        {
            isNetworkReady.SetValue(udonbehaviour_, true);
        }

        public static void SendCustomNetworkEventHook(UdonBehaviour behaviour, NetworkEventTarget target, string eventName)
        {
            if (target == NetworkEventTarget.All || (target == NetworkEventTarget.Owner && Networking.IsOwner(behaviour.gameObject)))
            {
			    Debug.Log("Sending Network Event! eventName:" + eventName +", obj:" +VRC.Tools.GetGameObjectPath(behaviour.gameObject));
                behaviour.SendCustomEvent(eventName);
            }
            else
            {
                Debug.Log("Did not send custom network event " +eventName +" for object at "+ VRC.Tools.GetGameObjectPath(behaviour.gameObject));
            }
        }

        private void SetUdonbehaviour(UdonBehaviour udonbehaviour)
        {
            if (udonbehaviour == null)
            {
                this.LogError("UdonBehaviour is null. Destroying helper.");
                DestroyImmediate(this);
                return;
            }
            udonbehaviour_ = udonbehaviour;

            CyanEmuUdonManager.AddUdonBehaviour(udonbehaviour_);
        }

        public UdonBehaviour GetUdonBehaviour()
        {
            return udonbehaviour_;
        }

        private void OnDestroy()
        {
            CyanEmuUdonManager.RemoveUdonBehaviour(udonbehaviour_);
        }

        #region ICyanEmuSyncableHandler

        public void OnOwnershipTransferred(int ownerID)
        {
            udonbehaviour_.RunEvent("_onOwnershipTransferred", ("Player", VRCPlayerApi.GetPlayerById(ownerID)));
        }

        #endregion

        #region ICyanEmuInteractable

        public float GetProximity()
        {
            return udonbehaviour_.proximity;
        }

        public bool CanInteract()
        {
            return udonbehaviour_.HasInteractiveEvents;
        }

        public string GetInteractText()
        {
            return udonbehaviour_.interactText;
        }

        public void Interact()
        {
            udonbehaviour_.Interact();
        }

        #endregion

        #region ICyanEmuPickupable

        public void OnPickup()
        {
            udonbehaviour_.OnPickup();
        }

        public void OnDrop()
        {
            udonbehaviour_.OnDrop();
        }

        public void OnPickupUseDown()
        {
            udonbehaviour_.OnPickupUseDown();
        }

        public void OnPickupUseUp()
        {
            udonbehaviour_.OnPickupUseUp();
        }

        #endregion

        #region ICyanEmuStationHandler

        public void OnStationEnter(VRCStation station)
        {
            VRC.SDK3.Components.VRCStation sdk3Station = station as VRC.SDK3.Components.VRCStation;
            udonbehaviour_.RunEvent(sdk3Station.OnLocalPlayerEnterStation, ("Player", Networking.LocalPlayer));
        }

        public void OnStationExit(VRCStation station)
        {
            VRC.SDK3.Components.VRCStation sdk3Station = station as VRC.SDK3.Components.VRCStation;
            udonbehaviour_.RunEvent(sdk3Station.OnLocalPlayerExitStation, ("Player", Networking.LocalPlayer));
        }

        #endregion
    }
}
#endif