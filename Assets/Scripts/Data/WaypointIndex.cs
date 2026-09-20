using System.Collections.Generic;

namespace CampusNav.Data
{
    /// <summary>
    /// A waypoint plus the building/floor context it lives in (null for
    /// outdoor waypoints). Both RoomSearchIndex and the pathfinder build on
    /// top of this flattened view instead of re-walking the nested
    /// CampusData structure themselves.
    /// </summary>
    public class ResolvedWaypoint
    {
        public WaypointData Waypoint;
        public string BuildingId;   // null/empty if this is an outdoor waypoint
        public string FloorId;      // null/empty if this is an outdoor waypoint
        public float ElevationMeters; // floor's vertical offset, 0 for outdoor

        public bool IsOutdoor => string.IsNullOrEmpty(FloorId);
    }

    /// <summary>
    /// Flattens the nested CampusData (buildings -> floors -> waypoints,
    /// plus top-level outdoor waypoints) into a single global lookup table.
    /// Every waypoint ID in a well-formed campus file is unique across this
    /// whole index — that uniqueness is what lets pathfinding and search
    /// treat "indoor" and "outdoor" as one continuous graph.
    /// </summary>
    public class WaypointIndex
    {
        private readonly Dictionary<string, ResolvedWaypoint> _byId = new Dictionary<string, ResolvedWaypoint>();

        public IReadOnlyDictionary<string, ResolvedWaypoint> All => _byId;

        public WaypointIndex(CampusData campus)
        {
            Build(campus);
        }

        private void Build(CampusData campus)
        {
            if (campus == null) return;

            foreach (var wp in campus.outdoorWaypoints)
            {
                AddOrWarn(wp, buildingId: null, floorId: null, elevation: 0f);
            }

            foreach (var building in campus.buildings)
            {
                foreach (var floor in building.floors)
                {
                    foreach (var wp in floor.waypoints)
                    {
                        AddOrWarn(wp, building.id, floor.id, floor.elevationMeters);
                    }
                }
            }
        }

        private void AddOrWarn(WaypointData wp, string buildingId, string floorId, float elevation)
        {
            if (wp == null || string.IsNullOrEmpty(wp.id))
            {
                UnityEngine.Debug.LogWarning("CampusNav: skipping waypoint with missing id.");
                return;
            }

            if (_byId.ContainsKey(wp.id))
            {
                UnityEngine.Debug.LogWarning($"CampusNav: duplicate waypoint id '{wp.id}' — check your campus JSON. Keeping the first one.");
                return;
            }

            _byId[wp.id] = new ResolvedWaypoint
            {
                Waypoint = wp,
                BuildingId = buildingId,
                FloorId = floorId,
                ElevationMeters = elevation
            };
        }

        public ResolvedWaypoint GetById(string id)
        {
            return _byId.TryGetValue(id, out var resolved) ? resolved : null;
        }

        public bool TryGetById(string id, out ResolvedWaypoint resolved)
        {
            return _byId.TryGetValue(id, out resolved);
        }
    }
}
