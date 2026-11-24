using UnityEngine;
using UnityEngine.Rendering;

namespace ToxicDepths.Mining
{
    // Adapted from John Einselen's Unity-BarycentricWireframe (572b232).
    // The overlay only uses positions and barycentric tangents.
    internal static class BarycentricWireframeUtility
    {
        private static readonly Vector4[] Corners =
        {
            new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f)
        };

        internal static Mesh CreateBarycentricMesh(Mesh source)
        {
            if (!source.isReadable)
            {
                Debug.LogWarning($"Enable Read/Write on ore mesh '{source.name}' to render its scan overlay.", source);
                return null;
            }

            int[] triangles = source.triangles;
            Vector3[] vertices = source.vertices;
            var positions = new Vector3[triangles.Length];
            var barycentrics = new Vector4[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                positions[i] = vertices[triangles[i]];
                barycentrics[i] = Corners[i % 3];
                triangles[i] = i;
            }

            var overlay = new Mesh
            {
                name = source.name + " (Scan Overlay)",
                indexFormat = positions.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = positions,
                tangents = barycentrics,
                triangles = triangles,
                bounds = source.bounds
            };
            overlay.UploadMeshData(true);
            return overlay;
        }
    }
}
