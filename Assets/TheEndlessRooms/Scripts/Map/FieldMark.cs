using System;
using UnityEngine;

namespace EndlessRooms.Map
{
    /// <summary>
    /// A player-placed map annotation. <see cref="OwnerId"/> is an unused placeholder
    /// until co-op needs to distinguish personal from shared markers (PRD Section 20).
    /// </summary>
    public sealed class FieldMark
    {
        public FieldMark(Guid id, Guid roomId, Vector2 localOffset, FieldMarkType type, string note, string ownerId)
        {
            Id = id;
            RoomId = roomId;
            LocalOffset = localOffset;
            Type = type;
            Note = note;
            OwnerId = ownerId;
        }

        public Guid Id { get; }
        public Guid RoomId { get; }
        public Vector2 LocalOffset { get; }
        public FieldMarkType Type { get; }
        public string Note { get; }
        public string OwnerId { get; }
    }
}
