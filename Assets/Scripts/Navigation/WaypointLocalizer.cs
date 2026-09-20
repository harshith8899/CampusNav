using System;
using System.Collections.Generic;
using UnityEngine;
using CampusNav.Data;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Resolves the player's position inside a building from a scanned QR
    /// code, and maintains the mapping between the AR session's own world
    /// space (wherever ARKit/ARCore decided its origin is) and the project's
    /// shared campus local-XZ space.
    ///
    /// AR tracking drifts the longer you walk without re-anchoring, which is
    /// exactly why QR codes exist at multiple points per floor (see README) —
    /// each scan snaps the mapping back to a known-good position.
    /// </summary>
    public class WaypointLocalizer : MonoBehaviour
    {
        [SerializeField] private Transform _arCameraTransform; // assign the AR Camera in the Inspector
        [SerializeField] private IQrScannerBehaviour _qrScanner; // see note below

        private WaypointIndex _waypointIndex;
        private readonly Dictionary<string, string> _qrCodeToWaypointId = new Dictionary<string, string>();

        // Offset mapping: campusPos = RotateY(arWorldPos - _arPosAtAnchor, _rotationOffsetDegrees) + _campusPosAtAnchor
        private Vector3 _arPosAtAnchor;
        private Vector3 _campusPosAtAnchor;
        private float _rotationOffsetDegrees;

        public bool HasLocalized { get; private set; }
        public string CurrentWaypointId { get; private set; }

        /// <summary>Raised after a successful QR scan resolves/re-anchors the player's position.</summary>
        public event Action<string /*waypointId*/> OnLocalized;

        public void Initialize(CampusData campus, WaypointIndex waypointIndex)
        {
            _waypointIndex = waypointIndex;
            _qrCodeToWaypointId.Clear();

            foreach (var kvp in waypointIndex.All)
            {
                var wp = kvp.Value.Waypoint;
                if (wp.isQrAnchor && !string.IsNullOrEmpty(wp.qrCodeId))
                {
                    _qrCodeToWaypointId[wp.qrCodeId] = wp.id;
                }
            }
        }

        private void OnEnable()
        {
            if (_qrScanner != null) _qrScanner.OnQrDecoded += HandleQrDecoded;
        }

        private void OnDisable()
        {
            if (_qrScanner != null) _qrScanner.OnQrDecoded -= HandleQrDecoded;
        }

        private void HandleQrDecoded(string qrPayload)
        {
            if (!_qrCodeToWaypointId.TryGetValue(qrPayload, out string waypointId))
            {
                Debug.LogWarning($"CampusNav: scanned QR '{qrPayload}' does not match any known waypoint.");
                return;
            }

            var resolved = _waypointIndex.GetById(waypointId);
            if (resolved == null) return;

            AnchorTo(resolved);
        }

        private void AnchorTo(ResolvedWaypoint resolved)
        {
            if (_arCameraTransform == null)
            {
                Debug.LogError("CampusNav: WaypointLocalizer has no AR camera assigned — cannot anchor.");
                return;
            }

            var wp = resolved.Waypoint;

            _arPosAtAnchor = _arCameraTransform.position;
            _campusPosAtAnchor = new Vector3(wp.x, resolved.ElevationMeters, wp.z);

            // The heading the player is assumed to be facing right when they
            // scan (set per-QR in the data, e.g. "facing north when you scan
            // this code by the elevator"). Offset = campus heading - AR yaw.
            float arYaw = _arCameraTransform.eulerAngles.y;
            _rotationOffsetDegrees = wp.anchorHeadingDegrees - arYaw;

            CurrentWaypointId = wp.id;
            HasLocalized = true;

            OnLocalized?.Invoke(wp.id);
        }

        /// <summary>
        /// Converts a position in the AR session's world space into the
        /// shared campus local-XZ space, using the offset established at the
        /// last QR scan. Returns false (with an undefined output) if no scan
        /// has happened yet.
        /// </summary>
        public bool TryArWorldToCampusLocal(Vector3 arWorldPos, out Vector3 campusLocalPos)
        {
            if (!HasLocalized)
            {
                campusLocalPos = default;
                return false;
            }

            Vector3 relative = arWorldPos - _arPosAtAnchor;
            Vector3 rotated = Quaternion.Euler(0f, _rotationOffsetDegrees, 0f) * relative;
            campusLocalPos = rotated + _campusPosAtAnchor;
            return true;
        }

        /// <summary>
        /// Converts a direction expressed in campus local space (e.g. "toward
        /// the next waypoint") into a direction in the AR session's world
        /// space, so it can be used to orient an AR-anchored arrow.
        /// </summary>
        public Vector3 CampusDirectionToArWorld(Vector3 campusDirection)
        {
            return Quaternion.Euler(0f, -_rotationOffsetDegrees, 0f) * campusDirection;
        }

        /// <summary>
        /// The inverse of CampusDirectionToArWorld: converts a direction
        /// expressed in the AR session's world space (e.g. "which way the
        /// camera is currently facing") into campus local space. Used to
        /// derive the player's heading for the mini-map while indoors.
        /// </summary>
        public Vector3 ArWorldDirectionToCampus(Vector3 arWorldDirection)
        {
            return Quaternion.Euler(0f, _rotationOffsetDegrees, 0f) * arWorldDirection;
        }
    }

    /// <summary>
    /// MonoBehaviour-friendly wrapper around IQrScanner so it can be assigned
    /// in the Inspector. Implement this on whatever component wraps your
    /// chosen QR plugin (see README for plugin recommendations), e.g.:
    ///   public class ZxingQrScanner : IQrScannerBehaviour { ... }
    /// </summary>
    public abstract class IQrScannerBehaviour : MonoBehaviour, IQrScanner
    {
        public abstract event Action<string> OnQrDecoded;
        public abstract void StartScanning();
        public abstract void StopScanning();
    }
}
