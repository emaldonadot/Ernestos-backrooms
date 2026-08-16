using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Records this scene's own level as completed in <see cref="LevelProgressService"/>
    /// when the player reaches the exit — placed once per level scene, wired with that
    /// level's own <see cref="LevelDefinition"/>. Deliberately separate from
    /// <c>LevelCompleteUI</c> (which just shows the "you escaped" screen and navigates
    /// back to the menu) — recording progress and displaying/navigating are two
    /// different jobs.
    /// </summary>
    public sealed class LevelCompletionRecorder : MonoBehaviour
    {
        [SerializeField] private LevelDefinition _level;

        private void OnEnable()
        {
            GameEvents.LevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.LevelCompleted -= HandleLevelCompleted;
        }

        private void HandleLevelCompleted()
        {
            if (_level == null)
            {
                return;
            }

            if (GameServices.TryGet<LevelProgressService>(out var service))
            {
                service.MarkCompleted(_level.LevelId);
            }
        }
    }
}
