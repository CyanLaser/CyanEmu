// VRCP_SyncableEditorHelper
// Created by CyanLaser

using System.Collections.Generic;
using UnityEditor;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class VRCP_SyncableEditorHelper
    {
        public static void DisplaySyncOptions(VRCP_Syncable syncable)
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
                syncable.SetOwner(players[owner].playerId);
            }
        }
    }
}
