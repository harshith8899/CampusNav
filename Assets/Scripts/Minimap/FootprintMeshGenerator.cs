using System.Collections.Generic;
using UnityEngine;
using CampusNav.Data;

namespace CampusNav.Minimap
{
    /// <summary>
    /// Builds a simple low-poly extruded block mesh (flat roof + straight
    /// side walls) from a building's footprint polygon + height — this is
    /// what makes the mini-map's "Apple-Maps-flyover" style buildings
    /// without needing any hand-modeled assets.
    /// </summary>
    public static class FootprintMeshGenerator
    {
        public static Mesh Generate(List<Vec2> footprint, float height)
        {
            if (footprint == null || footprint.Count < 3) return new Mesh();

            var points2D = new List<Vector2>();
            foreach (var p in footprint) points2D.Add(new Vector2(p.x, p.z));

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();

            int n = points2D.Count;

            // --- Roof (top face, at y = height) ---
            int roofStart = vertices.Count;
            foreach (var p in points2D) vertices.Add(new Vector3(p.x, height, p.y));
            var roofTris = PolygonTriangulator.Triangulate(points2D);
            for (int i = 0; i < roofTris.Count; i += 3)
            {
                // Reverse winding so the roof faces up (+Y) under Unity's
                // clockwise-front-face convention.
                triangles.Add(roofStart + roofTris[i]);
                triangles.Add(roofStart + roofTris[i + 2]);
                triangles.Add(roofStart + roofTris[i + 1]);
            }
            for (int i = 0; i < n; i++) normals.Add(Vector3.up);

            // --- Side walls (one quad per edge) ---
            for (int i = 0; i < n; i++)
            {
                Vector2 a = points2D[i];
                Vector2 b = points2D[(i + 1) % n];

                int baseIndex = vertices.Count;

                Vector3 aBottom = new Vector3(a.x, 0f, a.y);
                Vector3 bBottom = new Vector3(b.x, 0f, b.y);
                Vector3 aTop = new Vector3(a.x, height, a.y);
                Vector3 bTop = new Vector3(b.x, height, b.y);

                vertices.Add(aBottom); // baseIndex + 0
                vertices.Add(bBottom); // baseIndex + 1
                vertices.Add(bTop);    // baseIndex + 2
                vertices.Add(aTop);    // baseIndex + 3

                Vector3 wallNormal = Vector3.Cross(bBottom - aBottom, aTop - aBottom).normalized;
                for (int k = 0; k < 4; k++) normals.Add(wallNormal);

                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);

                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }

            var mesh = new Mesh { name = "BuildingFootprintMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
