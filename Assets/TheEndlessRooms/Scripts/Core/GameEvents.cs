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

        /// <summary>Raised with a room's stable Guid when a player's collider enters it. Consumed by the Map system without Core depending on Map or Procedural.</summary>
        public static event Action<Guid> RoomEntered;

        /// <summary>Raised once when the player reaches the exit condition.</summary>
        public static event Action LevelCompleted;

        public static void RaiseInteractionPerformed(GameObject instigator, IInteractable target)
        {
            InteractionPerformed?.Invoke(instigator, target);
        }

        public static void RaiseRoomEntered(Guid roomId)
        {
            RoomEntered?.Invoke(roomId);
        }

        public static void RaiseLevelCompleted()
        {
            LevelCompleted?.Invoke();
        }
    }
}
