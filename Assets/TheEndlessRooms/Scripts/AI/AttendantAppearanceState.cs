using System;

namespace EndlessRooms.AI
{
    public enum AttendantAppearancePhase
    {
        /// <summary>Not present. Lights normal.</summary>
        Idle,

        /// <summary>About to appear — lights flicker, a sting sound plays.</summary>
        Warning,

        /// <summary>Active and hunting for a limited window.</summary>
        Hunting,
    }

    /// <summary>
    /// Pure phase/timer logic for the Attendant's periodic appear/disappear cycle
    /// (Milestone 9 — "not constantly chasing me," a warning beat before it shows up,
    /// and it gives up and vanishes if it doesn't find the player within the hunt
    /// window) — no MonoBehaviour dependency, so it's EditMode-testable directly.
    /// <see cref="EndlessRooms.AI.AttendantAppearanceController"/> wraps this, turning
    /// phase changes into lights/sound/Attendant-activation side effects.
    /// </summary>
    public sealed class AttendantAppearanceState
    {
        private readonly float _minIdleSeconds;
        private readonly float _maxIdleSeconds;
        private readonly float _warningDurationSeconds;
        private readonly float _huntDurationSeconds;
        private readonly Func<float, float, float> _idleDurationRoll;

        private float _timer;

        public AttendantAppearancePhase Phase { get; private set; }

        /// <param name="idleDurationRoll">Injectable random-range function (defaults to <see cref="UnityEngine.Random.Range(float, float)"/>) so tests can supply a fixed value instead of a real random roll.</param>
        public AttendantAppearanceState(
            float minIdleSeconds,
            float maxIdleSeconds,
            float warningDurationSeconds,
            float huntDurationSeconds,
            Func<float, float, float> idleDurationRoll = null)
        {
            _minIdleSeconds = minIdleSeconds;
            _maxIdleSeconds = maxIdleSeconds;
            _warningDurationSeconds = warningDurationSeconds;
            _huntDurationSeconds = huntDurationSeconds;
            _idleDurationRoll = idleDurationRoll ?? UnityEngine.Random.Range;

            EnterIdle();
        }

        /// <summary>Advances the timer; returns true exactly on the frame the phase changes.</summary>
        public bool Tick(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
            {
                return false;
            }

            switch (Phase)
            {
                case AttendantAppearancePhase.Idle:
                    EnterWarning();
                    break;
                case AttendantAppearancePhase.Warning:
                    EnterHunting();
                    break;
                case AttendantAppearancePhase.Hunting:
                    EnterIdle();
                    break;
            }

            return true;
        }

        /// <summary>Immediately forces back to Idle regardless of the current timer — e.g. the player was just caught, so the current hunt is already over.</summary>
        public void ForceIdle()
        {
            EnterIdle();
        }

        private void EnterIdle()
        {
            Phase = AttendantAppearancePhase.Idle;
            _timer = _idleDurationRoll(_minIdleSeconds, _maxIdleSeconds);
        }

        private void EnterWarning()
        {
            Phase = AttendantAppearancePhase.Warning;
            _timer = _warningDurationSeconds;
        }

        private void EnterHunting()
        {
            Phase = AttendantAppearancePhase.Hunting;
            _timer = _huntDurationSeconds;
        }
    }
}
