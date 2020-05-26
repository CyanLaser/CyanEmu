// VRCP_SyncedObjectHelper
// Created by CyanLaser

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public abstract class VRCP_SyncedObjectHelper : MonoBehaviour, VRCP_Syncable, VRCP_Respawnable
    {
        private int ownerID_ = 1;

        private Vector3 originalPosition_;
        private Quaternion originalRotation_;

        protected virtual void Awake()
        {
            originalPosition_ = transform.position;
            originalRotation_ = transform.rotation;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            this.Log("Teleporting Object " + VRC.Tools.GetGameObjectPath(gameObject) + " to " + position + " and rotation " + rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        protected virtual void OnEnable()
        {
            VRCP_CyanEmuMain.AddSyncedObject(this);
        }

        protected virtual void OnDisable()
        {
            VRCP_CyanEmuMain.RemoveSyncedObject(this);
        }

        private void OnDestroy()
        {
            VRCP_CyanEmuMain.RemoveSyncedObject(this);
        }

        #region VRCP_Syncable

        public int GetOwner()
        {
            return ownerID_;
        }

        public void SetOwner(int ownerID)
        {
            ownerID_ = ownerID;
        }

        #endregion

        #region VRCP_Respawnable

        public void Respawn()
        {
            this.Log("Respawning Object " + VRC.Tools.GetGameObjectPath(gameObject));
            TeleportTo(originalPosition_, originalRotation_);
        }

        #endregion
    }
}