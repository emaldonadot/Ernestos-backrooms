using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Basic intensity-jitter + occasional full drop-out on a <see cref="Light"/>,
    /// matching PRD Section 15's "fluorescent light hum" direction — pairs with
    /// <see cref="RoomAmbience"/> for the audio side of the same fixture. Purely
    /// cosmetic, client-local visual noise (not gameplay state), so unlike procedural
    /// generation this uses <see cref="UnityEngine.Random"/> rather than a seeded RNG —
    /// see DECISIONS.md's determinism entry, which is about reproducible generation and
    /// co-op state, not every use of randomness in the game.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class FlickeringLight : MonoBehaviour
    {
        [SerializeField] private float _baseIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float _jitterAmount = 0.15f;
        [SerializeField] private float _jitterSpeed = 12f;
        [Tooltip("Chance per second of a brief full drop-out, on top of the continuous jitter.")]
        [SerializeField, Range(0f, 1f)] private float _dropoutChancePerSecond = 0.15f;
        [SerializeField] private float _dropoutDuration = 0.08f;

        [Header("Intensified (e.g. Attendant warning/hunt cues)")]
        [Tooltip("Multiplies jitter amount and dropout chance while SetIntensified(true) is active, on top of the always-on baseline flicker above.")]
        [SerializeField] private float _intensifiedJitterMultiplier = 2.5f;
        [SerializeField] private float _intensifiedDropoutMultiplier = 4f;

        private Light _light;
        private float _dropoutTimer;
        private float _noiseSeed;
        private bool _intensified;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _noiseSeed = Random.Range(0f, 1000f);
            if (_baseIntensity <= 0f)
            {
                _baseIntensity = _light.intensity;
            }
        }

        /// <summary>Defensive reset in case something disables this component outright (e.g. scene tooling) — snaps back to a clean intensity instead of staying stuck dim/dark.</summary>
        private void OnDisable()
        {
            if (_light != null)
            {
                _light.intensity = _baseIntensity;
            }
        }

        /// <summary>
        /// Toggled by <see cref="AttendantAppearanceController"/> during its Warning and
        /// Hunting phases — the corridor lights flicker gently all the time as ambient
        /// atmosphere, but noticeably harder in the few seconds before the Attendant
        /// appears and for as long as it's actively present.
        /// </summary>
        public void SetIntensified(bool intensified)
        {
            _intensified = intensified;
        }

        private void Update()
        {
            float jitterAmount = _intensified ? _jitterAmount * _intensifiedJitterMultiplier : _jitterAmount;
            float dropoutChance = _intensified ? _dropoutChancePerSecond * _intensifiedDropoutMultiplier : _dropoutChancePerSecond;

            if (_dropoutTimer > 0f)
            {
                _dropoutTimer -= Time.deltaTime;
                _light.intensity = 0f;
                return;
            }

            if (Random.value < dropoutChance * Time.deltaTime)
            {
                _dropoutTimer = _dropoutDuration;
                _light.intensity = 0f;
                return;
            }

            float noise = Mathf.PerlinNoise(_noiseSeed, Time.time * _jitterSpeed) * 2f - 1f;
            _light.intensity = Mathf.Max(0f, _baseIntensity + noise * jitterAmount * _baseIntensity);
        }
    }
}
