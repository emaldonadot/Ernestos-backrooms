using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.UI
{
    /// <summary>Shows a simple "you made it" panel on <see cref="GameEvents.LevelCompleted"/> — the MVP's one exit condition needs no more than this.</summary>
    public sealed class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;

        private void OnEnable()
        {
            GameEvents.LevelCompleted += HandleLevelCompleted;
            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEvents.LevelCompleted -= HandleLevelCompleted;
        }

        private void HandleLevelCompleted()
        {
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }
        }
    }
}
