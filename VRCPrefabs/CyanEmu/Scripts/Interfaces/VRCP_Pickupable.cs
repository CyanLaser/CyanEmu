// VRCP_Pickupable
// Created by CyanLaser

namespace VRCPrefabs.CyanEmu
{
    public interface VRCP_Pickupable
    {
        void OnPickup();
        void OnDrop();
        void OnPickupUseDown();
        void OnPickupUseUp();
    }
}