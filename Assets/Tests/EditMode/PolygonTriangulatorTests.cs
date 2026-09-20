using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CampusNav.Minimap;

namespace CampusNav.EditorTools.Tests
{
    public class PolygonTriangulatorTests
    {
        [Test]
        public void Triangulate_Rectangle_ProducesTwoTriangles()
        {
            var rectangle = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(2f, 1f),
                new Vector2(0f, 1f),
            };

            List<int> indices = PolygonTriangulator.Triangulate(rectangle);

            Assert.AreEqual(6, indices.Count, "Expected 2 triangles (6 indices) for a rectangle.");
        }

        [Test]
        public void Triangulate_ConcaveLShape_ProducesFourTriangles()
        {
            // A 2x2 square with its top-right 1x1 corner notched out —
            // one reflex (concave) vertex, wound counter-clockwise.
            var lShape = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(2f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 2f),
                new Vector2(0f, 2f),
            };

            List<int> indices = PolygonTriangulator.Triangulate(lShape);

            Assert.AreEqual(12, indices.Count, "Expected 4 triangles (12 indices) for the L-shape.");
        }
    }
}
