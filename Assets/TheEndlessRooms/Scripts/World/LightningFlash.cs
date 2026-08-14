using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Randomly spikes a Light's intensity to simulate distant lightning, only while
    /// <see cref="SetStorming"/> is active — used by AttendantAppearanceController to
    /// turn the courtyard sky stormy for as long as the Attendant is warning/hunting,
    /// separate from the moonlight's own steady baseline intensity.
    /// </summary>
    public sealed class LightningFlash : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private float _baseIntensity = 0.12f;
        [SerializeField] private float _flashIntensity = 3.5f;
        [SerializeField] private float _flashDuration = 0.12f;
        [Tooltip("Chance per second of a flash while storming.")]
        [SerializeField, Range(0f, 2f)] private float _flashChancePerSecond = 0.25f;

        private bool _storming;
        private float _flashTimer;

        /// <summary>Raised the instant a flash triggers — AttendantAppearanceController pairs a thunder sound to this instead of running its own separate random timer.</summary>
        public event System.Action OnFlash;

        public void SetStorming(bool storming)
        {
            _storming = storming;
            if (!storming && _light != null)
            {
                _light.intensity = _baseIntensity;
            }
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    _light.intensity = _baseIntensity;
                }

                return;
            }

            if (!_storming)
            {
                return;
            }

            if (Random.value < _flashChancePerSecond * Time.deltaTime)
            {
                _light.intensity = _flashIntensity;
                _flashTimer = _flashDuration;
                OnFlash?.Invoke();
            }
        }
    }
}
