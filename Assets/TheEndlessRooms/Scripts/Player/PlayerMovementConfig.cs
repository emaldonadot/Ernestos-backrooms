using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Design-time movement/stamina tuning, kept as a ScriptableObject so designers can
    /// author and swap presets from the Inspector without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "The Endless Rooms/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float WalkSpeed = 4.5f;
        [Min(0f)] public float SprintSpeed = 8f;
        [Min(0f)] public float CrouchSpeed = 2.2f;
        [Min(0f)] public float Acceleration = 24f;

        [Header("Crouch")]
        [Min(0.1f)] public float StandingHeight = 1.8f;
        [Min(0.1f)] public float CrouchingHeight = 1.0f;
        [Min(0f)] public float CrouchTransitionSpeed = 8f;

        [Header("Look")]
        [Min(0.01f)] public float MouseSensitivity = 0.15f;
        [Range(-90f, 0f)] public float MinPitch = -85f;
        [Range(0f, 90f)] public float MaxPitch = 85f;

        [Header("Gravity")]
        public float Gravity = -9.81f;

        [Header("Stamina")]
        [Min(0f)] public float MaxStamina = 5f;
        [Min(0f)] public float StaminaDrainPerSecond = 1f;
        [Min(0f)] public float StaminaRegenPerSecond = 0.75f;
        [Min(0f)] public float StaminaRegenDelay = 1f;
    }
}
