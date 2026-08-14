using System.Collections;
using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRooms.UI
{
    /// <summary>
    /// Milestone 9's "lose" consequence: shows a caught screen, then reloads the whole
    /// scene after a brief delay (long enough for the capture camera shake to actually
    /// register before the view cuts — same reasoning as
    /// <c>EndlessRooms.Persistence.RespawnController</c>). Deliberately simpler than
    /// that controller's checkpoint-reload flow: Level 1 has no save/checkpoint system
    /// wired in yet, and a full scene reload is a perfectly good first-pass "restart"
    /// when there's no meaningful persistent progress to preserve.
    /// </summary>
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private float _reloadDelaySeconds = 2f;

        private void OnEnable()
        {
            GameEvents.PlayerCaptured += HandlePlayerCaptured;
            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEvents.PlayerCaptured -= HandlePlayerCaptured;
        }

        private void HandlePlayerCaptured()
        {
            SetVisible(true);
            StartCoroutine(ReloadAfterDelay());
        }

        private IEnumerator ReloadAfterDelay()
        {
            yield return new WaitForSeconds(_reloadDelaySeconds);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
