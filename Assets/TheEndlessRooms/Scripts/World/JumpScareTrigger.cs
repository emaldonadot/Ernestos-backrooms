using System.Collections;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A harmless scare — Milestone 9's distinction from the real threat (The
    /// Attendant, unchanged from Milestone 7): no perception, no state machine, no
    /// danger, and entirely independent of the Attendant's own appearance/storm cycle —
    /// it can catch the player any time they wander back into the room, storm or not.
    /// On the player entering the trigger volume, a placeholder visual appears briefly
    /// with a sting sound, then the trigger goes on cooldown (not a permanent
    /// one-and-done) so it can startle the player again later. Same "player-tag trigger
    /// volume" shape as <see cref="RoomTrigger"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class JumpScareTrigger : MonoBehaviour
    {
        [Tooltip("Placeholder scare visual (a ghost/monster stand-in) — starts inactive, shown only for _visualDuration seconds when triggered.")]
        [SerializeField] private GameObject _scareVisual;
        [SerializeField] private AudioClip _scareSound;
        [SerializeField] private float _visualDuration = 1.5f;
        [Tooltip("Seconds after a scare before this trigger can fire again.")]
        [SerializeField] private float _cooldownSeconds = 25f;

        private Collider _collider;
        private AudioSource _audioSource;
        private bool _isCoolingDown;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _audioSource = GetComponent<AudioSource>();

            if (_scareVisual != null)
            {
                _scareVisual.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCoolingDown || !other.CompareTag("Player"))
            {
                return;
            }

            _isCoolingDown = true;
            _collider.enabled = false;
            StartCoroutine(PlayScare());
        }

        private IEnumerator PlayScare()
        {
            if (_scareVisual != null)
            {
                _scareVisual.SetActive(true);
            }

            if (_scareSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_scareSound);
            }

            yield return new WaitForSeconds(_visualDuration);

            if (_scareVisual != null)
            {
                _scareVisual.SetActive(false);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, _cooldownSeconds - _visualDuration));

            _collider.enabled = true;
            _isCoolingDown = false;
        }
    }
}
