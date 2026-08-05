using System;
using System.Collections.Generic;
using System.Linq;

namespace EndlessRooms.Procedural
{
    /// <summary>
    /// The full abstract layout produced by <see cref="RoomGraphGenerator"/>: every room
    /// and every connection between them, independent of how (or whether) it gets
    /// instantiated into scene geometry.
    /// </summary>
    public sealed class RoomGraph
    {
        private readonly Dictionary<Guid, RoomNode> _nodes = new();
        private readonly List<RoomConnection> _connections = new();

        public Guid EntryNodeId { get; private set; }
        public Guid ExitNodeId { get; private set; }

        public IReadOnlyCollection<RoomNode> Nodes => _nodes.Values;
        public IReadOnlyList<RoomConnection> Connections => _connections;

        public RoomNode GetNode(Guid id) => _nodes[id];

        public bool TryGetNode(Guid id, out RoomNode node) => _nodes.TryGetValue(id, out node);

        public void AddNode(RoomNode node)
        {
            _nodes[node.Id] = node;
        }

        public void AddConnection(RoomConnection connection)
        {
            _connections.Add(connection);
        }

        public void SetEntry(Guid nodeId) => EntryNodeId = nodeId;

        public void SetExit(Guid nodeId) => ExitNodeId = nodeId;

        public bool HasConnection(Guid a, Guid b)
        {
            return _connections.Any(c => (c.FromId == a && c.ToId == b) || (c.FromId == b && c.ToId == a));
        }

        public IEnumerable<Guid> GetNeighborIds(Guid nodeId)
        {
            foreach (RoomConnection connection in _connections)
            {
                if (connection.FromId == nodeId)
                {
                    yield return connection.ToId;
                }
                else if (connection.ToId == nodeId)
                {
                    yield return connection.FromId;
                }
            }
        }
    }
}
