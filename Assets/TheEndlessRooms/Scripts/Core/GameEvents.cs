using System;
using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Minimal static event bus for cross-system notifications that don't warrant a
    /// full message-queue framework. Keep this list small and specific — broad or
    /// generic events belong on the systems that own the data instead.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameObject, IInteractable> InteractionPerformed;

        public static void RaiseInteractionPerformed(GameObject instigator, IInteractable target)
        {
            InteractionPerformed?.Invoke(instigator, target);
        }
    }
}
