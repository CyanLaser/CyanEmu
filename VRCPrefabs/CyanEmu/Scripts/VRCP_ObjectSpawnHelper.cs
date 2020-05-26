// VRCP_ObjectSpawnHelper
// Created by CyanLaser

using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_ObjectSpawnHelper : MonoBehaviour
    {
        private List<GameObject> spawnedObjects_ = new List<GameObject>();
        private VRC_ObjectSpawn objectSpawn_;

        public static void InitializeSpawner(VRC_ObjectSpawn objectSpawn)
        {
            VRCP_ObjectSpawnHelper spawnHelper = objectSpawn.GetComponent<VRCP_ObjectSpawnHelper>();
            if (spawnHelper == null)
            {
                spawnHelper = objectSpawn.gameObject.AddComponent<VRCP_ObjectSpawnHelper>();
                spawnHelper.objectSpawn_ = objectSpawn;
            }

            if (objectSpawn.Instantiate == null)
            {
                objectSpawn.Instantiate = spawnHelper.SpawnObject;
            }
            if (objectSpawn.ReapObjects == null)
            {
                objectSpawn.ReapObjects = spawnHelper.ReapObjects;
            }
        }

        private void SpawnObject(Vector3 position, Quaternion rotation)
        {
            this.Log("Spawning Object " + objectSpawn_.ObjectPrefab.name + " at " + position + " and rotataion " + rotation);
            GameObject spawnedObject = VRCP_CyanEmuMain.SpawnObject(objectSpawn_.ObjectPrefab, position, rotation);
            spawnedObjects_.Add(spawnedObject);
        }

        private void ReapObjects()
        {
            this.Log("Reaping all spawned objects");
            foreach (GameObject obj in spawnedObjects_)
            {
                Destroy(obj);
            }
            spawnedObjects_.Clear();
        }
    }
}
