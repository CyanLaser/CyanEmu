namespace VRCPrefabs.CyanEmu
{
    public interface ICyanEmuPickupable
    {
        void OnPickup();
        void OnDrop();
        void OnPickupUseDown();
        void OnPickupUseUp();
    }
}