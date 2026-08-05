using System;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.Map
{
    /// <summary>
    /// The only thing <see cref="FieldLogService"/> exposes about a room — never the
    /// underlying <see cref="RoomNode"/>, so rendering can't accidentally read
    /// undiscovered ground truth. <see cref="Category"/> is null while
    /// <see cref="State"/> is <see cref="RoomDiscoveryState.Glimpsed"/>.
    /// </summary>
    public readonly struct FieldLogRoomView
    {
        public FieldLogRoomView(Guid roomId, Vector2Int gridPosition, RoomCategory? category, RoomDiscoveryState state)
        {
            RoomId = roomId;
            GridPosition = gridPosition;
            Category = category;
            State = state;
        }

        public Guid RoomId { get; }
        public Vector2Int GridPosition { get; }
        public RoomCategory? Category { get; }
        public RoomDiscoveryState State { get; }
    }
}
