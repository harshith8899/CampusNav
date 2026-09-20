using UnityEngine;
using CampusNav.Data;

namespace CampusNav.Minimap
{
    /// <summary>
    /// Spawns one low-poly extruded block GameObject per building, using
    /// FootprintMeshGenerator, into the mini-map's own scene/render layer.
    ///
    /// MANUAL EDITOR STEPS REQUIRED:
    /// - Assign _buildingMaterial (any simple flat-color/unlit material).
    /// - Put the spawned buildings, the player marker, and the mini-map
    ///   camera all on a dedicated layer (e.g. "Minimap") so the main AR
    ///   camera does NOT render them, and set the mini-map camera's Culling
    ///   Mask to only that layer.
    /// </summary>
    public class CampusMeshBuilder : MonoBehaviour
    {
        [SerializeField] private Material _buildingMaterial;
        [SerializeField] private int _minimapLayer;

        public void Build(CampusData campus)
        {
            foreach (var building in campus.buildings)
            {
                var go = new GameObject($"Building_{building.id}");
                go.transform.SetParent(transform, worldPositionStays: false);
                go.layer = _minimapLayer;

                var meshFilter = go.AddComponent<MeshFilter>();
                var meshRenderer = go.AddComponent<MeshRenderer>();

                meshFilter.mesh = FootprintMeshGenerator.Generate(building.footprint, building.height);
                meshRenderer.sharedMaterial = _buildingMaterial;
            }
        }
    }
}
