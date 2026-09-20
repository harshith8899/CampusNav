using System.Collections.Generic;
using NUnit.Framework;
using CampusNav.Data;

namespace CampusNav.EditorTools.Tests
{
    public class WaypointIndexTests
    {
        [Test]
        public void FlattenedIndex_ContainsEveryWaypointId_ExactlyOnce()
        {
            var provider = new LocalJsonMapDataProvider("sample_campus.json");
            CampusData campus = provider.GetCampusData();

            var expectedIds = new List<string>();
            foreach (var wp in campus.outdoorWaypoints) expectedIds.Add(wp.id);
            foreach (var building in campus.buildings)
            {
                foreach (var floor in building.floors)
                {
                    foreach (var wp in floor.waypoints) expectedIds.Add(wp.id);
                }
            }

            var index = new WaypointIndex(campus);

            // If the same id appeared twice in the source data, WaypointIndex
            // would silently drop the duplicate — matching counts here proves
            // both "every id is present" and "every id is unique".
            Assert.AreEqual(expectedIds.Count, index.All.Count, "Flattened index size does not match the raw waypoint count — duplicate or missing id.");

            foreach (var id in expectedIds)
            {
                Assert.IsTrue(index.TryGetById(id, out _), $"Waypoint id '{id}' is missing from the flattened index.");
            }
        }
    }
}
