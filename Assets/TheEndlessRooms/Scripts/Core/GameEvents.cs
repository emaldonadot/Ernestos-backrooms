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

        /// <summary>Raised when a <c>HidingSpot</c> is entered/exited. Every <see cref="IDetectable"/> implementation (PC and VR) mirrors this into its own <c>IsHidden</c> rather than HidingSpot (World) referencing the player rig (Player) directly.</summary>
        public static event Action<bool> PlayerHiddenChanged;

        /// <summary>Raised by Milestone 7's Attendant when it catches the player during Chase. <c>RespawnController</c> (Persistence) reloads the last checkpoint save in response.</summary>
        public static event Action PlayerCaptured;

        /// <summary>Raised by a <c>FieldNote</c> (World) on interact, carrying its text fragment. <c>FieldNoteUI</c> (UI) displays it — decoupled the same way as RoomEntered/LevelCompleted, so EndlessRooms.UI has no dependency on EndlessRooms.World.</summary>
        public static event Action<string> FieldNoteOpened;

        /// <summary>Raised with the selected inventory item's <see cref="InventoryItemDefinition.ItemId"/> when the player presses UseItem — item-specific behavior (PlayerFlashlight, PlayerUvFlashlight, CassetteMessagePlayer) lives on whichever component owns that reaction, not here.</summary>
        public static event Action<string> ItemUseRequested;

        /// <summary>Raised by <c>PlayerUvFlashlight</c> whenever its beam turns on/off — a world prop with a UV-only hidden clue reacts to this rather than referencing the Player component directly.</summary>
        public static event Action<bool> UvLightToggled;

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

        public static void RaisePlayerHiddenChanged(bool isHidden)
        {
            PlayerHiddenChanged?.Invoke(isHidden);
        }

        public static void RaisePlayerCaptured()
        {
            PlayerCaptured?.Invoke();
        }

        public static void RaiseFieldNoteOpened(string fragmentText)
        {
            FieldNoteOpened?.Invoke(fragmentText);
        }

        public static void RaiseItemUseRequested(string itemId)
        {
            ItemUseRequested?.Invoke(itemId);
        }

        public static void RaiseUvLightToggled(bool isOn)
        {
            UvLightToggled?.Invoke(isOn);
        }
    }
}
