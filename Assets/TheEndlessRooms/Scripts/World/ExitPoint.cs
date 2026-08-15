using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>The level's exit condition — optionally gated on carrying a specific item (the Golden Key, for Level 1's progression).</summary>
    public sealed class ExitPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private InventoryItemDefinition _requiredItem;

        private bool _isCompleted;

        public string GetInteractionPrompt()
        {
            if (_requiredItem != null)
            {
                return $"Leave The Continuance (Needs {_requiredItem.DisplayName})";
            }

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

            if (_requiredItem != null)
            {
                var inventory = context.Instigator != null ? context.Instigator.GetComponentInParent<Inventory>() : null;
                if (inventory == null || !inventory.HasItem(_requiredItem.ItemId))
                {
                    Debug.Log($"The door won't open — needs {_requiredItem.DisplayName}.", this);
                    return;
                }
            }

            _isCompleted = true;
            GameEvents.RaiseLevelCompleted();
        }
    }
}
