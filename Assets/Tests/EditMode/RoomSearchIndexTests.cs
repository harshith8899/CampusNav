using System.Linq;
using NUnit.Framework;
using CampusNav.Data;
using CampusNav.Search;

namespace CampusNav.EditorTools.Tests
{
    public class RoomSearchIndexTests
    {
        private RoomSearchIndex BuildIndex()
        {
            var provider = new LocalJsonMapDataProvider("sample_campus.json");
            CampusData campus = provider.GetCampusData();
            var waypointIndex = new WaypointIndex(campus);
            return new RoomSearchIndex(campus, waypointIndex);
        }

        [Test]
        public void Search_RoomTwoOhOne_ReturnsExpectedWaypointId()
        {
            RoomSearchIndex index = BuildIndex();

            var results = index.Search("Room 201");

            var ids = results.Select(r => r.WaypointId).Distinct().ToList();
            CollectionAssert.AreEquivalent(new[] { "BLDG_A_F1_ROOM201" }, ids);
        }

        [Test]
        public void Search_BlockA_ReturnsExpectedWaypointIds()
        {
            RoomSearchIndex index = BuildIndex();

            var results = index.Search("Block A");

            // "Block A" matches the building entry itself, its entrance
            // (indexed twice — once as a named room, once as the building's
            // entrance waypoint), and every room whose display name is
            // suffixed with the building name.
            var ids = results.Select(r => r.WaypointId).Distinct().ToList();
            CollectionAssert.AreEquivalent(
                new[] { "BLDG_A_ENTRANCE", "BLDG_A_F0_ROOM101", "BLDG_A_F1_ROOM201" },
                ids);
        }
    }
}
