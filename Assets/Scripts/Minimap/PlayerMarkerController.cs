using UnityEngine;
using CampusNav.Navigation;

namespace CampusNav.Minimap
{
    /// <summary>
    /// Drives the "you are here" marker on the mini-map. Reads the player's
    /// current position from whichever source is authoritative for the
    /// active navigation mode (GPS outdoors, QR/AR-derived position indoors)
    /// and smooths it — raw GPS and AR position updates are jittery/noisy,
    /// so applying them directly to the marker would look twitchy rather
    /// than like a clean walking animation.
    /// </summary>
    public class PlayerMarkerController : MonoBehaviour
    {
        [SerializeField] private NavigationModeController _modeController;
        [SerializeField] private GpsCompassNavigator _gpsNavigator;
        [SerializeField] private WaypointLocalizer _localizer;
        [SerializeField] private Transform _arCameraTransform; // for indoor position sampling

        [Header("Smoothing")]
        [SerializeField] private float _positionSmoothTime = 0.35f;
        [SerializeField] private float _rotationSmoothSpeed = 8f;

        private Vector3 _smoothedPosition;
        private Vector3 _positionVelocity;
        private float _lastHeadingDegrees;
        private bool _initialized;

        private void Update()
        {
            if (!TryGetTargetWorldPosition(out Vector3 targetPos, out float targetHeading)) return;

            if (!_initialized)
            {
                _smoothedPosition = targetPos;
                _lastHeadingDegrees = targetHeading;
                _initialized = true;
            }

            _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, targetPos, ref _positionVelocity, _positionSmoothTime);
            _lastHeadingDegrees = Mathf.LerpAngle(_lastHeadingDegrees, targetHeading, Time.deltaTime * _rotationSmoothSpeed);

            transform.position = _smoothedPosition;
            transform.rotation = Quaternion.Euler(0f, _lastHeadingDegrees, 0f);
        }

        private bool TryGetTargetWorldPosition(out Vector3 worldPos, out float headingDegrees)
        {
            worldPos = default;
            headingDegrees = 0f;

            if (_modeController == null) return false;

            if (_modeController.CurrentMode == NavMode.Outdoor)
            {
                if (_gpsNavigator == null || !_gpsNavigator.IsReady) return false;
                Vector2 local = _gpsNavigator.CurrentLocalPosition;
                worldPos = new Vector3(local.x, 0f, local.y);
                headingDegrees = _gpsNavigator.CurrentHeadingDegrees;
                return true;
            }
            else
            {
                if (_localizer == null || !_localizer.HasLocalized || _arCameraTransform == null) return false;
                if (!_localizer.TryArWorldToCampusLocal(_arCameraTransform.position, out Vector3 campusPos)) return false;

                worldPos = campusPos;

                // Convert the AR camera's forward direction into campus
                // space so the marker's heading matches the building's
                // coordinate frame, not the AR session's arbitrary one.
                Vector3 forwardFlat = _arCameraTransform.forward;
                forwardFlat.y = 0f;
                if (forwardFlat.sqrMagnitude > 0.0001f)
                {
                    Vector3 campusForward = _localizer.ArWorldDirectionToCampus(forwardFlat.normalized);
                    headingDegrees = Mathf.Atan2(campusForward.x, campusForward.z) * Mathf.Rad2Deg;
                }
                return true;
            }
        }
    }
}
