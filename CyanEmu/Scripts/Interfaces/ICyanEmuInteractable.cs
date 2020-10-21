namespace VRCPrefabs.CyanEmu
{
    public interface ICyanEmuInteractable
    {
        float GetProximity();
        bool CanInteract();
        string GetInteractText();
        void Interact();
    }
}