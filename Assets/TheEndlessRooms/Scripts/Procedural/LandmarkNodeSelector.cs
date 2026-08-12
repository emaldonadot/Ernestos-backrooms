using System;
using System.Collections.Generic;
using System.Linq;

namespace EndlessRooms.Procedural
{
    /// <summary>
    /// Picks which generated room becomes the guaranteed Milestone 8 landmark (The
    /// Atrium) — never the entry or exit, and at least <c>minHopsFromEntry</c> hops
    /// away so it doesn't show up right at spawn. Pure graph math, no Unity scene
    /// dependency, so it's EditMode-testable. Ties are broken by grid position (lowest
    /// x, then y) rather than graph/dictionary iteration order, so the choice is fully
    /// deterministic for a given seed.
    /// </summary>
    public static class LandmarkNodeSelector
    {
        /// <summary>The most-connected eligible node, or null if none qualifies.</summary>
        public static Guid? SelectLandmarkNode(RoomGraph graph, int minHopsFromEntry)
        {
            int excludeWithinHops = Math.Max(minHopsFromEntry - 1, 0);
            var tooClose = GetNodesWithinHops(graph, graph.EntryNodeId, excludeWithinHops);

            RoomNode best = null;
            int bestConnectionCount = -1;

            foreach (RoomNode node in graph.Nodes.OrderBy(n => n.GridPosition.x).ThenBy(n => n.GridPosition.y))
            {
                if (node.Id == graph.EntryNodeId || node.Id == graph.ExitNodeId || tooClose.Contains(node.Id))
                {
                    continue;
                }

                int connectionCount = graph.GetNeighborIds(node.Id).Count();
                if (connectionCount > bestConnectionCount)
                {
                    bestConnectionCount = connectionCount;
                    best = node;
                }
            }

            return best?.Id;
        }

        /// <summary>
        /// Not reused from <c>EndlessRooms.AI.RoomGraphPathfinder</c> (which has the same
        /// helper) — that type lives in the AI assembly, which already depends on
        /// Procedural, so referencing it back from here would be a circular asmdef
        /// reference. This is small enough that duplicating it is the simpler trade-off.
        /// </summary>
        private static HashSet<Guid> GetNodesWithinHops(RoomGraph graph, Guid fromId, int maxHops)
        {
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

            return visited;
        }
    }
}
