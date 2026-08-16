using System.Collections;
using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRooms.UI
{
    /// <summary>
    /// Shows a "you made it" panel on <see cref="GameEvents.LevelCompleted"/>, then
    /// returns to the main menu after a brief delay — same "show a screen, then
    /// navigate away automatically" shape as <see cref="GameOverController"/>'s
    /// reload-on-capture. Progress itself is recorded separately by
    /// <see cref="EndlessRooms.Core.LevelCompletionRecorder"/>; this component only
    /// displays and navigates.
    /// </summary>
    public sealed class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private float _returnToMenuDelaySeconds = 4f;
        [SerializeField] private string _pcMenuSceneName = "MainMenu_PC";
        [SerializeField] private string _questMenuSceneName = "MainMenu_Quest";

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
            StartCoroutine(ReturnToMenuAfterDelay());
        }

        private IEnumerator ReturnToMenuAfterDelay()
        {
            yield return new WaitForSeconds(_returnToMenuDelaySeconds);
            string menuSceneName = Application.platform == RuntimePlatform.Android ? _questMenuSceneName : _pcMenuSceneName;
            SceneManager.LoadScene(menuSceneName);
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
