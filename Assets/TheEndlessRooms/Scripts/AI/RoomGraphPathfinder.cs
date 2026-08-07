using System;
using System.Collections.Generic;
using System.Linq;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.AI
{
    /// <summary>
    /// BFS over <see cref="RoomGraph.GetNeighborIds"/> — the same technique
    /// <c>RoomGraphValidator</c>'s reachability pass already uses. The level *is* a
    /// graph already (Milestone 2), so The Attendant paths along it instead of needing
    /// a baked NavMesh; see the "no NavMesh" call in
    /// docs/features/milestone-7-horror-prototype.md. Pure C#, no Unity scene
    /// dependency beyond <see cref="Vector3"/> math, so it's fully EditMode-testable.
    /// </summary>
    public static class RoomGraphPathfinder
    {
        /// <summary>Ordered node ids from <paramref name="fromId"/> to <paramref name="toId"/> inclusive, or empty if unreachable.</summary>
        public static List<Guid> FindPath(RoomGraph graph, Guid fromId, Guid toId)
        {
            if (fromId == toId)
            {
                return new List<Guid> { fromId };
            }

            var visited = new HashSet<Guid> { fromId };
            var cameFrom = new Dictionary<Guid, Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(fromId);

            while (queue.Count > 0)
            {
                Guid current = queue.Dequeue();
                if (current == toId)
                {
                    return ReconstructPath(cameFrom, fromId, toId);
                }

                foreach (Guid neighbor in graph.GetNeighborIds(current))
                {
                    if (visited.Add(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return new List<Guid>();
        }

        /// <summary>All node ids reachable within <paramref name="maxHops"/> of <paramref name="fromId"/>, including itself.</summary>
        public static List<Guid> GetNodesWithinHops(RoomGraph graph, Guid fromId, int maxHops)
        {
            var result = new List<Guid> { fromId };
            var visited = new HashSet<Guid> { fromId };
            var frontier = new List<Guid> { fromId };

            for (int hop = 0; hop < maxHops; hop++)
            {
                var nextFrontier = new List<Guid>();
                foreach (Guid nodeId in frontier)
                {
                    foreach (Guid neighbor in graph.GetNeighborIds(nodeId))
                    {
                        if (visited.Add(neighbor))
                        {
                            result.Add(neighbor);
                            nextFrontier.Add(neighbor);
                        }
                    }
                }

                frontier = nextFrontier;
                if (frontier.Count == 0)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>The node whose world position (via <paramref name="nodeToWorld"/>) is closest to <paramref name="worldPosition"/>.</summary>
        public static Guid FindNearestNode(RoomGraph graph, Vector3 worldPosition, Func<Guid, Vector3> nodeToWorld)
        {
            return graph.Nodes
                .Select(node => node.Id)
                .OrderBy(id => (nodeToWorld(id) - worldPosition).sqrMagnitude)
                .First();
        }

        private static List<Guid> ReconstructPath(Dictionary<Guid, Guid> cameFrom, Guid fromId, Guid toId)
        {
            var path = new List<Guid> { toId };
            Guid current = toId;
            while (current != fromId)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
