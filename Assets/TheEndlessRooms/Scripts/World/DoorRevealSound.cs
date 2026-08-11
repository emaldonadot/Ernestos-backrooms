using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Plays a one-shot audio cue the first time a specific door is opened — used for
    /// the secret room's reveal sting. Reuses <see cref="Door.DoorToggled"/> (already
    /// built in Milestone 7 for the Attendant's door-reactivity) rather than adding an
    /// audio hook to <see cref="Door"/> itself, since this is specific to one door, not
    /// a general door feature every door needs.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class DoorRevealSound : MonoBehaviour
    {
        [SerializeField] private Door _door;
        [SerializeField] private AudioClip _revealClip;

        private AudioSource _audioSource;
        private bool _hasPlayed;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_door != null)
            {
                _door.DoorToggled += HandleDoorToggled;
            }
        }

        private void OnDisable()
        {
            if (_door != null)
            {
                _door.DoorToggled -= HandleDoorToggled;
            }
        }

        private void HandleDoorToggled(Door door)
        {
            if (!_hasPlayed && door.IsOpen && _revealClip != null)
            {
                _hasPlayed = true;
                _audioSource.PlayOneShot(_revealClip);
            }
        }
    }
}
