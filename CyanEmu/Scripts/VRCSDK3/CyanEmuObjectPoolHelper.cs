#if UDON

using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuObjectPoolHelper : MonoBehaviour, ICyanEmuSyncable
    {
        private int ownerID_ = 1;
        private VRCObjectPool objectPool_;
        
        public static void OnInit(VRCObjectPool objectPool)
        {
            objectPool.gameObject.AddComponent<CyanEmuObjectPoolHelper>().Init(objectPool);
        }
        
        public static void OnSpawn(VRCObjectPool objectPool, int index)
        {
            objectPool.GetComponent<CyanEmuObjectPoolHelper>().OnObjectSpawned(index);
        }
        
        public static void OnReturn(VRCObjectPool objectPool, int index)
        {
            objectPool.GetComponent<CyanEmuObjectPoolHelper>().OnObjectReturned(index);
        }


        private void Init(VRCObjectPool objectPool)
        {
            objectPool_ = objectPool;
        }

        private void OnObjectSpawned(int index)
        {
            Networking.SetOwner(VRCPlayerApi.GetPlayerById(ownerID_), objectPool_.Pool[index]);
        }
        
        private void OnObjectReturned(int index)
        {
            
        }
        
        #region ICyanEmuSyncable

        public int GetOwner()
        {
            return ownerID_;
        }

        public void SetOwner(int ownerID)
        {
            ownerID_ = ownerID;
        }

        #endregion
    }
}
#endif