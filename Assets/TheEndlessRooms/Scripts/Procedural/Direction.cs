using System;
using UnityEngine;

namespace EndlessRooms.Procedural
{
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.East => Direction.West,
                Direction.South => Direction.North,
                Direction.West => Direction.East,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
            };
        }

        public static Vector2Int ToGridOffset(this Direction direction)
        {
            return direction switch
            {
                Direction.North => new Vector2Int(0, 1),
                Direction.East => new Vector2Int(1, 0),
                Direction.South => new Vector2Int(0, -1),
                Direction.West => new Vector2Int(-1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
            };
        }

        public static readonly Direction[] All =
        {
            Direction.North, Direction.East, Direction.South, Direction.West,
        };
    }
}
