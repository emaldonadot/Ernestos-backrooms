using System;
using System.Collections.Generic;
using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.Map
{
    /// <summary>
    /// The player-built map's data layer: tracks which rooms have been entered or
    /// merely glimpsed from a neighbor, and the player's custom markers. Holds the
    /// ground-truth <see cref="RoomGraph"/> privately — nothing outside this class can
    /// read an undiscovered room's category or an unknown connection.
    /// </summary>
    public sealed class FieldLogService : IDisposable
    {
        private RoomGraph _worldGraph;
        private readonly Dictionary<Guid, RoomDiscoveryState> _discoveryStates = new();
        private readonly List<FieldMark> _marks = new();

        public FieldLogService()
        {
            GameEvents.RoomEntered += MarkRoomEntered;
        }

        public event Action DiscoveryChanged;
        public event Action MarksChanged;

        public Guid CurrentRoomId { get; private set; }

        public void Initialize(RoomGraph worldGraph)
        {
            _worldGraph = worldGraph;
            _discoveryStates.Clear();
            _marks.Clear();
        }

        public void MarkRoomEntered(Guid roomId)
        {
            if (_worldGraph == null || !_worldGraph.TryGetNode(roomId, out _))
            {
                return;
            }

            _discoveryStates[roomId] = RoomDiscoveryState.Entered;
            CurrentRoomId = roomId;

            foreach (Guid neighborId in _worldGraph.GetNeighborIds(roomId))
            {
                if (!_discoveryStates.ContainsKey(neighborId))
                {
                    _discoveryStates[neighborId] = RoomDiscoveryState.Glimpsed;
                }
            }

            DiscoveryChanged?.Invoke();
        }

        public IEnumerable<FieldLogRoomView> GetKnownRooms()
        {
            if (_worldGraph == null)
            {
                return Enumerable.Empty<FieldLogRoomView>();
            }

            var views = new List<FieldLogRoomView>();
            foreach (var entry in _discoveryStates)
            {
                if (entry.Value == RoomDiscoveryState.Unknown || !_worldGraph.TryGetNode(entry.Key, out RoomNode node))
                {
                    continue;
                }

                RoomCategory? category = entry.Value == RoomDiscoveryState.Entered ? node.Definition?.Category : null;
                views.Add(new FieldLogRoomView(node.Id, node.GridPosition, category, entry.Value));
            }

            return views;
        }

        public IEnumerable<(Guid FromId, Guid ToId)> GetKnownConnections()
        {
            if (_worldGraph == null)
            {
                return Enumerable.Empty<(Guid, Guid)>();
            }

            // A connection is only shown once one endpoint is Entered: merely glimpsing
            // a room (seeing a doorway lead to it) doesn't tell you what *that* room
            // connects to — only walking into it does.
            return _worldGraph.Connections
                .Where(c => IsEntered(c.FromId) || IsEntered(c.ToId))
                .Select(c => (c.FromId, c.ToId));
        }

        private bool IsEntered(Guid roomId)
        {
            return _discoveryStates.TryGetValue(roomId, out RoomDiscoveryState state) && state == RoomDiscoveryState.Entered;
        }

        public IReadOnlyList<FieldMark> Marks => _marks;

        public FieldMark AddMark(Guid roomId, Vector2 localOffset, FieldMarkType type, string note, string ownerId = "local")
        {
            var mark = new FieldMark(Guid.NewGuid(), roomId, localOffset, type, note, ownerId);
            _marks.Add(mark);
            MarksChanged?.Invoke();
            return mark;
        }

        public bool RemoveMark(Guid markId)
        {
            int removed = _marks.RemoveAll(m => m.Id == markId);
            if (removed > 0)
            {
                MarksChanged?.Invoke();
            }

            return removed > 0;
        }

        /// <summary>
        /// Sets an exact discovery state loaded from a save, bypassing
        /// <see cref="MarkRoomEntered"/>'s neighbor-promotion logic — that logic is
        /// only correct for live discovery, not for replaying a snapshot where a room
        /// might be <see cref="RoomDiscoveryState.Glimpsed"/> without its "entered"
        /// neighbor being restored yet.
        /// </summary>
        public void RestoreDiscoveryState(Guid roomId, RoomDiscoveryState state)
        {
            _discoveryStates[roomId] = state;
        }

        public void RestoreCurrentRoomId(Guid roomId)
        {
            CurrentRoomId = roomId;
        }

        /// <summary>Re-adds a mark with its original Id preserved, loaded from a save — <see cref="AddMark"/> always mints a new Id, which would break Remove-by-Id round-tripping.</summary>
        public void RestoreMark(FieldMark mark)
        {
            _marks.Add(mark);
        }

        public void Dispose()
        {
            GameEvents.RoomEntered -= MarkRoomEntered;
        }
    }
}
