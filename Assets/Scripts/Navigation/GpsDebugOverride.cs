#if UNITY_EDITOR
using UnityEngine;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Editor-only Play Mode helper that fakes a GPS position + compass
    /// heading on a GpsCompassNavigator, since Input.location/Input.compass
    /// don't produce real readings when running in the Editor. Drive it with
    /// WASD (position) + Q/E (heading), or drag the Inspector fields
    /// directly while paused.
    ///
    /// This entire file is wrapped in UNITY_EDITOR, not a runtime flag, so
    /// the class is absent from the compiled assembly in every non-Editor
    /// build — there is no code path by which it could run on a device.
    /// </summary>
    [AddComponentMenu("CampusNav/Debug/GPS Debug Override (Editor Only)")]
    public class GpsDebugOverride : MonoBehaviour
    {
        [SerializeField] private GpsCompassNavigator _navigator;

        [Header("Fake reading (drag while in Play Mode)")]
        [SerializeField] private Vector2 _localPositionMeters;
        [SerializeField] [Range(0f, 360f)] private float _headingDegrees;
        [SerializeField] private float _simulatedAccuracyMeters = 5f;

        [Header("Keyboard control")]
        [SerializeField] private bool _enableKeyboardControl = true;
        [SerializeField] private float _moveSpeedMetersPerSecond = 5f;
        [SerializeField] private float _turnSpeedDegreesPerSecond = 90f;

        private void Update()
        {
            if (_navigator == null) return;

            if (_enableKeyboardControl)
            {
                var move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                _localPositionMeters += move * (_moveSpeedMetersPerSecond * Time.deltaTime);

                if (Input.GetKey(KeyCode.Q)) _headingDegrees -= _turnSpeedDegreesPerSecond * Time.deltaTime;
                if (Input.GetKey(KeyCode.E)) _headingDegrees += _turnSpeedDegreesPerSecond * Time.deltaTime;
                _headingDegrees = Mathf.Repeat(_headingDegrees, 360f);
            }

            _navigator.DebugApplyOverride(_localPositionMeters, _headingDegrees, _simulatedAccuracyMeters);
        }

        private void OnDisable()
        {
            if (_navigator != null) _navigator.DebugClearOverride();
        }
    }
}
#endif
