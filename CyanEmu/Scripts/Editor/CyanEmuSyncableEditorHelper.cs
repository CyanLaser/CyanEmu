using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class CyanEmuSyncableEditorHelper
    {
        public static void DisplaySyncOptions(ICyanEmuSyncable syncable)
        {
            int currentOwner = 0;
            List<VRCPlayerApi> players = VRCPlayerApi.AllPlayers;
            string[] playerNames = new string[players.Count];
            for (int i = 0; i < players.Count; ++i)
            {
                if (players[i].playerId == syncable.GetOwner())
                {
                    currentOwner = i;
                }
                playerNames[i] = players[i].displayName;
            }

            int owner = EditorGUILayout.Popup("Set Owner", currentOwner, playerNames);
            if (owner != currentOwner)
            {
                Networking.SetOwner(players[owner], (syncable as MonoBehaviour).gameObject);
            }
        }
    }
}
