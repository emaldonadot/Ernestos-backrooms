using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// The always-visible floating status text next to a <see cref="LevelSelectEntry"/>
    /// in the main menu — separate from that component since Core stays UI-agnostic
    /// (same split as FieldNote/FieldNoteUI). Refreshed once at <see cref="Start"/>:
    /// nothing changes a level's completion/unlock state while the player is just
    /// standing in the menu looking at it.
    /// </summary>
    public sealed class LevelSelectLabel : MonoBehaviour
    {
        [SerializeField] private LevelSelectEntry _entry;
        [SerializeField] private Text _statusText;

        private void Start()
        {
            if (_entry == null || _statusText == null)
            {
                return;
            }

            _statusText.text = _entry.GetInteractionPrompt();
            _statusText.color = _entry.IsUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
    }
}
