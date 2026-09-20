using NUnit.Framework;
using CampusNav.Data;

namespace CampusNav.EditorTools.Tests
{
    public class LocalJsonMapDataProviderTests
    {
        [Test]
        public void GetCampusData_ParsesSampleCampusJson_WithExpectedCounts()
        {
            var provider = new LocalJsonMapDataProvider("sample_campus.json");

            CampusData campus = provider.GetCampusData();

            Assert.IsNotNull(campus, "sample_campus.json failed to parse.");
            Assert.AreEqual("Sample Campus", campus.campusName);

            Assert.AreEqual(1, campus.buildings.Count, "Unexpected building count.");
            Assert.AreEqual(2, campus.buildings[0].floors.Count, "Unexpected floor count for BLDG_A.");
            Assert.AreEqual(4, campus.buildings[0].floors[0].waypoints.Count, "Unexpected waypoint count on Ground Floor.");
            Assert.AreEqual(3, campus.buildings[0].floors[1].waypoints.Count, "Unexpected waypoint count on 1st Floor.");
            Assert.AreEqual(2, campus.outdoorWaypoints.Count, "Unexpected outdoor waypoint count.");
        }
    }
}
