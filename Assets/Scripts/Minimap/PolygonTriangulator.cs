using System.Collections.Generic;
using UnityEngine;

namespace CampusNav.Minimap
{
    /// <summary>
    /// Minimal ear-clipping triangulator for simple (non-self-intersecting)
    /// 2D polygons, used to turn a hand-traced building footprint into a
    /// flat roof mesh. Good enough for building outlines (a few points,
    /// mostly convex with maybe one or two concave corners) — not meant for
    /// arbitrary complex geometry.
    /// </summary>
    public static class PolygonTriangulator
    {
        /// <summary>
        /// Triangulates a polygon given as XZ points. Returns a flat list of
        /// indices into the input list, 3 per triangle, wound
        /// counter-clockwise when viewed from +Y (matches Unity's default
        /// front-face winding for a mesh you view from above... actually
        /// Unity uses clockwise for front faces as seen by the camera, so
        /// callers should flip winding if the roof renders back-face-culled
        /// from above; this method focuses on correctness of the split, not
        /// a specific renderer's winding convention).
        /// </summary>
        public static List<int> Triangulate(List<Vector2> points)
        {
            var indices = new List<int>();
            if (points == null || points.Count < 3) return indices;

            var remaining = new List<int>();
            for (int i = 0; i < points.Count; i++) remaining.Add(i);

            // Ensure counter-clockwise winding (positive signed area) —
            // ear-clipping assumes this.
            if (SignedArea(points) < 0f) remaining.Reverse();

            int guard = 0;
            int maxIterations = points.Count * points.Count * 2 + 16;

            while (remaining.Count > 3 && guard++ < maxIterations)
            {
                bool clipped = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    int iPrev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int iCurr = remaining[i];
                    int iNext = remaining[(i + 1) % remaining.Count];

                    if (IsEar(points, remaining, iPrev, iCurr, iNext))
                    {
                        indices.Add(iPrev);
                        indices.Add(iCurr);
                        indices.Add(iNext);
                        remaining.RemoveAt(i);
                        clipped = true;
                        break;
                    }
                }

                if (!clipped)
                {
                    // Degenerate/self-intersecting input — bail out rather
                    // than looping forever; caller gets a partial mesh.
                    Debug.LogWarning("CampusNav: PolygonTriangulator could not fully triangulate polygon (check for self-intersections).");
                    break;
                }
            }

            if (remaining.Count == 3)
            {
                indices.Add(remaining[0]);
                indices.Add(remaining[1]);
                indices.Add(remaining[2]);
            }

            return indices;
        }

        private static bool IsEar(List<Vector2> pts, List<int> remaining, int a, int b, int c)
        {
            Vector2 pa = pts[a], pb = pts[b], pc = pts[c];

            // Must be convex at b.
            if (Cross(pb - pa, pc - pb) <= 0f) return false;

            // No other remaining point may lie inside triangle a-b-c.
            foreach (int idx in remaining)
            {
                if (idx == a || idx == b || idx == c) continue;
                if (PointInTriangle(pts[idx], pa, pb, pc)) return false;
            }
            return true;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a);
            float d2 = Cross(c - b, p - b);
            float d3 = Cross(a - c, p - c);

            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static float SignedArea(List<Vector2> pts)
        {
            float area = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[(i + 1) % pts.Count];
                area += (a.x * b.y - b.x * a.y);
            }
            return area * 0.5f;
        }
    }
}
