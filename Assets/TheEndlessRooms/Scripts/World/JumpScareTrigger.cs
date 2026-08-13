using System.Collections;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A harmless, one-shot scare — Milestone 9's distinction from the real threat (The
    /// Attendant, unchanged from Milestone 7): no perception, no state machine, no
    /// danger. On the player entering the trigger volume, a placeholder visual appears
    /// briefly with a sting sound, then the trigger permanently disables itself. Same
    /// "player-tag trigger volume" shape as <see cref="RoomTrigger"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class JumpScareTrigger : MonoBehaviour
    {
        [Tooltip("Placeholder scare visual (a ghost/monster stand-in) — starts inactive, shown only for _visualDuration seconds when triggered.")]
        [SerializeField] private GameObject _scareVisual;
        [SerializeField] private AudioClip _scareSound;
        [SerializeField] private float _visualDuration = 1.5f;

        private Collider _collider;
        private AudioSource _audioSource;
        private bool _hasTriggered;

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
            if (_hasTriggered || !other.CompareTag("Player"))
            {
                return;
            }

            _hasTriggered = true;
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
        }
    }
}
