using System;

namespace EndlessRooms.Procedural
{
    /// <summary>An edge between two rooms; <see cref="FromDirection"/> points from <see cref="FromId"/> toward <see cref="ToId"/>.</summary>
    public sealed class RoomConnection
    {
        public RoomConnection(Guid fromId, Guid toId, Direction fromDirection)
        {
            FromId = fromId;
            ToId = toId;
            FromDirection = fromDirection;
        }

        public Guid FromId { get; }
        public Guid ToId { get; }
        public Direction FromDirection { get; }
    }
}
