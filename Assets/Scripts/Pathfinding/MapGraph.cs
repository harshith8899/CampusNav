using System.Collections.Generic;
using UnityEngine;
using CampusNav.Data;

namespace CampusNav.Pathfinding
{
    public class GraphEdge
    {
        public string ToId;
        public float Cost;
        public EdgeType Type;
    }

    /// <summary>
    /// A flat, unified graph of every waypoint on campus — indoor, outdoor,
    /// cross-floor and cross-building — built once from CampusData. This is
    /// what A* actually searches over: it doesn't know or care whether a
    /// node is "indoors" or "outdoors", only about node positions and edge
    /// costs. That's what lets one pathfinder route from an outdoor GPS
    /// position all the way to a specific indoor room.
    /// </summary>
    public class MapGraph
    {
        // 3D position per node: (x, elevation, z) in the shared local meter
        // space, elevation coming from the owning floor (0 for outdoor).
        public readonly Dictionary<string, Vector3> NodePositions = new Dictionary<string, Vector3>();
        public readonly Dictionary<string, List<GraphEdge>> Adjacency = new Dictionary<string, List<GraphEdge>>();

        private readonly WaypointIndex _waypointIndex;

        public MapGraph(CampusData campus, WaypointIndex waypointIndex)
        {
            _waypointIndex = waypointIndex;
            Build(campus);
        }

        private void Build(CampusData campus)
        {
            // 1. Register every node's 3D position.
            foreach (var kvp in _waypointIndex.All)
            {
                var resolved = kvp.Value;
                var wp = resolved.Waypoint;
                NodePositions[wp.id] = new Vector3(wp.x, resolved.ElevationMeters, wp.z);
                Adjacency[wp.id] = new List<GraphEdge>();
            }

            // 2. Add every edge list in the file: outdoor, each floor's
            //    indoor edges, and each building's cross-floor edges
            //    (elevators/stairs).
            AddEdges(campus.outdoorEdges);

            foreach (var building in campus.buildings)
            {
                foreach (var floor in building.floors)
                {
                    AddEdges(floor.edges);
                }
                AddEdges(building.crossFloorEdges);
            }
        }

        private void AddEdges(List<EdgeData> edges)
        {
            if (edges == null) return;

            foreach (var edge in edges)
            {
                if (!NodePositions.ContainsKey(edge.fromId) || !NodePositions.ContainsKey(edge.toId))
                {
                    Debug.LogWarning($"CampusNav: edge references unknown waypoint(s): '{edge.fromId}' -> '{edge.toId}'. Skipping.");
                    continue;
                }

                float cost = edge.costOverride > 0f
                    ? edge.costOverride
                    : Vector3.Distance(NodePositions[edge.fromId], NodePositions[edge.toId]);

                Adjacency[edge.fromId].Add(new GraphEdge { ToId = edge.toId, Cost = cost, Type = edge.type });

                if (edge.bidirectional)
                {
                    Adjacency[edge.toId].Add(new GraphEdge { ToId = edge.fromId, Cost = cost, Type = edge.type });
                }
            }
        }

        public bool TryGetPosition(string nodeId, out Vector3 position)
        {
            return NodePositions.TryGetValue(nodeId, out position);
        }
    }
}
