using System.Collections.Generic;
using UnityEngine;

namespace CampusNav.Pathfinding
{
    /// <summary>
    /// Standard A* over a MapGraph. Because the graph is already flattened
    /// across outdoor space, buildings, and floors, this single search
    /// handles within-floor, cross-floor (elevator/stairs), and
    /// cross-building (outdoor) routing with no special-casing — the
    /// "hierarchy" lives in how the graph was built, not in the search
    /// itself.
    /// </summary>
    public class AStarPathfinder
    {
        private readonly MapGraph _graph;

        public AStarPathfinder(MapGraph graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// Returns the ordered list of waypoint IDs from startId to goalId
        /// (inclusive), or null if no path exists or either ID is unknown.
        /// </summary>
        public List<string> FindPath(string startId, string goalId)
        {
            if (!_graph.NodePositions.ContainsKey(startId) || !_graph.NodePositions.ContainsKey(goalId))
            {
                Debug.LogWarning($"CampusNav: pathfinding failed — unknown start '{startId}' or goal '{goalId}'.");
                return null;
            }

            if (startId == goalId) return new List<string> { startId };

            var openSet = new HashSet<string> { startId };
            var cameFrom = new Dictionary<string, string>();

            var gScore = new Dictionary<string, float> { [startId] = 0f };
            var fScore = new Dictionary<string, float> { [startId] = Heuristic(startId, goalId) };

            // Simple linear-scan priority queue. Campus graphs are small
            // (hundreds, not millions, of waypoints) so this is plenty fast;
            // swap for a binary heap if you later have a huge combined map.
            while (openSet.Count > 0)
            {
                string current = LowestFScore(openSet, fScore);

                if (current == goalId)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                foreach (var edge in _graph.Adjacency[current])
                {
                    float tentativeG = gScore[current] + edge.Cost;

                    if (!gScore.TryGetValue(edge.ToId, out float existingG) || tentativeG < existingG)
                    {
                        cameFrom[edge.ToId] = current;
                        gScore[edge.ToId] = tentativeG;
                        fScore[edge.ToId] = tentativeG + Heuristic(edge.ToId, goalId);
                        openSet.Add(edge.ToId);
                    }
                }
            }

            Debug.LogWarning($"CampusNav: no path found from '{startId}' to '{goalId}'.");
            return null;
        }

        private float Heuristic(string aId, string bId)
        {
            Vector3 a = _graph.NodePositions[aId];
            Vector3 b = _graph.NodePositions[bId];
            return Vector3.Distance(a, b);
        }

        private static string LowestFScore(HashSet<string> openSet, Dictionary<string, float> fScore)
        {
            string best = null;
            float bestScore = float.MaxValue;
            foreach (var id in openSet)
            {
                float score = fScore.TryGetValue(id, out float s) ? s : float.MaxValue;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = id;
                }
            }
            return best;
        }

        private static List<string> ReconstructPath(Dictionary<string, string> cameFrom, string current)
        {
            var path = new List<string> { current };
            while (cameFrom.TryGetValue(current, out string prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
