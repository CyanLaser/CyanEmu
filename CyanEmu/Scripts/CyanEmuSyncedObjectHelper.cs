using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public abstract class CyanEmuSyncedObjectHelper : MonoBehaviour, ICyanEmuSyncable, ICyanEmuRespawnable
    {
        private int ownerID_ = 1;

        private Vector3 originalPosition_;
        private Quaternion originalRotation_;

        private Rigidbody rigidbody_;

        public bool SyncPosition { get; protected set; }

        protected virtual void Awake()
        {
            originalPosition_ = transform.position;
            originalRotation_ = transform.rotation;
            rigidbody_ = GetComponent<Rigidbody>();
            
            CyanEmuMain.AddSyncedObject(this);
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            this.Log("Teleporting Object " + VRC.Tools.GetGameObjectPath(gameObject) + " to " + position + " and rotation " + rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void OnDestroy()
        {
            CyanEmuMain.RemoveSyncedObject(this);
        }

        #region ICyanEmuSyncable

        public int GetOwner()
        {
            return ownerID_;
        }

        public void SetOwner(int ownerID)
        {
            ownerID_ = ownerID;
        }

        #endregion

        #region ICyanEmuRespawnable

        public void Respawn()
        {
            this.Log("Respawning Object " + VRC.Tools.GetGameObjectPath(gameObject));
            TeleportTo(originalPosition_, originalRotation_);
            
            if (rigidbody_ != null)
            {
                rigidbody_.velocity = Vector3.zero;
            }
        }

        #endregion
    }
}