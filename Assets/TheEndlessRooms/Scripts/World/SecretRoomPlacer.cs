using System.Linq;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// The secret room (and everything under it — door, props, field notes) is built
    /// once at edit time as a rigid unit under one root transform, at a placeholder
    /// position a fixed offset from the player's spawn. That offset lands on the same
    /// integer grid the procedural graph uses (see <see cref="ProceduralLevelBuilder"/>'s
    /// GridPosition * cellSize math), so nothing stopped the graph from placing a real
    /// room at that exact spot — which happened deterministically for Milestone 8's
    /// seed, wedging The Attendant's patrol point inside doubled-up geometry it could
    /// never path out of. Since the graph is only known once <see cref="ProceduralLevelBuilder.BuildLevel"/>
    /// actually runs, this waits for that, then slides the whole root sideways to the
    /// nearest candidate cell the graph didn't use — every child moves with it because
    /// they're parented, so none of the original placement math needs to change.
    /// </summary>
    public sealed class SecretRoomPlacer : MonoBehaviour
    {
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;
        [SerializeField] private Transform _secretRoomRoot;

        private void OnEnable()
        {
            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt += OnLevelBuilt;
            }
        }

        private void OnDisable()
        {
            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt -= OnLevelBuilt;
            }
        }

        private void OnLevelBuilt(RoomGraph graph)
        {
            if (_secretRoomRoot == null)
            {
                return;
            }

            int southSteps = FindFreeSouthSteps(graph);
            Vector3 originalOffset = new(0f, 0f, -_levelBuilder.CellSize);
            Vector3 chosenWorldOffset = new(0f, 0f, -southSteps * _levelBuilder.CellSize);

            _secretRoomRoot.position += chosenWorldOffset - originalOffset;
        }

        /// <summary>
        /// Pure graph math — no Unity scene dependency beyond <see cref="RoomGraph"/>
        /// itself — so it's EditMode-testable without a live level. Walks straight
        /// south from the entry, one cell at a time, until landing on a cell the graph
        /// didn't use. (The original fixed "1 cell south" design never guaranteed the
        /// door opens into open space rather than some other room's wall either — this
        /// preserves that same behavior, just on whichever cell actually turns out to
        /// be free instead of assuming cell 1 always is.)
        /// </summary>
        public static int FindFreeSouthSteps(RoomGraph graph)
        {
            RoomNode entryNode = graph.GetNode(graph.EntryNodeId);
            var occupied = graph.Nodes.Select(node => node.GridPosition).ToHashSet();

            int southSteps = 1;
            int maxSteps = graph.Nodes.Count + 4;
            while (southSteps < maxSteps && occupied.Contains(entryNode.GridPosition + new Vector2Int(0, -southSteps)))
            {
                southSteps++;
            }

            return southSteps;
        }
    }
}
