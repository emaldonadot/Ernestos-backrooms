using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Builds the level once and moves the player to its entry room. Toggles the
    /// <see cref="CharacterController"/> off around the teleport, the standard way to
    /// stop it fighting a manual position set.
    /// </summary>
    public sealed class LevelPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;
        [SerializeField] private Transform _player;
        [SerializeField] private CharacterController _playerCharacterController;

        private void Start()
        {
            if (_levelBuilder == null || _player == null)
            {
                Debug.LogError($"{nameof(LevelPlayerSpawner)} on '{name}' is missing its level builder or player reference.", this);
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
