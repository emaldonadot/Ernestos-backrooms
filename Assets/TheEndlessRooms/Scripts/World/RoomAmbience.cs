using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Loops a low-volume ambience clip (fluorescent hum, ventilation) per PRD Section
    /// 15. A basic hook, not full audio occlusion simulation — occlusion through
    /// walls/doors is deferred to a later polish pass (see
    /// docs/features/milestone-7-horror-prototype.md's Scope section).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class RoomAmbience : MonoBehaviour
    {
        [SerializeField] private AudioClip _ambienceClip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.2f;

        private void Awake()
        {
            var source = GetComponent<AudioSource>();
            source.clip = _ambienceClip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = _volume;

            if (_ambienceClip != null)
            {
                source.Play();
            }
        }
    }
}
