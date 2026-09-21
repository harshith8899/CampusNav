using System;
using System.Collections;
using UnityEngine;
using CampusNav.Utils;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Wraps Unity's location + compass services and converts live GPS/heading
    /// readings into the project's shared local XZ meter space (via
    /// GeoConverter), so outdoor navigation code never has to touch raw
    /// lat/lon itself.
    ///
    /// MANUAL EDITOR STEPS REQUIRED (see project README):
    /// - Player Settings > Android: enable Fine/Coarse Location permission.
    /// - Player Settings > iOS: set "Location Usage Description" (Info.plist
    ///   NSLocationWhenInUseUsageDescription), otherwise iOS silently denies.
    /// </summary>
    public class GpsCompassNavigator : MonoBehaviour
    {
        [Header("Origin (set from CampusData at startup)")]
        [SerializeField] private double _originLat;
        [SerializeField] private double _originLon;

        [Header("Location service tuning")]
        [SerializeField] private float _desiredAccuracyMeters = 5f;
        [SerializeField] private float _updateDistanceMeters = 1f;
        [SerializeField] private float _startTimeoutSeconds = 20f;

        private bool _isReady;

        /// <summary>
        /// True once a usable fix is available: either a real GPS fix, or —
        /// in the Editor only — a debug override. In a non-Editor build this
        /// reflects only the real location service; the debug bypass below
        /// is compiled out entirely and doesn't exist there.
        /// </summary>
        public bool IsReady
        {
            get
            {
#if UNITY_EDITOR
                return _isReady || _debugOverrideActive;
#else
                return _isReady;
#endif
            }
        }

        public Vector2 CurrentLocalPosition { get; private set; } // (x, z) meters
        public float CurrentHeadingDegrees { get; private set; }  // 0 = north, clockwise
        public double CurrentLat { get; private set; }
        public double CurrentLon { get; private set; }

        /// <summary>Horizontal accuracy of the last fix, in meters (lower is better).</summary>
        public float CurrentAccuracyMeters { get; private set; }

        /// <summary>
        /// True once the location service has definitively failed to produce
        /// a usable fix (disabled by the user/OS, or start timed out). Used
        /// to drive the "weak/no GPS signal" banner.
        /// </summary>
        public bool ServiceFailedToStart { get; private set; }

        /// <summary>Raised every time a new GPS fix or compass heading arrives.</summary>
        public event Action OnLocationUpdated;

        public void Initialize(double originLat, double originLon)
        {
            _originLat = originLat;
            _originLon = originLon;
        }

#if UNITY_EDITOR
        // Editor-only escape hatch: Input.location/Input.compass don't
        // produce real readings in the Editor, so Play Mode testing and
        // manual debugging need a way to fake a fix. Driven by
        // GpsDebugOverride (also UNITY_EDITOR-gated) or directly from a
        // test. Compiled out of every non-Editor build entirely — this
        // method, and the IsReady bypass above, do not exist in a device
        // build, regardless of any runtime flag, so neither can ever run
        // there.
        private bool _debugOverrideActive;

        public void DebugApplyOverride(Vector2 localXZ, float headingDegrees, float accuracyMeters)
        {
            // Setting this alone already makes IsReady true (see the getter
            // above) — deliberately independent of whether the real location
            // service ever started, so a desktop Editor run with no GPS
            // hardware still drives the whole outdoor nav pipeline off the
            // faked reading.
            _debugOverrideActive = true;
            CurrentLocalPosition = localXZ;
            CurrentHeadingDegrees = headingDegrees;
            CurrentAccuracyMeters = accuracyMeters;
            OnLocationUpdated?.Invoke();
        }

        public void DebugClearOverride()
        {
            _debugOverrideActive = false;
        }
#endif

        private void OnEnable()
        {
            StartCoroutine(StartServicesRoutine());
        }

        private void OnDisable()
        {
            Input.location.Stop();
            Input.compass.enabled = false;
        }

        private IEnumerator StartServicesRoutine()
        {
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("CampusNav: location services are disabled by the user/OS. Outdoor GPS navigation will not work until enabled in device settings.");
                ServiceFailedToStart = true;
                yield break;
            }

            Input.location.Start(_desiredAccuracyMeters, _updateDistanceMeters);
            Input.compass.enabled = true;

            float elapsed = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && elapsed < _startTimeoutSeconds)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarning($"CampusNav: location service failed to start (status: {Input.location.status}).");
                ServiceFailedToStart = true;
                yield break;
            }

            _isReady = true;
            StartCoroutine(PollLocationRoutine());
        }

        private IEnumerator PollLocationRoutine()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
#if UNITY_EDITOR
                // A debug override is driving readings instead — don't let a
                // real (or Editor-simulated) fix clobber it.
                if (_debugOverrideActive)
                {
                    yield return wait;
                    continue;
                }
#endif
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    var data = Input.location.lastData;
                    CurrentLat = data.latitude;
                    CurrentLon = data.longitude;
                    CurrentAccuracyMeters = data.horizontalAccuracy;
                    CurrentLocalPosition = GeoConverter.LatLonToLocal(CurrentLat, CurrentLon, _originLat, _originLon);
                    CurrentHeadingDegrees = Input.compass.trueHeading;

                    OnLocationUpdated?.Invoke();
                }
                yield return wait;
            }
        }
    }
}
