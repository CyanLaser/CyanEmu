namespace VRCPrefabs.CyanEmu
{
    public interface ICyanEmuInteractable {
        bool CanInteract(float distance);
        string GetInteractText();
        void Interact();
    }
}