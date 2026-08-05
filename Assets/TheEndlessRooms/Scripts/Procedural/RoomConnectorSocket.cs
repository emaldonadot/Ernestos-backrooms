using System;
using UnityEngine;

namespace EndlessRooms.Procedural
{
    /// <summary>A connection point on a room prefab that a corridor/door can snap to.</summary>
    [Serializable]
    public struct RoomConnectorSocket
    {
        public Vector3 LocalPosition;
        public float LocalYRotation;
        public bool RequiresDoor;
    }
}
