using System;
using System.Collections.Generic;
using System.Linq;

namespace EndlessRooms.Procedural
{
    public readonly struct RoomGraphValidationResult
    {
        public RoomGraphValidationResult(bool isValid, IReadOnlyList<Guid> unreachableMandatoryNodeIds, bool exitReachable)
        {
            IsValid = isValid;
            UnreachableMandatoryNodeIds = unreachableMandatoryNodeIds;
            ExitReachable = exitReachable;
        }

        public bool IsValid { get; }
        public IReadOnlyList<Guid> UnreachableMandatoryNodeIds { get; }
        public bool ExitReachable { get; }
    }

    /// <summary>
    /// Confirms a generated graph is actually playable: the exit must be reachable from
    /// the entry, and so must every room flagged <see cref="RoomDefinition.IsMandatory"/>.
    /// </summary>
    public static class RoomGraphValidator
    {
        public static RoomGraphValidationResult Validate(RoomGraph graph)
        {
            HashSet<Guid> reachable = ReachableFrom(graph, graph.EntryNodeId);

            bool exitReachable = reachable.Contains(graph.ExitNodeId);

            List<Guid> unreachableMandatory = graph.Nodes
                .Where(n => n.Definition != null && n.Definition.IsMandatory && !reachable.Contains(n.Id))
                .Select(n => n.Id)
                .ToList();

            bool isValid = exitReachable && unreachableMandatory.Count == 0;
            return new RoomGraphValidationResult(isValid, unreachableMandatory, exitReachable);
        }

        private static HashSet<Guid> ReachableFrom(RoomGraph graph, Guid startId)
        {
            var visited = new HashSet<Guid>();
            if (!graph.TryGetNode(startId, out _))
            {
                return visited;
            }

            var queue = new Queue<Guid>();
            queue.Enqueue(startId);
            visited.Add(startId);

            while (queue.Count > 0)
            {
                Guid currentId = queue.Dequeue();
                foreach (Guid neighborId in graph.GetNeighborIds(currentId))
                {
                    if (visited.Add(neighborId))
                    {
                        queue.Enqueue(neighborId);
                    }
                }
            }

            return visited;
        }
    }
}
