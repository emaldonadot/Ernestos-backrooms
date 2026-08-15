using System.Collections;
using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// The player's own scared-heartbeat reaction to being caught — a full-screen
    /// vignette that cycles through a handful of frames in time with a fast heartbeat
    /// sound, plus a one-shot scare scream. A separate GameEvents.PlayerCaptured
    /// reaction alongside GameOverController's "You Were Caught" panel (which sits in
    /// the clear center these vignette frames leave open) and CameraShakeEffect's
    /// shake — all three independently subscribe to the same capture event rather than
    /// one orchestrating the others.
    /// </summary>
    public sealed class HeartbeatVignetteController : MonoBehaviour
    {
        [SerializeField] private GameObject _vignetteRoot;
        [SerializeField] private Image _vignetteImage;
        [SerializeField] private Sprite[] _frames = System.Array.Empty<Sprite>();
        [Tooltip("How many heartbeats to animate through before the vignette clears — tune to roughly match the heartbeat clip's length.")]
        [SerializeField] private int _beatCount = 5;
        [SerializeField] private float _beatsPerSecond = 2f;

        [SerializeField] private AudioSource _screamAudioSource;
        [SerializeField] private AudioClip _screamSound;
        [SerializeField] private AudioSource _heartbeatAudioSource;
        [SerializeField] private AudioClip _heartbeatSound;

        private void OnEnable()
        {
            GameEvents.PlayerCaptured += HandlePlayerCaptured;
            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEvents.PlayerCaptured -= HandlePlayerCaptured;
        }

        private void HandlePlayerCaptured()
        {
            if (_screamAudioSource != null && _screamSound != null)
            {
                _screamAudioSource.PlayOneShot(_screamSound);
            }

            if (_heartbeatAudioSource != null && _heartbeatSound != null)
            {
                _heartbeatAudioSource.PlayOneShot(_heartbeatSound);
            }

            StartCoroutine(PlayVignette());
        }

        private IEnumerator PlayVignette()
        {
            if (_frames.Length == 0 || _vignetteImage == null)
            {
                yield break;
            }

            SetVisible(true);

            float beatDuration = _beatsPerSecond > 0f ? 1f / _beatsPerSecond : 0.5f;
            float frameDuration = beatDuration / _frames.Length;

            for (int beat = 0; beat < _beatCount; beat++)
            {
                for (int frame = 0; frame < _frames.Length; frame++)
                {
                    _vignetteImage.sprite = _frames[frame];
                    yield return new WaitForSeconds(frameDuration);
                }
            }

            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_vignetteRoot != null)
            {
                _vignetteRoot.SetActive(visible);
            }
        }
    }
}
