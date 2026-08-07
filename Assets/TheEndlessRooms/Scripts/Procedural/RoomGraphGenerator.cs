using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EndlessRooms.Procedural
{
    /// <summary>
    /// Builds a <see cref="RoomGraph"/> from a seed: a critical path from the entry
    /// definition to the exit definition, optional branches filling out the remaining
    /// room count, and a final pass that connects any rooms that ended up adjacent and
    /// category-compatible (turning some branches into loops/junctions). Every random
    /// choice goes through a <see cref="System.Random"/> seeded from
    /// <see cref="RoomGraphGenerationSettings.Seed"/> — never <c>UnityEngine.Random</c> —
    /// so the same seed always produces the same graph. Node <see cref="Guid"/>s are
    /// also drawn from that same seeded stream (see <see cref="NextGuid"/>) rather than
    /// <see cref="Guid.NewGuid"/>, so the same seed reproduces the same room/door
    /// identities too — required for save data to reattach to the right rooms after a
    /// reload regenerates the world from scratch.
    /// </summary>
    public static class RoomGraphGenerator
    {
        /// <summary>Generates and validates a graph, retrying with a deterministically derived sub-seed on failure.</summary>
        public static RoomGraph GenerateValidated(RoomGraphGenerationSettings settings, int maxAttempts = 20)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int attemptSeed = unchecked(settings.Seed * 397 + attempt);
                RoomGraph graph = Generate(settings, attemptSeed);

                if (RoomGraphValidator.Validate(graph).IsValid)
                {
                    return graph;
                }
            }

            throw new InvalidOperationException(
                $"Could not generate a valid room graph for seed {settings.Seed} in {maxAttempts} attempts. " +
                "Check that FillerDefinitions/ExitDefinition allow enough neighbor combinations to reach the target RoomCount.");
        }

        public static RoomGraph Generate(RoomGraphGenerationSettings settings, int seedOverride)
        {
            if (settings.EntryDefinition == null || settings.ExitDefinition == null)
            {
                throw new ArgumentException("Entry and exit definitions are required.", nameof(settings));
            }

            var rng = new System.Random(seedOverride);
            var graph = new RoomGraph();
            var occupied = new Dictionary<Vector2Int, Guid>();

            RoomNode entry = PlaceNode(graph, occupied, rng, settings.EntryDefinition, Vector2Int.zero);
            graph.SetEntry(entry.Id);

            int criticalPathRooms = Math.Max(2, settings.RoomCount / 2);
            var pathNodes = new List<RoomNode> { entry };

            while (pathNodes.Count < criticalPathRooms)
            {
                RoomNode current = pathNodes[^1];
                if (!TryPlaceNeighbor(graph, occupied, rng, current, settings.FillerDefinitions, out RoomNode placed))
                {
                    break;
                }

                pathNodes.Add(placed);
            }

            if (!TryPlaceExitNearAny(graph, occupied, rng, pathNodes, settings.ExitDefinition, out RoomNode exitNode))
            {
                // Leave the graph without a reachable exit; the validator will reject it
                // and GenerateValidated will retry with a different sub-seed.
                return graph;
            }

            graph.SetExit(exitNode.Id);

            var allNodes = new List<RoomNode>(graph.Nodes);
            int branchFailuresInARow = 0;
            while (allNodes.Count < settings.RoomCount && branchFailuresInARow < allNodes.Count + 1)
            {
                RoomNode branchFrom = allNodes[rng.Next(allNodes.Count)];
                if (TryPlaceNeighbor(graph, occupied, rng, branchFrom, settings.FillerDefinitions, out RoomNode placed))
                {
                    allNodes.Add(placed);
                    branchFailuresInARow = 0;
                }
                else
                {
                    branchFailuresInARow++;
                }
            }

            AddLoopConnections(graph, occupied);

            return graph;
        }

        private static RoomNode PlaceNode(RoomGraph graph, Dictionary<Vector2Int, Guid> occupied, System.Random rng, RoomDefinition definition, Vector2Int position)
        {
            var node = new RoomNode(NextGuid(rng), definition, position);
            graph.AddNode(node);
            occupied[position] = node.Id;
            return node;
        }

        /// <summary>Draws a Guid from the seeded stream instead of <see cref="Guid.NewGuid"/> so it reproduces deterministically for a given seed.</summary>
        private static Guid NextGuid(System.Random rng)
        {
            var bytes = new byte[16];
            rng.NextBytes(bytes);
            return new Guid(bytes);
        }

        private static bool TryPlaceNeighbor(
            RoomGraph graph,
            Dictionary<Vector2Int, Guid> occupied,
            System.Random rng,
            RoomNode from,
            IReadOnlyList<RoomDefinition> candidateDefinitions,
            out RoomNode placed)
        {
            foreach (Direction direction in Shuffled(rng, DirectionExtensions.All))
            {
                Vector2Int targetPosition = from.GridPosition + direction.ToGridOffset();
                if (occupied.ContainsKey(targetPosition))
                {
                    continue;
                }

                var compatible = candidateDefinitions
                    .Where(d => from.Definition.AllowsNeighbor(d.Category) && d.AllowsNeighbor(from.Definition.Category))
                    .ToList();

                if (compatible.Count == 0)
                {
                    continue;
                }

                RoomDefinition chosen = compatible[rng.Next(compatible.Count)];
                placed = PlaceNode(graph, occupied, rng, chosen, targetPosition);
                graph.AddConnection(new RoomConnection(from.Id, placed.Id, direction));
                return true;
            }

            placed = null;
            return false;
        }

        private static bool TryPlaceExitNearAny(
            RoomGraph graph,
            Dictionary<Vector2Int, Guid> occupied,
            System.Random rng,
            List<RoomNode> candidates,
            RoomDefinition exitDefinition,
            out RoomNode exitNode)
        {
            foreach (RoomNode candidate in Enumerable.Reverse(candidates))
            {
                foreach (Direction direction in Shuffled(rng, DirectionExtensions.All))
                {
                    Vector2Int targetPosition = candidate.GridPosition + direction.ToGridOffset();
                    if (occupied.ContainsKey(targetPosition))
                    {
                        continue;
                    }

                    if (!candidate.Definition.AllowsNeighbor(exitDefinition.Category) || !exitDefinition.AllowsNeighbor(candidate.Definition.Category))
                    {
                        continue;
                    }

                    exitNode = PlaceNode(graph, occupied, rng, exitDefinition, targetPosition);
                    graph.AddConnection(new RoomConnection(candidate.Id, exitNode.Id, direction));
                    return true;
                }
            }

            exitNode = null;
            return false;
        }

        private static void AddLoopConnections(RoomGraph graph, Dictionary<Vector2Int, Guid> occupied)
        {
            foreach (RoomNode node in graph.Nodes.ToList())
            {
                foreach (Direction direction in DirectionExtensions.All)
                {
                    Vector2Int neighborPosition = node.GridPosition + direction.ToGridOffset();
                    if (!occupied.TryGetValue(neighborPosition, out Guid neighborId) || neighborId == node.Id)
                    {
                        continue;
                    }

                    if (graph.HasConnection(node.Id, neighborId))
                    {
                        continue;
                    }

                    RoomNode neighbor = graph.GetNode(neighborId);
                    if (node.Definition.AllowsNeighbor(neighbor.Definition.Category) && neighbor.Definition.AllowsNeighbor(node.Definition.Category))
                    {
                        graph.AddConnection(new RoomConnection(node.Id, neighborId, direction));
                    }
                }
            }
        }

        private static IEnumerable<Direction> Shuffled(System.Random rng, Direction[] directions)
        {
            var copy = (Direction[])directions.Clone();
            for (int i = copy.Length - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                (copy[i], copy[swapIndex]) = (copy[swapIndex], copy[i]);
            }

            return copy;
        }
    }
}
