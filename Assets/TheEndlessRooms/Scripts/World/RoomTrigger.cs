using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Trigger volume covering a room's interior; raises <see cref="GameEvents.RoomEntered"/>
    /// with this room's stable id when the player enters. <see cref="RoomId"/> is wired
    /// by <see cref="ProceduralLevelBuilder"/> at instantiation time, the same way
    /// <see cref="RoomInstance"/> is.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomTrigger : MonoBehaviour
    {
        public Guid RoomId { get; private set; }

        internal void Initialize(Guid roomId)
        {
            RoomId = roomId;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameEvents.RaiseRoomEntered(RoomId);
            }
        }
    }
}
