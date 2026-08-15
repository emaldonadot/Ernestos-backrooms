using System;
using EndlessRooms.Procedural;
using EndlessRooms.World;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    /// <summary>Covers Milestone 9's UseExternalGraph seam — see its doc comment on ProceduralLevelBuilder.</summary>
    public class ProceduralLevelBuilderExternalGraphTests
    {
        private static RoomGraph MakeTinyGraph()
        {
            var graph = new RoomGraph();
            Guid a = Guid.NewGuid();
            Guid b = Guid.NewGuid();
            graph.AddNode(new RoomNode(a, null, new Vector2Int(0, 0)));
            graph.AddNode(new RoomNode(b, null, new Vector2Int(1, 0)));
            graph.AddConnection(new RoomConnection(a, b, Direction.East));
            graph.SetEntry(a);
            graph.SetExit(b);
            return graph;
        }

        [Test]
        public void UseExternalGraph_SetsLastGraph()
        {
            var go = new GameObject("TestLevelBuilder");
            var builder = go.AddComponent<ProceduralLevelBuilder>();
            RoomGraph graph = MakeTinyGraph();

            builder.UseExternalGraph(graph);

            Assert.AreSame(graph, builder.LastGraph);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void UseExternalGraph_RaisesLevelBuiltWithTheSameGraph()
        {
            var go = new GameObject("TestLevelBuilder");
            var builder = go.AddComponent<ProceduralLevelBuilder>();
            RoomGraph graph = MakeTinyGraph();

            RoomGraph raised = null;
            builder.LevelBuilt += g => raised = g;

            builder.UseExternalGraph(graph);

            Assert.AreSame(graph, raised);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void UseExternalGraph_DoesNotInstantiateAnyChildren()
        {
            var go = new GameObject("TestLevelBuilder");
            var builder = go.AddComponent<ProceduralLevelBuilder>();

            builder.UseExternalGraph(MakeTinyGraph());

            Assert.AreEqual(0, go.transform.childCount);

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
