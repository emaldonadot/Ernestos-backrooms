using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Flips to unlocked the first time the UV beam is switched on anywhere —
    /// <see cref="EndlessRooms.Player.PlayerUvFlashlight"/> only allows that once it's
    /// powered, so a single global toggle is an accurate enough proxy for "the player has
    /// seen the bathroom code" without needing to check room bounds here too.
    /// </summary>
    public sealed class UvCodeKnownGate : MonoBehaviour, IProgressionGate
    {
        public bool IsUnlocked { get; private set; }

        public event Action Changed;

        private void OnEnable()
        {
            GameEvents.UvLightToggled += HandleUvLightToggled;
        }

        private void OnDisable()
        {
            GameEvents.UvLightToggled -= HandleUvLightToggled;
        }

        private void HandleUvLightToggled(bool isOn)
        {
            if (!isOn || IsUnlocked)
            {
                return;
            }

            IsUnlocked = true;
            Changed?.Invoke();
        }
    }
}
