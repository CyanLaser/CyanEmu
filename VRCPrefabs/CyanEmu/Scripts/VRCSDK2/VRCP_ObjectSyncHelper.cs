// VRCP_ObjectSyncHelper
// Created by CyanLaser

#if VRC_SDK_VRCSDK2

using UnityEngine;
using VRCSDK2;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_ObjectSyncHelper : VRCP_SyncedObjectHelper
    {
        private VRC_ObjectSync sync_;
        private VRCP_PickupHelper pickupHelper_;


        public static void InitializeObjectSync(VRC_ObjectSync sync)
        {
            sync.gameObject.AddComponent<VRCP_ObjectSyncHelper>();
        }

        public static void TeleportTo(VRC_ObjectSync obj, Vector3 position, Quaternion rotation)
        {
            obj.GetComponent<VRCP_ObjectSyncHelper>().TeleportTo(position, rotation);
        }

        public static void RespawnObject(VRC_ObjectSync sync)
        {
            sync.GetComponent<VRCP_ObjectSyncHelper>().Respawn();
        }

        protected override void Awake()
        {
            base.Awake();

            sync_ = GetComponent<VRC_ObjectSync>();
            pickupHelper_ = GetComponent<VRCP_PickupHelper>();

            if ((GetComponent<Animator>() != null || GetComponent<Animation>() != null) && GetComponent<VRC_SyncAnimation>() == null)
            {
                gameObject.AddComponent<VRC_SyncAnimation>();
                this.LogWarning("Object sync has animtor or animation component but no Sync Animation component. This will be forced synced!");
            }
        }

        public void EnableKinematic()
        {
            this.Log("Enabling kinematic on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            if (pickupHelper_)
            {
                pickupHelper_.SetKinematic(true);
            }
        }

        public void DisableKinematic()
        {
            this.Log("Disabling kinematic on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            if (pickupHelper_)
            {
                pickupHelper_.SetKinematic(false);
            }
        }

        public void EnableGravity()
        {
            this.Log("Enabling gravity on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            if (pickupHelper_)
            {
                pickupHelper_.SetGravity(true);
            }
        }

        public void DisableGravity()
        {
            this.Log("Disabling gravity on Object " + VRC.Tools.GetGameObjectPath(gameObject));
            if (pickupHelper_)
            {
                pickupHelper_.SetGravity(false);
            }
        }
    }
}

#endif