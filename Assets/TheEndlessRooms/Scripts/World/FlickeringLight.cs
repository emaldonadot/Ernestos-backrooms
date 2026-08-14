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

        private Light _light;
        private float _dropoutTimer;
        private float _noiseSeed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _noiseSeed = Random.Range(0f, 1000f);
            if (_baseIntensity <= 0f)
            {
                _baseIntensity = _light.intensity;
            }
        }

        /// <summary>
        /// Milestone 9's AttendantAppearanceController toggles this component's enabled
        /// state on/off as a warning cue rather than leaving it always-on — without this,
        /// disabling mid-flicker would leave the light stuck dim (or mid-dropout) instead
        /// of snapping back to a clean, normal intensity.
        /// </summary>
        private void OnDisable()
        {
            if (_light != null)
            {
                _light.intensity = _baseIntensity;
            }
        }

        private void Update()
        {
            if (_dropoutTimer > 0f)
            {
                _dropoutTimer -= Time.deltaTime;
                _light.intensity = 0f;
                return;
            }

            if (Random.value < _dropoutChancePerSecond * Time.deltaTime)
            {
                _dropoutTimer = _dropoutDuration;
                _light.intensity = 0f;
                return;
            }

            float noise = Mathf.PerlinNoise(_noiseSeed, Time.time * _jitterSpeed) * 2f - 1f;
            _light.intensity = Mathf.Max(0f, _baseIntensity + noise * _jitterAmount * _baseIntensity);
        }
    }
}
