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

        public bool IsReady { get; private set; }
        public Vector2 CurrentLocalPosition { get; private set; } // (x, z) meters
        public float CurrentHeadingDegrees { get; private set; }  // 0 = north, clockwise
        public double CurrentLat { get; private set; }
        public double CurrentLon { get; private set; }

        /// <summary>Raised every time a new GPS fix or compass heading arrives.</summary>
        public event Action OnLocationUpdated;

        public void Initialize(double originLat, double originLon)
        {
            _originLat = originLat;
            _originLon = originLon;
        }

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
                yield break;
            }

            IsReady = true;
            StartCoroutine(PollLocationRoutine());
        }

        private IEnumerator PollLocationRoutine()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    var data = Input.location.lastData;
                    CurrentLat = data.latitude;
                    CurrentLon = data.longitude;
                    CurrentLocalPosition = GeoConverter.LatLonToLocal(CurrentLat, CurrentLon, _originLat, _originLon);
                    CurrentHeadingDegrees = Input.compass.trueHeading;

                    OnLocationUpdated?.Invoke();
                }
                yield return wait;
            }
        }
    }
}
