using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Flips to unlocked the moment the UV flashlight is combined with a battery —
    /// distinct from <see cref="UvCodeKnownGate"/> (which tracks whether the player has
    /// actually switched it on at least once): this one drives the bathroom clue's own
    /// spider, which should appear as soon as the player *has what they need*, before
    /// they've necessarily walked to the bathroom and tried it.
    /// </summary>
    public sealed class UvPoweredGate : MonoBehaviour, IProgressionGate
    {
        public bool IsUnlocked { get; private set; }

        public event Action Changed;

        private void OnEnable()
        {
            GameEvents.UvFlashlightPowered += HandlePowered;
        }

        private void OnDisable()
        {
            GameEvents.UvFlashlightPowered -= HandlePowered;
        }

        private void HandlePowered()
        {
            if (IsUnlocked)
            {
                return;
            }

            IsUnlocked = true;
            Changed?.Invoke();
        }
    }
}
