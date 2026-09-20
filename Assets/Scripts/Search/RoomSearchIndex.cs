using System.Collections.Generic;
using System.Linq;
using CampusNav.Data;

namespace CampusNav.Search
{
    public class SearchResult
    {
        public string WaypointId;
        public string DisplayName;   // what to show the user, e.g. "Room 101 — Block A"
        public string BuildingId;
        public string FloorId;
    }

    /// <summary>
    /// Builds a simple, fast, in-memory search over every named destination
    /// on campus: rooms (waypoints with a roomName) and buildings themselves
    /// (searchable by name, resolving to their entrance waypoint).
    ///
    /// This is intentionally simple (substring match, no fuzzy/typo
    /// tolerance) so it has zero dependencies and is easy to swap for a
    /// fuzzy-search library later without touching any calling code — every
    /// caller only sees Search(string) -> List&lt;SearchResult&gt;.
    /// </summary>
    public class RoomSearchIndex
    {
        private readonly List<SearchResult> _entries = new List<SearchResult>();

        public RoomSearchIndex(CampusData campus, WaypointIndex waypointIndex)
        {
            Build(campus, waypointIndex);
        }

        private void Build(CampusData campus, WaypointIndex waypointIndex)
        {
            if (campus == null) return;

            // Index every named room waypoint (indoor or outdoor).
            foreach (var resolved in waypointIndex.All.Values)
            {
                var wp = resolved.Waypoint;
                if (string.IsNullOrWhiteSpace(wp.roomName)) continue;

                string display = resolved.BuildingId != null
                    ? $"{wp.roomName} — {BuildingName(campus, resolved.BuildingId)}"
                    : wp.roomName;

                _entries.Add(new SearchResult
                {
                    WaypointId = wp.id,
                    DisplayName = display,
                    BuildingId = resolved.BuildingId,
                    FloorId = resolved.FloorId
                });
            }

            // Also index each building itself, so "Block A" resolves
            // straight to its entrance waypoint even with no specific room.
            foreach (var building in campus.buildings)
            {
                if (string.IsNullOrEmpty(building.entranceWaypointId)) continue;

                _entries.Add(new SearchResult
                {
                    WaypointId = building.entranceWaypointId,
                    DisplayName = building.name,
                    BuildingId = building.id,
                    FloorId = null
                });
            }
        }

        private static string BuildingName(CampusData campus, string buildingId)
        {
            var b = campus.buildings.FirstOrDefault(x => x.id == buildingId);
            return b != null ? b.name : buildingId;
        }

        /// <summary>
        /// Case-insensitive substring search over display names. Empty query
        /// returns everything (useful for showing a full destination list).
        /// </summary>
        public List<SearchResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<SearchResult>(_entries);

            string q = query.Trim().ToLowerInvariant();
            return _entries
                .Where(e => e.DisplayName.ToLowerInvariant().Contains(q))
                .OrderBy(e => e.DisplayName)
                .ToList();
        }

        public IReadOnlyList<SearchResult> AllEntries => _entries;
    }
}
