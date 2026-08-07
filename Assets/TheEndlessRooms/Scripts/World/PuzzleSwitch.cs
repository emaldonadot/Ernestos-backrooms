using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// One lever in a <see cref="PuzzleGateController"/>'s switch sequence.
    /// <see cref="SwitchIndex"/> is fixed at placement time; interacting reports the
    /// activation to whichever controller subscribed, rather than knowing about the
    /// puzzle itself.
    /// </summary>
    public sealed class PuzzleSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _switchIndex;

        public int SwitchIndex => _switchIndex;

        public event Action<PuzzleSwitch> Activated;

        internal void Initialize(int switchIndex)
        {
            _switchIndex = switchIndex;
        }

        public string GetInteractionPrompt()
        {
            return $"Pull Switch {_switchIndex + 1}";
        }

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
            Activated?.Invoke(this);
        }
    }
}
