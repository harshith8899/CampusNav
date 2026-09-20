using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CampusNav.Data;
using CampusNav.Search;
using CampusNav.Pathfinding;

namespace CampusNav.Navigation
{
    public enum NavMode { Outdoor, Indoor }

    /// <summary>
    /// The top-level orchestrator: loads map data once, builds the search
    /// index and graph, computes routes, and switches between outdoor
    /// (GPS/compass) and indoor (QR/AR) navigation as the player moves.
    ///
    /// This is the one script a scene actually needs to wire up in the
    /// Inspector to get end-to-end navigation working; everything else
    /// (search, pathfinding, GPS, AR arrow) is a supporting piece this
    /// controller drives.
    /// </summary>
    public class NavigationModeController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string _campusJsonFileName = "sample_campus.json";

        [Header("Outdoor")]
        [SerializeField] private GpsCompassNavigator _gpsNavigator;
        [SerializeField] private OutdoorArrowUI _outdoorArrow;

        [Header("Indoor")]
        [SerializeField] private WaypointLocalizer _localizer;
        [SerializeField] private ArAnchoredArrow _arArrow;

        [Header("Tuning")]
        [SerializeField] private float _waypointArrivalRadiusMeters = 2.5f;

        public NavMode CurrentMode { get; private set; } = NavMode.Outdoor;
        public RoomSearchIndex SearchIndex { get; private set; }

        private CampusData _campus;
        private WaypointIndex _waypointIndex;
        private MapGraph _graph;
        private AStarPathfinder _pathfinder;

        private List<string> _currentPath;
        private int _currentPathIndex;
        private string _goalWaypointId;

        private void Awake()
        {
            var provider = new LocalJsonMapDataProvider(_campusJsonFileName);
            _campus = provider.GetCampusData();

            _waypointIndex = new WaypointIndex(_campus);
            SearchIndex = new RoomSearchIndex(_campus, _waypointIndex);
            _graph = new MapGraph(_campus, _waypointIndex);
            _pathfinder = new AStarPathfinder(_graph);

            if (_gpsNavigator != null) _gpsNavigator.Initialize(_campus.originLat, _campus.originLon);
            if (_localizer != null) _localizer.Initialize(_campus, _waypointIndex);
        }

        private void OnEnable()
        {
            if (_gpsNavigator != null) _gpsNavigator.OnLocationUpdated += HandleOutdoorPositionUpdated;
            if (_localizer != null) _localizer.OnLocalized += HandleIndoorLocalized;
        }

        private void OnDisable()
        {
            if (_gpsNavigator != null) _gpsNavigator.OnLocationUpdated -= HandleOutdoorPositionUpdated;
            if (_localizer != null) _localizer.OnLocalized -= HandleIndoorLocalized;
        }

        /// <summary>
        /// Call this when the user picks a search result. Computes a full
        /// route from the player's current known position to the target
        /// waypoint and starts guiding them, in whichever mode currently
        /// applies (outdoor GPS or indoor AR).
        /// </summary>
        public bool NavigateTo(string destinationWaypointId)
        {
            string startId = CurrentMode == NavMode.Indoor && _localizer.HasLocalized
                ? _localizer.CurrentWaypointId
                : FindNearestOutdoorNodeId(_gpsNavigator != null ? _gpsNavigator.CurrentLocalPosition : Vector2.zero);

            if (startId == null)
            {
                Debug.LogWarning("CampusNav: could not determine a starting node for navigation.");
                return false;
            }

            var path = _pathfinder.FindPath(startId, destinationWaypointId);
            if (path == null || path.Count == 0) return false;

            _currentPath = path;
            _currentPathIndex = 0;
            _goalWaypointId = destinationWaypointId;

            // If we're already "at" the first node, advance immediately so
            // the arrow points at the next actual step, not our own feet.
            AdvanceIfArrived(force: true);
            PushCurrentTargetToActiveArrow();
            return true;
        }

        public void CancelNavigation()
        {
            _currentPath = null;
            _currentPathIndex = 0;
            _goalWaypointId = null;
            _outdoorArrow?.ClearTarget();
            _arArrow?.ClearTarget();
        }

        private void HandleOutdoorPositionUpdated()
        {
            if (CurrentMode != NavMode.Outdoor) return;
            AdvanceIfArrived(force: false);
            PushCurrentTargetToActiveArrow();
        }

        private void HandleIndoorLocalized(string waypointId)
        {
            var resolved = _waypointIndex.GetById(waypointId);
            bool enteringBuilding = resolved != null && !resolved.IsOutdoor;

            CurrentMode = enteringBuilding ? NavMode.Indoor : NavMode.Outdoor;

            // Re-anchoring mid-route: if we're already navigating somewhere,
            // recompute the remaining path from this freshly-confirmed node
            // rather than trusting dead-reckoned progress along the old path.
            if (_goalWaypointId != null)
            {
                var path = _pathfinder.FindPath(waypointId, _goalWaypointId);
                if (path != null)
                {
                    _currentPath = path;
                    _currentPathIndex = 0;
                    AdvanceIfArrived(force: true);
                }
            }

            PushCurrentTargetToActiveArrow();
        }

        private void AdvanceIfArrived(bool force)
        {
            if (_currentPath == null || _currentPathIndex >= _currentPath.Count - 1) return;

            if (force)
            {
                _currentPathIndex++;
                return;
            }

            string nextId = _currentPath[_currentPathIndex + 1];
            _graph.TryGetPosition(nextId, out Vector3 nextPos);

            Vector2 currentLocal = CurrentMode == NavMode.Outdoor && _gpsNavigator != null
                ? _gpsNavigator.CurrentLocalPosition
                : new Vector2(nextPos.x, nextPos.z); // indoor progress advances via QR re-scans, not distance

            float dist = Vector2.Distance(currentLocal, new Vector2(nextPos.x, nextPos.z));
            if (dist <= _waypointArrivalRadiusMeters)
            {
                _currentPathIndex++;
                AdvanceIfArrived(force: false); // chain through any other waypoints already within radius
            }
        }

        private void PushCurrentTargetToActiveArrow()
        {
            if (_currentPath == null || _currentPathIndex >= _currentPath.Count - 1)
            {
                _outdoorArrow?.ClearTarget();
                _arArrow?.ClearTarget();
                return;
            }

            string nextId = _currentPath[_currentPathIndex + 1];
            if (!_graph.TryGetPosition(nextId, out Vector3 nextPos)) return;

            if (CurrentMode == NavMode.Outdoor)
            {
                _outdoorArrow?.SetTarget(new Vector2(nextPos.x, nextPos.z));
                _arArrow?.ClearTarget();
            }
            else
            {
                _arArrow?.SetTargetCampusPosition(nextPos);
                _outdoorArrow?.ClearTarget();
            }
        }

        private string FindNearestOutdoorNodeId(Vector2 localXZ)
        {
            string best = null;
            float bestDist = float.MaxValue;

            foreach (var kvp in _waypointIndex.All)
            {
                if (!kvp.Value.IsOutdoor) continue;
                var wp = kvp.Value.Waypoint;
                float d = Vector2.Distance(localXZ, new Vector2(wp.x, wp.z));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = wp.id;
                }
            }
            return best;
        }
    }
}
