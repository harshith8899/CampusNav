using UnityEngine;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Positions and orients a 3D arrow model in front of the AR camera,
    /// pointing toward the next waypoint on the current indoor path. Requires
    /// AR Foundation to already be tracking (camera passthrough + pose) —
    /// this script only handles the "which way to point" logic, not camera
    /// tracking itself.
    ///
    /// MANUAL EDITOR STEPS REQUIRED:
    /// - Assign _arCamera to the AR Session Origin's Camera.
    /// - Assign _arrowModel to a 3D arrow prefab instance in the scene (a
    ///   simple cone/arrow mesh is enough).
    /// - This script assumes AR Foundation + an XR plugin are already
    ///   installed and an AR Session / AR Session Origin exist in the scene.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class ArAnchoredArrow : MonoBehaviour
    {
        [SerializeField] private Transform _arCamera;
        [SerializeField] private Transform _arrowModel;
        [SerializeField] private WaypointLocalizer _localizer;

        [Header("Placement")]
        [SerializeField] private float _distanceInFrontOfCamera = 1.5f;
        [SerializeField] private float _heightOffset = -0.3f; // slightly below eye level, like looking at the floor ahead
        [SerializeField] private float _positionSmoothSpeed = 8f;
        [SerializeField] private float _rotationSmoothSpeed = 8f;

        private Vector3 _nextWaypointCampusPos;
        private bool _hasTarget;

        /// <summary>Call this whenever the current path's next waypoint changes.</summary>
        public void SetTargetCampusPosition(Vector3 campusLocalPos)
        {
            _nextWaypointCampusPos = campusLocalPos;
            _hasTarget = true;
        }

        public void ClearTarget()
        {
            _hasTarget = false;
            if (_arrowModel != null) _arrowModel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_hasTarget || _localizer == null || !_localizer.HasLocalized || _arCamera == null || _arrowModel == null)
            {
                return;
            }

            if (!_arrowModel.gameObject.activeSelf) _arrowModel.gameObject.SetActive(true);

            if (!_localizer.TryArWorldToCampusLocal(_arCamera.position, out Vector3 currentCampusPos))
            {
                return;
            }

            // Direction to the next waypoint, computed in campus space (flat,
            // ignoring vertical difference so the arrow doesn't tilt oddly
            // when the next node is on a slightly different floor height).
            Vector3 campusDir = _nextWaypointCampusPos - currentCampusPos;
            campusDir.y = 0f;

            if (campusDir.sqrMagnitude < 0.0001f) return; // basically arrived

            campusDir.Normalize();

            // Convert that direction back into AR world space so it can be
            // used to orient the arrow relative to the camera's tracked pose.
            Vector3 arWorldDir = _localizer.CampusDirectionToArWorld(campusDir);
            arWorldDir.y = 0f;
            arWorldDir.Normalize();

            // Place the arrow a fixed distance in front of the camera, along
            // the camera's forward-flattened direction — it always "floats"
            // just ahead of the user, but points toward the real target.
            Vector3 cameraForwardFlat = _arCamera.forward;
            cameraForwardFlat.y = 0f;
            if (cameraForwardFlat.sqrMagnitude < 0.0001f) cameraForwardFlat = Vector3.forward;
            cameraForwardFlat.Normalize();

            Vector3 desiredPosition = _arCamera.position
                                       + cameraForwardFlat * _distanceInFrontOfCamera
                                       + Vector3.up * _heightOffset;

            Quaternion desiredRotation = Quaternion.LookRotation(arWorldDir, Vector3.up);

            _arrowModel.position = Vector3.Lerp(_arrowModel.position, desiredPosition, Time.deltaTime * _positionSmoothSpeed);
            _arrowModel.rotation = Quaternion.Slerp(_arrowModel.rotation, desiredRotation, Time.deltaTime * _rotationSmoothSpeed);
        }
    }
}
