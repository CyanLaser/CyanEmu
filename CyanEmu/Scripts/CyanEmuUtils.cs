using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRCPrefabs.CyanEmu
{
    public static class CyanEmuUtils
    {
        public static string GetPaths(GameObject[] objs)
        {
            string paths = "";
            for (int i = 0; i < objs.Length; ++i)
            {
                paths += PathForObject(objs[i]) + (i == objs.Length - 1 ? "" : " - ");
            }
            return paths;
        }

        public static string PathForObject(GameObject obj)
        {
            if (obj == null)
            {
                return "null";
            }

            string path = "";
            Transform transform = obj.transform;
            while (transform != null)
            {
                path = transform.name + (path == "" ? "" : "/" + path);
                transform = transform.parent;
            }
            return "/" + path;
        }

        public static GameObject GetGameObjectAtPath(string path)
        {
            string[] objPath = path.Split('/');
            if (objPath.Length == 0)
            {
                return null;
            }

            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);

            GameObject ret = null;

            for (int i = 0; i < rootObjects.Count; ++i)
            {
                if (rootObjects[i].name == objPath[0])
                {
                    ret = rootObjects[i];
                    break;
                }
            }

            if (ret)
            {
                for (int i = 1; i < objPath.Length; ++i)
                {
                    Transform child = ret.transform.Find(objPath[i]);
                    if (child == null)
                    {
                        return null;
                    }
                    ret = child.gameObject;
                }
            }

            return ret;
        }
        
        // Logging
        private static void Log(string area, string message)
        {
            Debug.Log("[" + System.DateTime.Now.ToString("hh:mm:ss.fff") + "][" + area + "] " + message);
        }

        public static void Log(this object obj, string message)
        {
            if (CyanEmuSettings.Instance.displayLogs)
            {
                Log(obj.GetType().Name, message);
            }
        }
        
        private static void LogWarning(string area, string message)
        {
            Debug.LogWarning("[" + System.DateTime.Now.ToString("hh:mm:ss.fff") + "][" + area + "] " + message);
        }

        public static void LogWarning(this object obj, string message)
        {
            LogWarning(obj.GetType().Name, message);
        }

        private static void LogError(string area, string message)
        {
            Debug.LogError("[" + System.DateTime.Now.ToString("hh:mm:ss.fff") + "][" + area + "] " + message);
        }

        public static void LogError(this object obj, string message)
        {
            LogError(obj.GetType().Name, message);
        }
    }
}