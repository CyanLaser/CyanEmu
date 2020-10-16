#if VRC_SDK_VRCSDK2

using System.Text;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.SceneManagement;
using System;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuBufferManager
    {
        // TODO redo everything to be syncable events instead of simply trigger buffering

        // SyncStates
        // 
        // Types:
        // Player join (removed with player left) (position syncable?)
        // VRC trigger
        // owned syncable items
        //  owner, Position, animation, udon variables

            
        //private const int VERSION = 3;

        public static CyanEmuBufferManager instance;

        private LinkedList<VRC_EventHandler.EventInfo> eventList_;

        public CyanEmuBufferManager()
        {
            instance = this;
            eventList_ = new LinkedList<VRC_EventHandler.EventInfo>();
        }
        
        public void ReadBufferData(string bufferData)
        {
            string[] triggers = bufferData.Split('\n');
            for (int i = 0; i < triggers.Length; ++i)
            {
                eventList_.AddLast(JsonUtility.FromJson<VRC_EventHandler.EventInfo>(triggers[i]));
            }
        }

        private string SaveBufferData()
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            foreach (var eventInfo in eventList_)
            {
                GameObject instigator = eventInfo.instagator;
                eventInfo.instagator = null;
                sb.Append(JsonUtility.ToJson(eventInfo, false));
                eventInfo.instagator = instigator;

                if (i + 1 != eventList_.Count)
                {
                    sb.Append('\n');
                }
                ++i;
            }
            
            return sb.ToString();
        }

        public void SaveBufferedEvent(VRC_EventHandler.EventInfo eventInfo)
        {
            if (eventInfo.broadcast.IsBufferOneBroadcastType())
            {
                eventList_.Remove(eventInfo);
            }
            eventList_.AddLast(eventInfo);
        }

        public void ReplayTriggers()
        {
            Debug.Log("Replaying Triggers!");

            foreach (var eventInfo in eventList_)
            {
                CyanEmuTriggerExecutor.ExecuteEvent(eventInfo, null);
            }
        }
        
        public static string GetBufferedTriggerFilePath()
        {
            return SceneManager.GetActiveScene().path.Replace(".unity", "") + "_BufferedTriggers.txt";
        }

        public static void SaveBufferedTriggersToFile()
        {
            if (instance == null)
            {
                return;
            }
            string bufferedTriggers = instance.SaveBufferData();
            if (bufferedTriggers.Length == 0)
            {
                return;
            }
            
            File.WriteAllText(GetBufferedTriggerFilePath(), bufferedTriggers);
        }

        public static void LoadBufferedTriggersFromFile()
        {
            if (instance == null)
            {
                return;
            }
            
            string fileName = GetBufferedTriggerFilePath();
            if (File.Exists(fileName))
            {
                string bufferedTriggers = File.ReadAllText(fileName);
                instance.ReadBufferData(bufferedTriggers);
            }
        }

        public static void DeleteBufferedTriggerFile()
        {
            string fileName = GetBufferedTriggerFilePath();
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        public static bool SceneContainsBufferedTriggers()
        {
            return File.Exists(GetBufferedTriggerFilePath());
        }
    }
}

#endif