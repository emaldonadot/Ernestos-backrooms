using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Offsets a camera transform with decaying random noise. Identical on PC and VR
    /// per the user's explicit choice (Milestone 7, see DECISIONS.md 2026-08-07) —
    /// there is deliberately no platform branch here; if this proves uncomfortable on
    /// a headset in practice, split the config per platform rather than the code.
    /// </summary>
    public sealed class CameraShakeEffect : MonoBehaviour
    {
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private float _captureShakeMagnitude = 0.4f;
        [SerializeField] private float _captureShakeDuration = 0.8f;
        [SerializeField] private float _decaySpeed = 3f;

        private Vector3 _localOrigin;
        private float _currentMagnitude;
        private float _remainingDuration;

        private void Awake()
        {
            if (_shakeTarget == null)
            {
                _shakeTarget = transform;
            }

            _localOrigin = _shakeTarget.localPosition;
        }

        private void OnEnable()
        {
            GameEvents.PlayerCaptured += OnPlayerCaptured;
        }

        private void OnDisable()
        {
            GameEvents.PlayerCaptured -= OnPlayerCaptured;
        }

        /// <summary>Scales shake intensity by chase proximity (0 = not chased, 1 = about to be caught) — called by <c>AttendantController</c> every frame while chasing.</summary>
        public void SetChaseIntensity(float intensity01)
        {
            _currentMagnitude = Mathf.Max(_currentMagnitude, Mathf.Clamp01(intensity01) * _captureShakeMagnitude * 0.4f);
            _remainingDuration = Mathf.Max(_remainingDuration, 0.1f);
        }

        private void OnPlayerCaptured()
        {
            _currentMagnitude = _captureShakeMagnitude;
            _remainingDuration = _captureShakeDuration;
        }

        private void Update()
        {
            if (_remainingDuration <= 0f)
            {
                _shakeTarget.localPosition = _localOrigin;
                return;
            }

            _remainingDuration -= Time.deltaTime;
            _currentMagnitude = Mathf.MoveTowards(_currentMagnitude, 0f, _decaySpeed * Time.deltaTime);
            _shakeTarget.localPosition = _localOrigin + Random.insideUnitSphere * _currentMagnitude;
        }
    }
}
