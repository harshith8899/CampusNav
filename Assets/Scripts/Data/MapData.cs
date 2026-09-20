using System;
using System.Collections.Generic;

namespace CampusNav.Data
{
    /// <summary>
    /// Root data model for the entire campus. This is the shape the JSON files
    /// on disk (or, later, a backend API response) must match.
    ///
    /// Coordinate convention:
    /// - "Local" coordinates (x, z) are meters on a flat plane, all relative to
    ///   CampusData.originLat/originLon (see Utils/GeoConverter.cs).
    /// - Floor-local waypoints use the SAME shared local space, just with a
    ///   floor's vertical offset applied via Floor.elevationMeters.
    /// - Outdoor waypoints carry real lat/lon; indoor waypoints normally don't
    ///   need lat/lon at all, since they're resolved via QR anchors instead.
    /// </summary>
    [Serializable]
    public class CampusData
    {
        public string campusName;

        // GPS origin point used to convert every lat/lon in this file into
        // local XZ meters. Pick any fixed point on campus (e.g. main gate).
        public double originLat;
        public double originLon;

        public List<BuildingData> buildings = new List<BuildingData>();

        // Outdoor waypoints connect building entrances to each other
        // (paths, roads, quads). Edges of type Outdoor live here too.
        public List<WaypointData> outdoorWaypoints = new List<WaypointData>();
        public List<EdgeData> outdoorEdges = new List<EdgeData>();
    }

    [Serializable]
    public class BuildingData
    {
        public string id;
        public string name;

        // The outdoor waypoint ID a user arrives at when walking up to this
        // building. This is the hand-off point between outdoor and indoor nav.
        public string entranceWaypointId;

        // Ground-floor footprint polygon in local XZ meters (shared campus
        // coordinate space), used to extrude a low-poly 3D block for the
        // mini-map. Wound consistently (clockwise) when authored.
        public List<Vec2> footprint = new List<Vec2>();
        public float height = 12f; // meters, used for extrusion

        public List<FloorData> floors = new List<FloorData>();

        // Edges that connect waypoints across two different floors of THIS
        // building (elevators, stairwells). Kept separate from each floor's
        // own "edges" list because a single floor doesn't own a cross-floor
        // connection — it belongs to the building as a whole.
        public List<EdgeData> crossFloorEdges = new List<EdgeData>();
    }

    [Serializable]
    public class FloorData
    {
        public string id;          // globally unique, e.g. "BuildingA_F2"
        public int level;          // 0 = ground floor, 1 = first floor, etc.
        public string name;        // display name, e.g. "2nd Floor"
        public float elevationMeters; // vertical offset from ground, for mini-map stacking

        public List<WaypointData> waypoints = new List<WaypointData>();
        public List<EdgeData> edges = new List<EdgeData>();
    }

    [Serializable]
    public class WaypointData
    {
        public string id;          // globally unique across the whole campus

        // Local XZ position in meters (shared campus coordinate space).
        // For indoor waypoints this is floor-local-but-campus-aligned: place
        // them using the same origin/scale as the building footprint.
        public float x;
        public float z;

        // Optional real-world GPS, only meaningful for outdoor waypoints or
        // building entrances (used to re-derive local x/z, or to sanity-check
        // authored data against GPS).
        public bool hasGps;
        public double lat;
        public double lon;

        // If true, this waypoint corresponds to a physical QR code sticker.
        // Scanning that QR resolves the player's position/orientation to
        // this waypoint.
        public bool isQrAnchor;
        public string qrCodeId; // the string payload encoded in the QR

        // Optional: if this waypoint IS a destination (a room, office, lab),
        // give it a searchable name. Waypoints that are just path junctions
        // can leave this blank.
        public string roomName;

        // Facing direction in degrees (0 = north/+Z), used to orient the
        // player after a QR scan resolves them to this waypoint.
        public float anchorHeadingDegrees;
    }

    public enum EdgeType
    {
        Walk = 0,
        Stairs = 1,
        Elevator = 2,
        Outdoor = 3,
        BuildingTransition = 4 // connects an outdoor waypoint to a building's entrance waypoint
    }

    [Serializable]
    public class EdgeData
    {
        public string fromId;
        public string toId;
        public EdgeType type;

        // Optional manual cost override (e.g. to penalize stairs/elevators).
        // If <= 0, the pathfinder computes cost from straight-line distance.
        public float costOverride;

        // Most edges are walkable both ways; set false for one-way cases
        // (e.g. a one-way turnstile or exit-only door).
        public bool bidirectional = true;
    }

    [Serializable]
    public struct Vec2
    {
        public float x;
        public float z;

        public Vec2(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
    }
}
