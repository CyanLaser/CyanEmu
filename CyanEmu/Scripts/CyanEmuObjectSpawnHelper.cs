using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuObjectSpawnHelper : MonoBehaviour
    {
        private List<GameObject> spawnedObjects_ = new List<GameObject>();
        private VRC_ObjectSpawn objectSpawn_;

        public static void InitializeSpawner(VRC_ObjectSpawn objectSpawn)
        {
            CyanEmuObjectSpawnHelper spawnHelper = objectSpawn.GetComponent<CyanEmuObjectSpawnHelper>();
            if (spawnHelper == null)
            {
                spawnHelper = objectSpawn.gameObject.AddComponent<CyanEmuObjectSpawnHelper>();
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
            GameObject spawnedObject = CyanEmuMain.SpawnObject(objectSpawn_.ObjectPrefab, position, rotation);
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
