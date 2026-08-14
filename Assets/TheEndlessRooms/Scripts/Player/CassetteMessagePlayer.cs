using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// "Combining" the cassette and recorder — implemented as a simple ownership check
    /// (both items in the same <see cref="Inventory"/>) rather than a dedicated combine
    /// UI, same simplification <see cref="PlayerUvFlashlight"/> uses for its battery
    /// requirement. Using either item while both are carried plays the message, reusing
    /// the existing FieldNote/FieldNoteUI text-reveal pipeline rather than building a
    /// second one.
    /// </summary>
    public sealed class CassetteMessagePlayer : MonoBehaviour
    {
        [SerializeField] private InventoryItemDefinition _cassetteItem;
        [SerializeField] private InventoryItemDefinition _recorderItem;
        [SerializeField] private Inventory _inventory;
        [TextArea(3, 10)]
        [SerializeField] private string _messageText = "";

        private void OnEnable()
        {
            GameEvents.ItemUseRequested += HandleItemUseRequested;
        }

        private void OnDisable()
        {
            GameEvents.ItemUseRequested -= HandleItemUseRequested;
        }

        private void HandleItemUseRequested(string itemId)
        {
            bool isCassetteOrRecorder = (_cassetteItem != null && itemId == _cassetteItem.ItemId)
                || (_recorderItem != null && itemId == _recorderItem.ItemId);

            if (!isCassetteOrRecorder || _inventory == null)
            {
                return;
            }

            bool hasBoth = _cassetteItem != null && _recorderItem != null
                && _inventory.HasItem(_cassetteItem.ItemId) && _inventory.HasItem(_recorderItem.ItemId);

            if (!hasBoth)
            {
                Debug.Log("Nothing happens — the cassette and recorder need to both be on you.");
                return;
            }

            GameEvents.RaiseFieldNoteOpened(_messageText);
        }
    }
}
