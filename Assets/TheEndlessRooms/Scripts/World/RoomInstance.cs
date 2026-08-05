using System;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Runtime handle to one instantiated modular room: its four walls (one per
    /// <see cref="Direction"/>) and the socket transforms a level builder aligns
    /// doors to. Wired once on the prefab via the Inspector, never found by name.
    /// </summary>
    public sealed class RoomInstance : MonoBehaviour
    {
        [SerializeField] private GameObject _wallNorth;
        [SerializeField] private GameObject _wallEast;
        [SerializeField] private GameObject _wallSouth;
        [SerializeField] private GameObject _wallWest;

        [SerializeField] private Transform _socketNorth;
        [SerializeField] private Transform _socketEast;
        [SerializeField] private Transform _socketSouth;
        [SerializeField] private Transform _socketWest;

        public GameObject GetWall(Direction direction)
        {
            return direction switch
            {
                Direction.North => _wallNorth,
                Direction.East => _wallEast,
                Direction.South => _wallSouth,
                Direction.West => _wallWest,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
            };
        }

        public Transform GetSocket(Direction direction)
        {
            return direction switch
            {
                Direction.North => _socketNorth,
                Direction.East => _socketEast,
                Direction.South => _socketSouth,
                Direction.West => _socketWest,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
            };
        }

        public void OpenWall(Direction direction)
        {
            GameObject wall = GetWall(direction);
            if (wall != null)
            {
                wall.SetActive(false);
            }
        }
    }
}
