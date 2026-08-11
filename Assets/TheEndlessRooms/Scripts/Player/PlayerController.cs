using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRooms.Player
{
    /// <summary>
    /// First-person walk/run/crouch/look controller driven by the new Input System.
    /// Movement tuning lives in <see cref="PlayerMovementConfig"/>; stamina logic lives
    /// in the plain <see cref="PlayerStamina"/> class so it stays testable in isolation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour, IDetectable
    {
        [Header("Config")]
        [SerializeField] private PlayerMovementConfig _config;

        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _lookAction;
        [SerializeField] private InputActionReference _sprintAction;
        [SerializeField] private InputActionReference _crouchAction;

        [Header("References")]
        [SerializeField] private Transform _cameraPivot;

        private CharacterController _characterController;
        private PlayerStamina _stamina;
        private Vector3 _currentVelocity;
        private float _verticalLookRotation;
        private bool _isCrouching;
        private float _currentHeight;

        public bool IsSprinting { get; private set; }
        public float CurrentStamina => _stamina?.Current ?? 0f;
        public float MaxStamina => _config != null ? _config.MaxStamina : 0f;

        public Transform DetectionPoint => _cameraPivot != null ? _cameraPivot : transform;
        public float NoiseLevel { get; private set; }
        public bool IsHidden { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_config == null)
            {
                Debug.LogError($"{nameof(PlayerController)} on '{name}' is missing a {nameof(PlayerMovementConfig)} reference.", this);
                enabled = false;
                return;
            }

            _stamina = new PlayerStamina(_config);
            _currentHeight = _config.StandingHeight;
            _characterController.height = _currentHeight;
        }

        private void OnEnable()
        {
            _moveAction?.action.Enable();
            _lookAction?.action.Enable();
            _sprintAction?.action.Enable();
            _crouchAction?.action.Enable();
            GameEvents.PlayerHiddenChanged += OnPlayerHiddenChanged;
        }

        private void OnDisable()
        {
            _moveAction?.action.Disable();
            _lookAction?.action.Disable();
            _sprintAction?.action.Disable();
            _crouchAction?.action.Disable();
            GameEvents.PlayerHiddenChanged -= OnPlayerHiddenChanged;
        }

        private void OnPlayerHiddenChanged(bool isHidden)
        {
            IsHidden = isHidden;
        }

        private void Update()
        {
            HandleLook();
            HandleCrouch();
            HandleMovement();
        }

        private void HandleLook()
        {
            if (_lookAction == null || _cameraPivot == null)
            {
                return;
            }

            Vector2 lookDelta = _lookAction.action.ReadValue<Vector2>() * _config.MouseSensitivity;

            transform.Rotate(Vector3.up, lookDelta.x);

            _verticalLookRotation = Mathf.Clamp(_verticalLookRotation - lookDelta.y, _config.MinPitch, _config.MaxPitch);
            _cameraPivot.localEulerAngles = new Vector3(_verticalLookRotation, 0f, 0f);
        }

        private void HandleCrouch()
        {
            _isCrouching = _crouchAction != null && _crouchAction.action.IsPressed();

            float targetHeight = _isCrouching ? _config.CrouchingHeight : _config.StandingHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, targetHeight, _config.CrouchTransitionSpeed * Time.deltaTime);
            _characterController.height = _currentHeight;

            Vector3 center = _characterController.center;
            center.y = _currentHeight / 2f;
            _characterController.center = center;
        }

        private void HandleMovement()
        {
            if (IsHidden)
            {
                // A hiding spot should actually hide you — otherwise there's nothing
                // stopping you from stepping right back out while still flagged as
                // concealed. Freezes horizontal movement entirely; look is untouched
                // so you can still peek around from a fixed spot.
                _currentVelocity.x = 0f;
                _currentVelocity.z = 0f;
                NoiseLevel = 0f;
                return;
            }

            Vector2 moveInput = _moveAction != null ? _moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            bool sprintHeld = _sprintAction != null && _sprintAction.action.IsPressed();

            IsSprinting = sprintHeld && !_isCrouching && moveInput.sqrMagnitude > 0.01f && !_stamina.IsExhausted;
            _stamina.Tick(Time.deltaTime, IsSprinting);

            float targetSpeed = _isCrouching
                ? _config.CrouchSpeed
                : IsSprinting
                    ? _config.SprintSpeed
                    : _config.WalkSpeed;

            Vector3 desiredDirection = Vector3.ClampMagnitude(transform.right * moveInput.x + transform.forward * moveInput.y, 1f);
            Vector3 targetVelocity = desiredDirection * targetSpeed;

            Vector3 horizontalVelocity = Vector3.MoveTowards(
                new Vector3(_currentVelocity.x, 0f, _currentVelocity.z),
                targetVelocity,
                _config.Acceleration * Time.deltaTime);

            _currentVelocity.x = horizontalVelocity.x;
            _currentVelocity.z = horizontalVelocity.z;

            _currentVelocity.y = _characterController.isGrounded
                ? Mathf.Max(_config.Gravity * Time.deltaTime, -0.1f)
                : _currentVelocity.y + _config.Gravity * Time.deltaTime;

            _characterController.Move(_currentVelocity * Time.deltaTime);

            float speedRatio = horizontalVelocity.magnitude / Mathf.Max(_config.SprintSpeed, 0.01f);
            NoiseLevel = _isCrouching ? 0f : Mathf.Clamp01(speedRatio);
        }
    }
}
