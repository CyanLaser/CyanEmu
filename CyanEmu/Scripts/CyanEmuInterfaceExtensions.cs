using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class CyanEmuInterfaceExtensions
    {
        #region ICyanEmuInteractable

        public static ICyanEmuInteractable GetClosestInteractable(this GameObject obj, float distance)
        {
            ICyanEmuInteractable closest = null;
            float bestDistance = float.MaxValue;

            foreach (var interactable in obj.GetComponents<ICyanEmuInteractable>())
            {
                float proximity = interactable.GetProximity();
                if (interactable.CanInteract(distance) && proximity < bestDistance)
                {
                    closest = interactable;
                    bestDistance = proximity;
                }
            }

            return closest;
        }

        public static bool CanInteract(this ICyanEmuInteractable interactable, float distance)
        {
            return interactable.CanInteract() && distance < interactable.GetProximity(); // TODO multiply by camera scale?
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