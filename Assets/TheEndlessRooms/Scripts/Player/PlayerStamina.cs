using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Pure stamina resource logic, deliberately decoupled from input/movement code
    /// so it can be driven and unit tested without a scene or a CharacterController.
    /// </summary>
    public sealed class PlayerStamina
    {
        private readonly PlayerMovementConfig _config;
        private float _regenCooldown;

        public PlayerStamina(PlayerMovementConfig config)
        {
            _config = config;
            Current = config.MaxStamina;
        }

        public float Current { get; private set; }

        public bool IsExhausted => Current <= 0f;

        public void Tick(float deltaTime, bool isSprinting)
        {
            if (isSprinting && Current > 0f)
            {
                Current = Mathf.Max(0f, Current - _config.StaminaDrainPerSecond * deltaTime);
                _regenCooldown = _config.StaminaRegenDelay;
                return;
            }

            if (_regenCooldown > 0f)
            {
                _regenCooldown -= deltaTime;
                return;
            }

            Current = Mathf.Min(_config.MaxStamina, Current + _config.StaminaRegenPerSecond * deltaTime);
        }
    }
}
