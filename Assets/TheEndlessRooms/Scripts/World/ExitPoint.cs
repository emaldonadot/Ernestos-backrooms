using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>The MVP's "one exit condition": interacting here once ends the level.</summary>
    public sealed class ExitPoint : MonoBehaviour, IInteractable
    {
        private bool _isCompleted;

        public string GetInteractionPrompt()
        {
            return "Leave The Continuance";
        }

        public bool CanInteract(InteractionContext context)
        {
            return !_isCompleted;
        }

        public void Interact(InteractionContext context)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            GameEvents.RaiseLevelCompleted();
        }
    }
}
