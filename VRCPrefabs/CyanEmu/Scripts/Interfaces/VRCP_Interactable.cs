// VRCP_Interactable
// Created by CyanLaser

namespace VRCPrefabs.CyanEmu
{
    public interface VRCP_Interactable {
        bool CanInteract(float distance);
        string GetInteractText();
        void Interact();
    }
}