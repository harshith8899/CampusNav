#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CampusNav.Navigation;

namespace CampusNav.PlayModeTests
{
    /// <summary>
    /// Exercises outdoor GPS navigation end-to-end using
    /// GpsCompassNavigator's Editor-only debug override
    /// (GpsCompassNavigator.DebugApplyOverride), since real Input.location
    /// readings aren't available in the Editor. Wrapped in UNITY_EDITOR, like
    /// the API it exercises, so this assembly can stay a normal
    /// (non-Editor-restricted) PlayMode test assembly — required for Unity's
    /// Test Runner to categorize it as PlayMode at all — while still
    /// compiling safely if it were ever pulled into an on-device Play Mode
    /// player test build, where DebugApplyOverride doesn't exist.
    /// </summary>
    public class OutdoorNavigationDebugGpsTests
    {
        private GameObject _root;
        private NavigationModeController _controller;
        private GpsCompassNavigator _gpsNavigator;
        private OutdoorArrowUI _outdoorArrow;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestNavigationRoot");
            _root.SetActive(false); // defer Awake/OnEnable until fully wired

            _gpsNavigator = _root.AddComponent<GpsCompassNavigator>();

            var arrowGo = new GameObject("TestArrow", typeof(RectTransform));
            arrowGo.transform.SetParent(_root.transform);
            _outdoorArrow = arrowGo.AddComponent<OutdoorArrowUI>();
            SetPrivateField(_outdoorArrow, "_arrowRectTransform", arrowGo.GetComponent<RectTransform>());
            SetPrivateField(_outdoorArrow, "_navigator", _gpsNavigator);

            _controller = _root.AddComponent<NavigationModeController>();
            SetPrivateField(_controller, "_gpsNavigator", _gpsNavigator);
            SetPrivateField(_controller, "_outdoorArrow", _outdoorArrow);

            _root.SetActive(true);

            // Fake a GPS fix right at OUT_MAIN_GATE (sample_campus.json),
            // deterministically, instead of relying on real Input.location.
            _gpsNavigator.DebugApplyOverride(new Vector2(10f, -60f), headingDegrees: 0f, accuracyMeters: 5f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
        }

        [Test]
        public void NavigateTo_FromFakeGpsFix_PointsArrowAtNextWaypointLocalPosition()
        {
            bool started = _controller.NavigateTo("BLDG_A_ENTRANCE");

            Assert.IsTrue(started, "NavigateTo should find a path from the faked GPS fix to BLDG_A_ENTRANCE.");
            Assert.IsTrue(_outdoorArrow.HasTarget, "OutdoorArrowUI should have a target after NavigateTo.");

            // Path from the fake fix is OUT_MAIN_GATE -> OUT_PATH_JUNCTION1 ->
            // BLDG_A_ENTRANCE. NavigateTo force-advances past the start node,
            // so the first pushed target is BLDG_A_ENTRANCE, whose local
            // position comes straight from sample_campus.json (x=10, z=0.5).
            var expected = new Vector2(10f, 0.5f);
            Assert.AreEqual(expected, _outdoorArrow.TargetLocalPosition);
        }

        [Test]
        public void DebugApplyOverride_WhenRealLocationServiceUnavailable_StillMakesIsReadyTrue()
        {
            // SetUp already let the real location service attempt run first
            // (it fails on this desktop, with no GPS hardware — exactly the
            // "location services are disabled by the user/OS" case) and only
            // then applied the debug override. IsReady must still end up
            // true, driven purely by the faked reading, or nothing gated on
            // it (OutdoorArrowUI included) would ever respond in the Editor.
            Assert.IsTrue(_gpsNavigator.ServiceFailedToStart, "Expected the real location service to have failed to start in this Editor environment.");
            Assert.IsTrue(_gpsNavigator.IsReady, "IsReady should be true from the debug override even though the real GPS service failed to start.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
#endif
