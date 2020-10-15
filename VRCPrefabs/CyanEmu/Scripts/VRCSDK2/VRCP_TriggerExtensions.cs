#if VRC_SDK_VRCSDK2

using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class CyanEmuTriggerExtensions
    {
        public static VRC_Trigger.TriggerEvent GetCustomNamed(this VRC_Trigger trigger, string name)
        {
            foreach (VRC_Trigger.TriggerEvent events in trigger.Triggers)
            {
                if (events.Name == name && events.TriggerType == VRC_Trigger.TriggerType.Custom)
                {
                    return events;
                }
            }
            return null;
        }

        public static int GetTriggerEventIndex(this VRC_Trigger trigger, VRC_Trigger.TriggerEvent trigEvent)
        {
            for (int curEvent = 0; curEvent < trigger.Triggers.Count; ++curEvent)
            {
                if (trigEvent == trigger.Triggers[curEvent])
                {
                    return curEvent;
                }
            }

            return -1;
        }

        public static VRC_Trigger.TriggerEvent GetTriggerEvent(this VRC_Trigger.CustomTriggerTarget target)
        {
            return target.TriggerObject.GetComponent<VRC_Trigger>().GetCustomNamed(target.CustomName);
        }

        public static string GetTriggerEventAsString(this VRC_Trigger.TriggerEvent trigEvent)
        {
            string path = "null";
            int eventIndex = -1;

            VRC_Trigger trig = CyanEmuTriggerExecutor.GetTriggerForEvent(trigEvent);
            if (trig != null)
            {
                eventIndex = trig.GetTriggerEventIndex(trigEvent);
                path = CyanEmuUtils.PathForObject(trig.gameObject);
            }

            return string.Format(
                "Trigger Event[{0}] \"{1}\" type: {2}, broadcast: {3}, delay: {4}, path: {5}",
                eventIndex,
                trigEvent.Name,
                trigEvent.TriggerType,
                trigEvent.BroadcastType,
                trigEvent.AfterSeconds,
                path
            );
        }

        public static string GetEventAsString(this VRC_EventHandler.VrcEvent triggerEvent, GameObject obj)
        {
            return string.Format(
                "type: {0}, on object: {1} Params: [{2}], [{3}], [{4}f], [\"{5}\"], bytes: [{6}]",
                triggerEvent.EventType,
                CyanEmuUtils.PathForObject(obj),
                triggerEvent.ParameterBoolOp,
                triggerEvent.ParameterInt,
                triggerEvent.ParameterFloat,
                triggerEvent.ParameterString,
                triggerEvent.ParameterBytesVersion
            );
        }

        public static bool IsOwnerBroadcastType(this VRC_EventHandler.VrcBroadcastType broadcastType)
        {
            return
                broadcastType == VRC_EventHandler.VrcBroadcastType.Owner ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.OwnerBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.OwnerUnbuffered;
        }

        public static bool IsMasterBroadcastType(this VRC_EventHandler.VrcBroadcastType broadcastType)
        {
            return
                broadcastType == VRC_EventHandler.VrcBroadcastType.Master ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.MasterBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.MasterUnbuffered;
        }

        public static bool IsEveryoneBroadcastType(this VRC_EventHandler.VrcBroadcastType broadcastType)
        {
            return
                broadcastType == VRC_EventHandler.VrcBroadcastType.Always ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.AlwaysBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.AlwaysUnbuffered;
        }

        public static bool IsBufferedBroadcastType(this VRC_EventHandler.VrcBroadcastType broadcastType)
        {
            return
                broadcastType == VRC_EventHandler.VrcBroadcastType.Owner ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.OwnerBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.Master ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.MasterBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.Always ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.AlwaysBufferOne;
        }

        public static bool IsBufferOneBroadcastType(this VRC_EventHandler.VrcBroadcastType broadcastType)
        {
            return
                broadcastType == VRC_EventHandler.VrcBroadcastType.OwnerBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.MasterBufferOne ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.AlwaysBufferOne;
        }

        public static bool IsAlwaysBufferedBroadcastType(this VRC_EventHandler.VrcBroadcastType broadcastType)
        {
            return
                broadcastType == VRC_EventHandler.VrcBroadcastType.Owner ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.Master ||
                broadcastType == VRC_EventHandler.VrcBroadcastType.Always;
        }
    }
}

#endif