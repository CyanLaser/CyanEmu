using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuStationHelper : MonoBehaviour, ICyanEmuStationHandler
    {
        private VRCStation station_;
        private ICyanEmuStationHandler[] stationHandlers_;
        private bool entered_ = false;

        public Transform EnterLocation
        {
            get
            {
                return station_.stationEnterPlayerLocation;
            }
        }

        public Transform ExitLocation
        {
            get
            {
                return station_.stationExitPlayerLocation;
            }
        }

        public bool IsMobile
        {
            get
            {
                return station_.PlayerMobility == VRCStation.Mobility.Mobile;
            }
        }

        public static void InitializeStations(VRCStation station)
        {
            station.gameObject.AddComponent<CyanEmuStationHelper>();
        }

        public static void UseStation(VRCStation station, VRCPlayerApi player)
        {
            station.GetComponent<CyanEmuStationHelper>().UseStation();
        }

        public static void ExitStation(VRCStation station, VRCPlayerApi player)
        {
            station.GetComponent<CyanEmuStationHelper>().ExitStation();
        }

        private void Awake()
        {
            station_ = GetComponent<VRCStation>();

            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                gameObject.AddComponent<BoxCollider>().isTrigger = true;
            }

            if (station_.stationEnterPlayerLocation == null)
            {
                station_.stationEnterPlayerLocation = transform;
            }
            if (station_.stationExitPlayerLocation == null)
            {
                station_.stationExitPlayerLocation = transform;
            }
        }

        private void Start()
        {
            stationHandlers_ = GetComponents<ICyanEmuStationHandler>();
        }

        public void UseStation()
        {
            if (entered_)
            {
                return;
            }
            entered_ = true;

            if (CyanEmuPlayerController.instance != null)
            {
                CyanEmuPlayerController.instance.EnterStation(this);
            }
            
            foreach (var handler in stationHandlers_)
            {
                handler.OnStationEnter(station_);
            }

            this.Log("Entering Station " + name);
        }

        public void ExitStation()
        {
            if (!entered_) 
            {
                return;
            }
            entered_ = false;

            if (CyanEmuPlayerController.instance != null)
            {
                CyanEmuPlayerController.instance.ExitStation(this);
            }

            foreach (var handler in stationHandlers_)
            {
                handler.OnStationExit(station_);
            }

            this.Log("Exiting Station " + name);
        }

        // Returns if should move
        public bool UpdateSeat(float speed)
        {
            if (IsMobile)
            {
                return true;
            }

            if (CyanEmuPlayerController.instance != null)
            {
                CyanEmuPlayerController.instance.SitPosition(station_.stationEnterPlayerLocation);
            }

            if (Mathf.Abs(speed) >= 0.1f && !station_.disableStationExit)
            {
                ExitStation();
                return true;
            }
            return false;
        }

        public void OnStationEnter(VRCStation station)
        {
#if VRC_SDK_VRCSDK2
            VRCSDK2.VRC_Station s = (VRCSDK2.VRC_Station)station;
            if (s.OnLocalPlayerEnterStation.TriggerObject == null)
            {
                return;
            }

            VRC_Trigger trigger = s.OnLocalPlayerEnterStation.TriggerObject.GetComponent<VRC_Trigger>();
            if (trigger != null)
            {
                trigger.ExecuteCustomTrigger(s.OnLocalPlayerEnterStation.CustomName);
            }
#endif
        }

        public void OnStationExit(VRCStation station)
        {
#if VRC_SDK_VRCSDK2
            VRCSDK2.VRC_Station s = (VRCSDK2.VRC_Station)station;
            if (s.OnLocalPlayerExitStation.TriggerObject == null)
            {
                return;
            }

            VRC_Trigger trigger = s.OnLocalPlayerExitStation.TriggerObject.GetComponent<VRC_Trigger>();
            if (trigger != null)
            {
                trigger.ExecuteCustomTrigger(s.OnLocalPlayerExitStation.CustomName);
            }
#endif
        }
    }
}