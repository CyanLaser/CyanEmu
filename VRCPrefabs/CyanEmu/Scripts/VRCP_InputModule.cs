// VRCP_InputModule
// Created by CyanLaser

using UnityEngine;
using UnityEngine.EventSystems;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_InputModule : StandaloneInputModule
    {
        private CursorLockMode currentLockState_ = CursorLockMode.None;

        public static void DisableOtherInputModules()
        {
            EventSystem[] systems = FindObjectsOfType<EventSystem>();
            foreach (EventSystem system in systems)
            {
                system.enabled = false;
            }
        }

        protected override void Start()
        {
            m_InputOverride = GetComponent<VRCP_BaseInput>();
            base.Start();
        }
        
        public override void Process()
        {
            currentLockState_ = Cursor.lockState;

            Cursor.lockState = CursorLockMode.None;

            base.Process();

            Cursor.lockState = currentLockState_;
        }
    }

    class VRCP_BaseInput : BaseInput
    {
        public bool isMenuOpen;
        private Vector2 lastMousePos_;
        private Vector2 mouseDelta_;

        public static Vector2 GetScreenCenter()
        {
            return new Vector2(Screen.width, Screen.height) * 0.5f;
        }

        public override Vector2 mousePosition
        {
            get
            {
                if (isMenuOpen)
                {
                    return base.mousePosition;
                }
                return GetScreenCenter() - mouseDelta_;
            }
        }

        private void Update()
        {
            Vector2 curPos = base.mousePosition;
            mouseDelta_ = curPos - lastMousePos_;
            lastMousePos_ = curPos;
        }
    }
}
