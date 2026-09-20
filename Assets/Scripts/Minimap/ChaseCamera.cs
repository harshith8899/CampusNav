using UnityEngine;

namespace CampusNav.Minimap
{
    /// <summary>
    /// Follows behind and above the player marker at a fixed offset,
    /// angled downward — the classic "racing game minimap" chase view.
    /// Renders to a RenderTexture that a RawImage in the main AR canvas
    /// displays as a corner overlay (see README for the UI wiring).
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform _playerMarker;

        [Header("Framing")]
        [SerializeField] private float _distanceBehind = 8f;
        [SerializeField] private float _heightAbove = 10f;
        // Note: the downward camera angle is a natural result of
        // _heightAbove vs _distanceBehind (the camera looks at the marker
        // from up and behind) — raise _heightAbove or lower _distanceBehind
        // for a steeper angle, no separate tilt parameter needed.

        [Header("Smoothing")]
        [SerializeField] private float _positionSmoothTime = 0.25f;
        [SerializeField] private float _rotationSmoothSpeed = 5f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (_playerMarker == null) return;

            Vector3 behindDir = -_playerMarker.forward;
            behindDir.y = 0f;
            if (behindDir.sqrMagnitude < 0.0001f) behindDir = Vector3.back;
            behindDir.Normalize();

            Vector3 desiredPosition = _playerMarker.position
                                       + behindDir * _distanceBehind
                                       + Vector3.up * _heightAbove;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);

            Quaternion desiredRotation = Quaternion.LookRotation((_playerMarker.position - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * _rotationSmoothSpeed);
        }
    }
}
