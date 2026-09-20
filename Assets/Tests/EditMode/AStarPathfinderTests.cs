using NUnit.Framework;
using CampusNav.Data;
using CampusNav.Pathfinding;

namespace CampusNav.EditorTools.Tests
{
    public class AStarPathfinderTests
    {
        [Test]
        public void FindPath_MainGateToRoom201_PassesThroughBothElevatorsInOrder()
        {
            var provider = new LocalJsonMapDataProvider("sample_campus.json");
            CampusData campus = provider.GetCampusData();
            var waypointIndex = new WaypointIndex(campus);
            var graph = new MapGraph(campus, waypointIndex);
            var pathfinder = new AStarPathfinder(graph);

            var path = pathfinder.FindPath("OUT_MAIN_GATE", "BLDG_A_F1_ROOM201");

            Assert.IsNotNull(path, "Expected a path from OUT_MAIN_GATE to BLDG_A_F1_ROOM201.");
            Assert.AreEqual("OUT_MAIN_GATE", path[0]);
            Assert.AreEqual("BLDG_A_F1_ROOM201", path[path.Count - 1]);

            int groundElevIndex = path.IndexOf("BLDG_A_F0_ELEV");
            int firstFloorElevIndex = path.IndexOf("BLDG_A_F1_ELEV");

            Assert.GreaterOrEqual(groundElevIndex, 0, "Path does not pass through BLDG_A_F0_ELEV.");
            Assert.GreaterOrEqual(firstFloorElevIndex, 0, "Path does not pass through BLDG_A_F1_ELEV.");
            Assert.Less(groundElevIndex, firstFloorElevIndex, "Path reaches BLDG_A_F1_ELEV before BLDG_A_F0_ELEV.");
        }
    }
}
