using UnityEngine;

// Reusing code for ObjectSync, but VRC does not have them related at all.
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#elif UDON && VRC_SDK_VRCSDK3
using VRC_ObjectSync = VRC.SDK3.Components.VRCObjectSync;
#else
// In some cases, it is possible for the sdk items to cause this to fail to compile.
// Add a proxy here to not have code break.
// This is a potential fix for the issue that causes CyanEmu to break on switching to android
using VRC_ObjectSync = VRCPrefabs.CyanEmu.CyanEmuObjectSync;
namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuObjectSync : MonoBehaviour {}
}
#endif

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuObjectSyncHelper : CyanEmuSyncedObjectHelper, ICyanEmuSyncableHandler
    {
        private VRC_ObjectSync sync_;

        private Rigidbody rigidbody_;

        public static void InitializeObjectSync(VRC_ObjectSync sync)
        {
            var helper = sync.GetComponent<CyanEmuObjectSyncHelper>();
            if (helper)
            {
                DestroyImmediate(helper);
            }
            
            sync.gameObject.AddComponent<CyanEmuObjectSyncHelper>();
        }

        public static void TeleportTo(VRC_ObjectSync obj, Vector3 position, Quaternion rotation)
        {
            obj.GetComponent<CyanEmuObjectSyncHelper>().TeleportTo(position, rotation);
        }

        public static void RespawnObject(VRC_ObjectSync sync)
        {
            sync.GetComponent<CyanEmuObjectSyncHelper>().Respawn();
        }
        
        public static void SetIsKinematic(VRC_ObjectSync sync, bool value)
        {
            sync.GetComponent<CyanEmuObjectSyncHelper>().SetIsKinematic(value);
        }
        
        public static void SetUseGravity(VRC_ObjectSync sync, bool value)
        {
            sync.GetComponent<CyanEmuObjectSyncHelper>().SetUseGravity(value);
        }
        
        public static bool GetIsKinematic(VRC_ObjectSync sync)
        {
            return sync.GetComponent<CyanEmuObjectSyncHelper>().GetIsKinematic();
        }
        
        public static bool GetUseGravity(VRC_ObjectSync sync)
        {
            return sync.GetComponent<CyanEmuObjectSyncHelper>().GetUseGravity();
        }
        
        public static void FlagDiscontinuityHook(VRC_ObjectSync sync)
        {
            sync.GetComponent<CyanEmuObjectSyncHelper>().FlagDiscontinuity();
        }

        protected override void Awake()
        {
            base.Awake();
            SyncPosition = true;

            rigidbody_ = GetComponent<Rigidbody>();
            sync_ = GetComponent<VRC_ObjectSync>();
            
#if VRC_SDK_VRCSDK2 
            if ((GetComponent<Animator>() != null || GetComponent<Animation>() != null) && GetComponent<VRC_SyncAnimation>() == null)
            {
                gameObject.AddComponent<VRC_SyncAnimation>();
                this.LogWarning("Object sync has animtor or animation component but no Sync Animation component. This will be forced synced!");
            }
#endif
        }

        #region ICyanEmuSyncableHandler

        public void OnOwnershipTransferred(int ownerID)
        {
#if VRC_SDK_VRCSDK2
            VRC_Trigger.Trigger(gameObject, VRC.SDKBase.VRC_Trigger.TriggerType.OnOwnershipTransfer);
#endif
        }

        #endregion

        #region legacy RPC methods

        public void EnableKinematic()
        {
            this.Log("Enabling kinematic on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            SetIsKinematic(true);
        }

        public void DisableKinematic()
        {
            this.Log("Disabling kinematic on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            SetIsKinematic(false);
        }

        public void EnableGravity()
        {
            this.Log("Enabling gravity on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            SetUseGravity(true);
        }

        public void DisableGravity()
        {
            this.Log("Disabling gravity on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            SetUseGravity(false);
        }

        #endregion
        

        private void SetIsKinematic(bool value)
        {
            if (rigidbody_)
            {
                rigidbody_.isKinematic = value;
            }
        }
        
        private void SetUseGravity(bool value)
        {
            if (rigidbody_)
            {
                rigidbody_.useGravity = value;
            }
        }
        
        private bool GetIsKinematic()
        {
            return rigidbody_ && rigidbody_.isKinematic;
        }
        
        private bool GetUseGravity()
        {
            return rigidbody_ && rigidbody_.useGravity;
        }
    }
}