using UnityEngine;
using UnityEngine.UI;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Shows a "weak/no GPS signal" banner over the outdoor HUD whenever the
    /// current fix is too imprecise to trust, or the location service never
    /// managed to start (permission denied, disabled, or timed out).
    ///
    /// MANUAL EDITOR STEP REQUIRED: assign _bannerRoot to a UI panel
    /// (Image + Text) placed in the outdoor HUD canvas, and _bannerText to
    /// its Text component. This script itself should live on an
    /// always-active object — not on _bannerRoot — since it needs to keep
    /// running Update() to turn the banner back off once signal recovers.
    /// </summary>
    public class GpsSignalStatusUI : MonoBehaviour
    {
        [SerializeField] private GpsCompassNavigator _navigator;
        [SerializeField] private GameObject _bannerRoot;
        [SerializeField] private Text _bannerText;
        [SerializeField] private float _weakSignalAccuracyMeters = 20f;

        private void Update()
        {
            if (_navigator == null || _bannerRoot == null) return;

            bool serviceFailed = _navigator.ServiceFailedToStart;
            bool weakSignal = _navigator.IsReady && _navigator.CurrentAccuracyMeters > _weakSignalAccuracyMeters;
            bool show = serviceFailed || weakSignal;

            if (_bannerRoot.activeSelf != show) _bannerRoot.SetActive(show);

            if (show && _bannerText != null)
            {
                _bannerText.text = serviceFailed ? "GPS unavailable" : "Weak GPS signal";
            }
        }
    }
}
