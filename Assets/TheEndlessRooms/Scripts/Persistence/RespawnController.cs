using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Persistence
{
    /// <summary>
    /// Milestone 7's capture → restart flow: the checkpoint <em>is</em> a save slot,
    /// not a second persistence system. Listens for
    /// <see cref="GameEvents.PlayerCaptured"/> (raised by <c>AttendantController</c>
    /// when it catches the player) and reloads the last save. If there is no save yet,
    /// falls back to rebuilding the current seed and repositioning the player at the
    /// entry room — the same recovery a fresh session would have.
    /// </summary>
    public sealed class RespawnController : MonoBehaviour
    {
        [SerializeField] private SaveService _saveService;
        [SerializeField] private EndlessRooms.World.ProceduralLevelBuilder _levelBuilder;
        [SerializeField] private Transform _player;
        [SerializeField] private CharacterController _playerCharacterController;

        private void OnEnable()
        {
            GameEvents.PlayerCaptured += OnPlayerCaptured;
        }

        private void OnDisable()
        {
            GameEvents.PlayerCaptured -= OnPlayerCaptured;
        }

        private void OnPlayerCaptured()
        {
            if (_saveService != null && _saveService.HasSaveFile())
            {
                _saveService.Load();
                return;
            }

            RespawnAtEntry();
        }

        private void RespawnAtEntry()
        {
            if (_levelBuilder == null || _player == null)
            {
                Debug.LogWarning($"{nameof(RespawnController)} on '{name}' can't respawn: missing level builder or player reference.", this);
                return;
            }

            _levelBuilder.BuildLevel();
            Vector3 spawnPosition = _levelBuilder.GetEntryWorldPosition();

            if (_playerCharacterController != null)
            {
                _playerCharacterController.enabled = false;
            }

            _player.position = spawnPosition;

            if (_playerCharacterController != null)
            {
                _playerCharacterController.enabled = true;
            }
        }
    }
}
