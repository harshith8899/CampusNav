using UnityEngine;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Simple HUD-style directional arrow for outdoor navigation: rotates a
    /// UI element to point toward the next waypoint, relative to the
    /// player's current compass heading. This is deliberately NOT an
    /// AR-anchored 3D arrow — outdoors over long distances, a simple
    /// "turn this way" HUD arrow reads more clearly than an AR overlay and
    /// doesn't fight with camera drift.
    ///
    /// MANUAL EDITOR STEP REQUIRED: assign _arrowRectTransform to a UI Image
    /// (the arrow graphic) placed in a Canvas.
    /// </summary>
    public class OutdoorArrowUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrowRectTransform;
        [SerializeField] private GpsCompassNavigator _navigator;

        // Smooth the arrow rotation so it doesn't jitter with noisy compass
        // readings — raw heading data is jumpy, especially near metal/steel
        // structures common on a campus.
        [SerializeField] private float _rotationSmoothSpeed = 6f;

        private Vector2 _targetLocalPosition;
        private bool _hasTarget;
        private float _currentDisplayedAngle;

        /// <summary>Call this whenever the current path's next waypoint changes.</summary>
        public void SetTarget(Vector2 targetLocalXZ)
        {
            _targetLocalPosition = targetLocalXZ;
            _hasTarget = true;
        }

        public void ClearTarget()
        {
            _hasTarget = false;
        }

        private void Update()
        {
            if (!_hasTarget || _navigator == null || !_navigator.IsReady || _arrowRectTransform == null) return;

            Vector2 toTarget = _targetLocalPosition - _navigator.CurrentLocalPosition;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            // Bearing to target, 0 = north, clockwise (matches compass.trueHeading).
            float targetBearing = Mathf.Atan2(toTarget.x, toTarget.y) * Mathf.Rad2Deg;
            if (targetBearing < 0f) targetBearing += 360f;

            // Arrow should point in the direction relative to where the
            // player is currently facing.
            float relativeBearing = targetBearing - _navigator.CurrentHeadingDegrees;

            _currentDisplayedAngle = Mathf.LerpAngle(_currentDisplayedAngle, relativeBearing, Time.deltaTime * _rotationSmoothSpeed);
            _arrowRectTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentDisplayedAngle);
        }

        /// <summary>Straight-line distance in meters to the current target, for a "120m" label.</summary>
        public float DistanceToTargetMeters()
        {
            if (!_hasTarget || _navigator == null) return 0f;
            return Vector2.Distance(_navigator.CurrentLocalPosition, _targetLocalPosition);
        }
    }
}
