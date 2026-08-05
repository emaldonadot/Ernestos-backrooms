using System;
using UnityEngine;

namespace EndlessRooms.Procedural
{
    /// <summary>One placed room in the abstract graph, before any spatial instantiation.</summary>
    public sealed class RoomNode
    {
        public RoomNode(Guid id, RoomDefinition definition, Vector2Int gridPosition)
        {
            Id = id;
            Definition = definition;
            GridPosition = gridPosition;
        }

        public Guid Id { get; }
        public RoomDefinition Definition { get; }
        public Vector2Int GridPosition { get; }
    }
}
