using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

#if UDON
using VRC.Udon;
using VRC.Udon.Common;
#endif

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuStationHelper : MonoBehaviour, ICyanEmuStationHandler
    {
        private VRCStation station_;
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
                return 
                    station_.PlayerMobility == VRCStation.Mobility.Mobile &&
                    !station_.seated;
            }
        }

        public static void InitializeStations(VRCStation station)
        {
            CyanEmuStationHelper prevHelper = station.gameObject.GetComponent<CyanEmuStationHelper>();
            if (prevHelper != null)
            {
                DestroyImmediate(prevHelper);
                station.LogWarning("Destroying old station helper on object: " + VRC.Tools.GetGameObjectPath(station.gameObject));
            }

            station.gameObject.AddComponent<CyanEmuStationHelper>();

            if (!station.seated && station.PlayerMobility != VRCStation.Mobility.Mobile)
            {
                station.LogWarning("Station has seated unchecked but is not mobile! " + VRC.Tools.GetGameObjectPath(station.gameObject));
            }
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

            CheckForMissingComponents();

            if (station_.stationEnterPlayerLocation == null)
            {
                station_.stationEnterPlayerLocation = transform;
            }
            if (station_.stationExitPlayerLocation == null)
            {
                station_.stationExitPlayerLocation = transform;
            }
        }

        private void CheckForMissingComponents()
        {
            Collider stationCollider = GetComponent<Collider>();
            if (stationCollider == null)
            {
                gameObject.AddComponent<BoxCollider>().isTrigger = true;
            }

#if UDON && UNITY_EDITOR
            UdonBehaviour udon = GetComponent<UdonBehaviour>();
            if (udon == null)
            {
                udon = gameObject.AddComponent<UdonBehaviour>();
                udon.interactText = "Sit";
                AbstractUdonProgramSource program = UnityEditor.AssetDatabase.LoadAssetAtPath<AbstractUdonProgramSource>("Assets/VRChat Examples/Prefabs/VRCChair/StationGraph.asset");
                if (program != null)
                {
                    udon.AssignProgramAndVariables(program.SerializedProgramAsset, new UdonVariableTable());
                }
            }
#endif

#if VRC_SDK_VRCSDK2
            // Auto add a Interact Trigger to use the station
            VRC_Trigger trigger = GetComponent<VRC_Trigger>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<VRCSDK2.VRC_Trigger>();
                trigger.Triggers = new List<VRC_Trigger.TriggerEvent>();
                trigger.interactText = "Sit";

                VRC_Trigger.TriggerEvent onInteract = new VRC_Trigger.TriggerEvent
                {
                    BroadcastType = VRC_EventHandler.VrcBroadcastType.Local,
                    TriggerType = VRC_Trigger.TriggerType.OnInteract,
                    Events = new List<VRC_EventHandler.VrcEvent>()
                };

                VRC_EventHandler.VrcEvent useStationEvent = new VRC_EventHandler.VrcEvent
                {
                    EventType = VRC_EventHandler.VrcEventType.SendRPC,
                    ParameterString = "UseStation",
                    ParameterObjects = new[] {gameObject},
                    ParameterInt = 6,
                };
                
                onInteract.Events.Add(useStationEvent);
                trigger.Triggers.Add(onInteract);

                // Reinitialize the trigger now that it has the proper events added.
                // Note that this only works as there were no vrc triggers on this object before.
                CyanEmuTriggerHelper helper = GetComponent<CyanEmuTriggerHelper>();
                if (helper != null)
                {
                    DestroyImmediate(helper);
                }
                CyanEmuTriggerHelper.InitializeTrigger(trigger);
            }
#endif
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

            gameObject.OnStationEnter(station_);

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

            gameObject.OnStationExit(station_);

            this.Log("Exiting Station " + name);
        }

        // Returns if should move
        public bool CanPlayerMoveWhileSeated(float speed)
        {
            if (Mathf.Abs(speed) >= 0.1f && !station_.disableStationExit)
            {
                ExitStation();
                return true;
            }

            if (IsMobile)
            {
                return true;
            }

            return false;
        }

        public void UpdatePlayerPosition(CyanEmuPlayerController player)
        {
            if (IsMobile)
            {
                return;
            }

            player.SitPosition(EnterLocation);
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