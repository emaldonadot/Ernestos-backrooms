using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRooms.Core
{
    /// <summary>
    /// A level entry in the main menu, interacted with the exact same
    /// look-and-press-Interact flow as everything else in this game (a door, a pickup) —
    /// reusing <c>InteractionCaster</c>/<c>IInteractable</c> instead of a real uGUI
    /// button graph means the menu needs zero new input plumbing and already works
    /// identically on PC (mouse-look raycast) and Quest (controller ray via
    /// InteractionCaster's ray-origin override), since both already drive that exact
    /// path everywhere else in the game.
    /// </summary>
    public sealed class LevelSelectEntry : MonoBehaviour, IInteractable
    {
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private LevelCatalog _catalog;

        public bool IsUnlocked => GameServices.TryGet<LevelProgressService>(out var service) && _level != null && service.IsUnlocked(_level.LevelId, _catalog);

        public bool IsCompleted => GameServices.TryGet<LevelProgressService>(out var service) && _level != null && service.IsCompleted(_level.LevelId);

        public string GetInteractionPrompt()
        {
            if (_level == null)
            {
                return string.Empty;
            }

            if (!IsUnlocked)
            {
                return $"{_level.DisplayName} — Locked";
            }

            return IsCompleted ? $"Replay {_level.DisplayName}" : $"Play {_level.DisplayName}";
        }

        public bool CanInteract(InteractionContext context)
        {
            return IsUnlocked;
        }

        public void Interact(InteractionContext context)
        {
            if (!IsUnlocked || _level == null || string.IsNullOrEmpty(_level.SceneName))
            {
                return;
            }

            SceneManager.LoadScene(_level.SceneName);
        }
    }
}
