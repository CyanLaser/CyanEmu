using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public interface ICyanEmuStationHandler
    {
        void OnStationEnter(VRCStation station);
        void OnStationExit(VRCStation station);
    }
}
