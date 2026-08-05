namespace EndlessRooms.Core
{
    /// <summary>
    /// Anything the player (or, later, any actor) can aim at and interact with:
    /// doors, switches, pickups, puzzle elements, etc.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short label shown in the interaction prompt UI, e.g. "Open Door".</summary>
        string GetInteractionPrompt();

        bool CanInteract(InteractionContext context);

        void Interact(InteractionContext context);
    }
}
