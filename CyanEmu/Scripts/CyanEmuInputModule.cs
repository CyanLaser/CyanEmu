using UnityEngine;
using UnityEngine.EventSystems;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuInputModule : StandaloneInputModule
    {
        private const int UILayer = 5;
        private const int UIMenuLayer = 12;
        
        private CursorLockMode currentLockState_ = CursorLockMode.None;
        private CyanEmuBaseInput baseInput_;
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
            m_InputOverride = baseInput_ = GetComponent<CyanEmuBaseInput>();
            eventSystem.sendNavigationEvents = false;
            
            base.Start();
        }
        
        public override void Process()
        {
            currentLockState_ = Cursor.lockState;

            Cursor.lockState = CursorLockMode.None;

            base.Process();

            Cursor.lockState = currentLockState_;
        }

        // Prevent clicking on menus on the ui layer if your menu is not open
        protected override MouseState GetMousePointerEventData(int id)
        {
            var pointerEventData = base.GetMousePointerEventData(id);
            var leftEventData = pointerEventData.GetButtonState(PointerEventData.InputButton.Left).eventData;
            var pointerRaycast = leftEventData.buttonData.pointerCurrentRaycast;
            var obj = pointerRaycast.gameObject;

            if (obj == null)
            {
                return pointerEventData;
            }

            bool isMenuLayer = obj.layer == UILayer || obj.layer == UIMenuLayer;
            if (isMenuLayer ^ baseInput_.isMenuOpen)
            {
                //Debug.Log("Found menu that you shouldn't interact with!");
                leftEventData.buttonData.pointerCurrentRaycast = new RaycastResult()
                {
                    depth = pointerRaycast.depth,
                    distance = pointerRaycast.distance,
                    index = pointerRaycast.index,
                    module = pointerRaycast.module,
                    gameObject = null,
                    screenPosition = pointerRaycast.screenPosition,
                    sortingLayer = pointerRaycast.sortingLayer,
                    sortingOrder = pointerRaycast.sortingOrder,
                    worldNormal = pointerRaycast.worldNormal,
                    worldPosition = pointerRaycast.worldPosition
                };
            }
            
            return pointerEventData;
        }
    }

    class CyanEmuBaseInput : BaseInput
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
