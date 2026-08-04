using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Carries who is performing an interaction. Passed explicitly rather than resolved
    /// from a global "the player" reference so multiple local/remote actors can interact
    /// once co-op is added, without changing any <see cref="IInteractable"/> implementation.
    /// </summary>
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject instigator)
        {
            Instigator = instigator;
        }

        public GameObject Instigator { get; }
    }
}
