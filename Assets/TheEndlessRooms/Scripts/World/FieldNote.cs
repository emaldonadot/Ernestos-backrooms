using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A readable environmental-storytelling fragment (PRD Section 14) — work orders,
    /// PA transcripts, personnel logs. Not <see cref="ISaveable"/>: reading it is
    /// idempotent, there's nothing to persist. Raises a Core event rather than
    /// referencing UI directly, matching the rest of the project's decoupling pattern
    /// (see <see cref="GameEvents.RoomEntered"/>/<see cref="GameEvents.LevelCompleted"/>)
    /// — <c>EndlessRooms.UI</c> has no dependency on <c>EndlessRooms.World</c>.
    /// </summary>
    public sealed class FieldNote : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _promptLabel = "Read Note";
        [TextArea(3, 10)]
        [SerializeField] private string _fragmentText = "";

        public string GetInteractionPrompt()
        {
            return _promptLabel;
        }

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
            GameEvents.RaiseFieldNoteOpened(_fragmentText);
        }
    }
}
