// VRCP_StationHandler
// Created by CyanLaser

using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public interface VRCP_StationHandler
    {
        void OnStationEnter(VRCStation station);
        void OnStationExit(VRCStation station);
    }
}
