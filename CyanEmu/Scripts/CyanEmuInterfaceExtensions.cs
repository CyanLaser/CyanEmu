using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class CyanEmuInterfaceExtensions
    {
        #region ICyanEmuInteractable

        // Dumb numbers
        private const float INTERACT_OFFSET = 0.9865f;
        private const float INTERACT_SCALE = 1.2685f;

        public static float CalculateInteractDistanceFormula()
        {
            float camSize = CyanEmuPlayerController.instance.GetCameraScale();
            return camSize * INTERACT_SCALE + INTERACT_OFFSET;
        }

        public static ICyanEmuInteractable GetClosestInteractable(this GameObject obj, float distance)
        {
            ICyanEmuInteractable closest = null;
            float bestDistance = float.MaxValue;

            foreach (var interactable in obj.GetComponents<ICyanEmuInteractable>())
            {
                if (interactable.CanInteract(distance))
                {
                    closest = interactable;
                    bestDistance = interactable.GetProximity();
                }
            }

            return closest;
        }

        public static bool CanInteract(this ICyanEmuInteractable interactable, float distance)
        {
            float proximityCalculation = CalculateInteractDistanceFormula() * interactable.GetProximity();
            return interactable.CanInteract() && distance <= proximityCalculation;
        }

        public static bool CanInteract(this GameObject obj, float distance)
        {
            foreach (var interactable in obj.GetComponents<ICyanEmuInteractable>())
            {
                if (interactable.CanInteract(distance))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Interact(this GameObject obj, float distance)
        {
            foreach (var interactable in obj.GetComponents<ICyanEmuInteractable>())
            {
                if (interactable.CanInteract(distance))
                {
                    interactable.Interact();
                }
            }
        }

        #endregion

        #region ICyanEmuPickupable

        public static void OnPickup(this GameObject obj)
        {
            foreach (var pickupable in obj.GetComponents<ICyanEmuPickupable>())
            {
                pickupable.OnPickup();
            }
        }

        public static void OnDrop(this GameObject obj)
        {
            foreach (var pickupable in obj.GetComponents<ICyanEmuPickupable>())
            {
                pickupable.OnDrop();
            }
        }

        public static void OnPickupUseDown(this GameObject obj)
        {
            foreach (var pickupable in obj.GetComponents<ICyanEmuPickupable>())
            {
                pickupable.OnPickupUseDown();
            }
        }

        public static void OnPickupUseUp(this GameObject obj)
        {
            foreach (var pickupable in obj.GetComponents<ICyanEmuPickupable>())
            {
                pickupable.OnPickupUseUp();
            }
        }

        #endregion

        #region ICyanEmuStationHandler

        public static void OnStationEnter(this GameObject obj, VRCStation station)
        {
            foreach (var stationHandler in obj.GetComponents<ICyanEmuStationHandler>())
            {
                stationHandler.OnStationEnter(station);
            }
        }

        public static void OnStationExit(this GameObject obj, VRCStation station)
        {
            foreach (var stationHandler in obj.GetComponents<ICyanEmuStationHandler>())
            {
                stationHandler.OnStationExit(station);
            }
        }

        #endregion

        #region ICyanEmuSyncable & ICyanEmuSyncableHandler

        public static void SetOwner(this GameObject obj, VRCPlayerApi player)
        {
            if (Networking.GetOwner(obj) == player)
            {
                return;
            }

            ICyanEmuSyncable[] syncs = obj.GetComponents<ICyanEmuSyncable>();
            foreach (ICyanEmuSyncable sync in syncs)
            {
                sync.SetOwner(player.playerId);
            }

            ICyanEmuSyncableHandler[] syncHandlers = obj.GetComponents<ICyanEmuSyncableHandler>();
            foreach (ICyanEmuSyncableHandler syncHandler in syncHandlers)
            {
                syncHandler.OnOwnershipTransferred(player.playerId);
            }
        }

        #endregion
    }
}