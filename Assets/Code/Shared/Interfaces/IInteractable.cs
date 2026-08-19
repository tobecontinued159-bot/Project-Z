using Fusion;

public interface IInteractable
{
    string PromptText { get; }
    bool CanInteract(PlayerRef player);
    void Interact(PlayerRef player);
}
