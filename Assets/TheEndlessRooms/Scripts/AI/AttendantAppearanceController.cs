using EndlessRooms.Core;
using EndlessRooms.World;
using UnityEngine;

namespace EndlessRooms.AI
{
    /// <summary>
    /// Milestone 9: wraps <see cref="AttendantController"/> with a periodic
    /// appear/disappear cycle instead of it being a constant, always-hunting presence —
    /// idle for a while, warn (flicker lights + a sting sound), activate the Attendant
    /// for a limited hunt window, then hide it again if it hasn't caught the player, and
    /// repeat. Doesn't touch AttendantController's own patrol/investigate/chase/search
    /// logic at all, just when it's active and where it (re)appears from
    /// (<see cref="AttendantController.ResetToHomePosition"/>).
    /// </summary>
    public sealed class AttendantAppearanceController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GameObject _attendantGo;
        [SerializeField] private AttendantController _attendant;

        [Header("Timing (seconds)")]
        [SerializeField] private float _minIdleSeconds = 20f;
        [SerializeField] private float _maxIdleSeconds = 45f;
        [SerializeField] private float _warningDurationSeconds = 3f;
        [SerializeField] private float _huntDurationSeconds = 25f;

        [Header("Warning cues")]
        [Tooltip("These flicker gently all the time as ambient atmosphere (see FlickeringLight's own baseline); this controller intensifies that flicker during the Warning and Hunting phases only.")]
        [SerializeField] private FlickeringLight[] _warningLights = System.Array.Empty<FlickeringLight>();
        [SerializeField] private AudioSource _warningAudioSource;
        [SerializeField] private AudioClip _warningSound;

        private AttendantAppearanceState _state;
        private AttendantAppearancePhase _lastAppliedPhase;
        private bool _hasAppliedPhase;

        private void Awake()
        {
            _state = new AttendantAppearanceState(_minIdleSeconds, _maxIdleSeconds, _warningDurationSeconds, _huntDurationSeconds);
        }

        private void OnEnable()
        {
            GameEvents.PlayerCaptured += ForceIdle;
            ApplyPhase(_state.Phase, forceReapply: true);
        }

        private void OnDisable()
        {
            GameEvents.PlayerCaptured -= ForceIdle;
        }

        private void Update()
        {
            if (_state.Tick(Time.deltaTime))
            {
                ApplyPhase(_state.Phase, forceReapply: false);
            }
        }

        /// <summary>Wired to <see cref="GameEvents.PlayerCaptured"/> above — ends the current hunt immediately rather than waiting out its timer, since the attempt is already over.</summary>
        public void ForceIdle()
        {
            _state.ForceIdle();
            ApplyPhase(_state.Phase, forceReapply: true);
        }

        private void ApplyPhase(AttendantAppearancePhase phase, bool forceReapply)
        {
            if (!forceReapply && _hasAppliedPhase && phase == _lastAppliedPhase)
            {
                return;
            }

            _lastAppliedPhase = phase;
            _hasAppliedPhase = true;

            switch (phase)
            {
                case AttendantAppearancePhase.Idle:
                    SetAttendantActive(false);
                    SetLightsIntensified(false);
                    break;
                case AttendantAppearancePhase.Warning:
                    SetLightsIntensified(true);
                    if (_warningAudioSource != null && _warningSound != null)
                    {
                        _warningAudioSource.PlayOneShot(_warningSound);
                    }

                    break;
                case AttendantAppearancePhase.Hunting:
                    SetLightsIntensified(true);
                    SetAttendantActive(true);
                    break;
            }
        }

        private void SetAttendantActive(bool active)
        {
            if (_attendantGo != null)
            {
                _attendantGo.SetActive(active);
            }

            if (active && _attendant != null)
            {
                _attendant.ResetToHomePosition();
            }
        }

        private void SetLightsIntensified(bool intensified)
        {
            foreach (FlickeringLight light in _warningLights)
            {
                if (light != null)
                {
                    light.SetIntensified(intensified);
                }
            }
        }
    }
}
