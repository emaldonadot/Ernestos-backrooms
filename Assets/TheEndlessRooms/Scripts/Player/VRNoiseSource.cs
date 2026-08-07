using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// The Quest rig's <see cref="IDetectable"/> implementation, mirroring
    /// <see cref="PlayerController"/>'s noise/hidden reporting for PC. The Attendant's
    /// core archetype (Milestone 7) is sound-driven, so the VR rig needs a noise output
    /// too — this reads the same <see cref="CharacterController"/> XRI's locomotion
    /// providers already drive, rather than adding any VR-specific movement code.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class VRNoiseSource : MonoBehaviour, IDetectable
    {
        [SerializeField] private Transform _detectionPoint;
        [SerializeField] private float _maxWalkSpeed = 3f;

        private CharacterController _characterController;

        public Transform DetectionPoint => _detectionPoint != null ? _detectionPoint : transform;
        public float NoiseLevel { get; private set; }
        public bool IsHidden { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            GameEvents.PlayerHiddenChanged += OnPlayerHiddenChanged;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHiddenChanged -= OnPlayerHiddenChanged;
        }

        private void Update()
        {
            Vector3 horizontalVelocity = _characterController.velocity;
            horizontalVelocity.y = 0f;
            NoiseLevel = Mathf.Clamp01(horizontalVelocity.magnitude / Mathf.Max(_maxWalkSpeed, 0.01f));
        }

        private void OnPlayerHiddenChanged(bool isHidden)
        {
            IsHidden = isHidden;
        }
    }
}
