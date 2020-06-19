// VRCP_UdonHelper
// Created by CyanLaser

#if UDON

using System.Reflection;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Security;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_UdonHelper : VRCP_SyncedObjectHelper, VRCP_Interactable, VRCP_Pickupable, VRCP_StationHandler
    {
        // TODO: have an array of udon behaviours
        private UdonBehaviour udonbehaviour_;

        private static FieldInfo isNetworkReady = typeof(UdonBehaviour).GetField("_isNetworkReady", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static FieldInfo programFieldInfo = typeof(UdonBehaviour).GetField("program", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static FieldInfo udonVMFieldInfo = typeof(UdonBehaviour).GetField("_udonVM", (BindingFlags.Instance | BindingFlags.NonPublic));

        public static void OnInit(UdonBehaviour behaviour, IUdonProgram program)
        {
			if (behaviour.gameObject.GetComponent<VRCP_UdonHelper>() != null) {
				Debug.Log("Duplicate Udon Helper. This isn't fully supported yet.");
				return;
			}

            VRCP_UdonHelper helper = behaviour.gameObject.AddComponent<VRCP_UdonHelper>();
            helper.SetUdonbehaviour(behaviour);
            
            isNetworkReady.SetValue(behaviour, VRCP_CyanEmuMain.IsNetworkReady());
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
            if (GetComponents<UdonBehaviour>().Length > 1)
            {
                this.LogError("Object contains more than one UdonBehaviour component! " + VRCP_Utils.PathForObject(gameObject));
            }

            if (udonbehaviour == null)
            {
                this.LogError("UdonBehaviour is null. Destroying helper.");
                DestroyImmediate(this);
                return;
            }
            udonbehaviour_ = udonbehaviour;

            VRCP_UdonManager.AddUdonBehaviour(udonbehaviour_);
        }

        public UdonBehaviour GetUdonBehaviour()
        {
            return udonbehaviour_;
        }

        private void OnDestroy()
        {
            VRCP_UdonManager.RemoveUdonBehaviour(udonbehaviour_);
        }

        #region VRCP_Interactable

        public bool CanInteract(float distance)
        {
            return udonbehaviour_.HasInteractiveEvents && distance <= udonbehaviour_.proximity;
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

        #region VRCP_Pickupable

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

        #region VRCP_StationHandler

        public void OnStationEnter(VRCStation station)
        {
            VRC.SDK3.Components.VRCStation sdk3Station = station as VRC.SDK3.Components.VRCStation;
            udonbehaviour_.RunEvent(sdk3Station.OnLocalPlayerEnterStation);
        }

        public void OnStationExit(VRCStation station)
        {
            VRC.SDK3.Components.VRCStation sdk3Station = station as VRC.SDK3.Components.VRCStation;
            udonbehaviour_.RunEvent(sdk3Station.OnLocalPlayerExitStation);
        }

        #endregion
    }
}
#endif