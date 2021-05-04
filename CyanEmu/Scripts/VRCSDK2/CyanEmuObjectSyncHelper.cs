#if VRC_SDK_VRCSDK2

using UnityEngine;
using VRCSDK2;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuObjectSyncHelper : CyanEmuSyncedObjectHelper, ICyanEmuSyncableHandler
    {
        private VRC_ObjectSync sync_;

        public static void InitializeObjectSync(VRC_ObjectSync sync)
        {
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

        protected override void Awake()
        {
            base.Awake();
            SyncPosition = true;

            sync_ = GetComponent<VRC_ObjectSync>();

            if ((GetComponent<Animator>() != null || GetComponent<Animation>() != null) && GetComponent<VRC_SyncAnimation>() == null)
            {
                gameObject.AddComponent<VRC_SyncAnimation>();
                this.LogWarning("Object sync has animtor or animation component but no Sync Animation component. This will be forced synced!");
            }
        }

        #region ICyanEmuSyncableHandler

        public void OnOwnershipTransferred(int ownerID)
        {
            VRC_Trigger.Trigger(gameObject, VRC.SDKBase.VRC_Trigger.TriggerType.OnOwnershipTransfer); 
        }

        #endregion

        public void EnableKinematic()
        {
            this.Log("Enabling kinematic on Object " + VRC.Tools.GetGameObjectPath(gameObject));
        }

        public void DisableKinematic()
        {
            this.Log("Disabling kinematic on Object " + VRC.Tools.GetGameObjectPath(gameObject));
        }

        public void EnableGravity()
        {
            this.Log("Enabling gravity on Object " + VRC.Tools.GetGameObjectPath(gameObject));
        }

        public void DisableGravity()
        {
            this.Log("Disabling gravity on Object " + VRC.Tools.GetGameObjectPath(gameObject));
        }
    }
}

#endif