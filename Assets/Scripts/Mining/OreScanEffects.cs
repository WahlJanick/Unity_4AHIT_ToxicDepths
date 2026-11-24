using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToxicDepths.Mining
{
    internal sealed class OreScanEffects : IDisposable
    {
        private static readonly int Sweep = Shader.PropertyToID("_Sweep");
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");
        private static readonly int Contact = Shader.PropertyToID("_Contact");
        private static readonly int Impact = Shader.PropertyToID("_Impact");
        private static readonly int WorldToBeam = Shader.PropertyToID("_WorldToBeam");
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        private static readonly Color ToxicOutline = new Color(0.22f, 0.72f, 0.36f);
        private static readonly Color NeutralOutline = Color.white;
        private readonly Dictionary<Mesh, Mesh> meshes = new Dictionary<Mesh, Mesh>();
        private readonly Dictionary<OreDeposit, List<(MeshRenderer renderer, Mesh mesh)>> targets =
            new Dictionary<OreDeposit, List<(MeshRenderer renderer, Mesh mesh)>>();
        private readonly MaterialPropertyBlock oreProperties = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock beamProperties = new MaterialPropertyBlock();
        private readonly Camera camera;
        private readonly Material wireframe;
        private readonly Material beamMaterial;
        private readonly Mesh beam;
        private Matrix4x4 worldToBeam;
        private Vector4 impact;

        internal OreScanEffects(Camera camera, Material wireframe, Material beamMaterial)
        {
            this.camera = camera;
            this.wireframe = wireframe;
            this.beamMaterial = beamMaterial;
            beam = CreateBeamMesh();
        }

        private static Mesh CreateBeamMesh()
        {
            const int sides = 4;
            const float aperture = 0.12f;
            const float cornerRadius = 0.7071068f;
            var vertices = new Vector3[sides * 2 + 2];
            var uv = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[sides * 12];
            int nearCenter = sides * 2;
            int farCenter = nearCenter + 1;
            vertices[farCenter] = Vector3.forward;
            uv[farCenter] = Vector2.up;
            normals[nearCenter] = Vector3.back;
            normals[farCenter] = Vector3.forward;

            for (int i = 0; i < sides; i++)
            {
                float angle = (i + 0.5f) * (2f * Mathf.PI / sides);
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);
                var rim = new Vector3(x * cornerRadius, y * cornerRadius, 0f);
                int far = i + sides;
                vertices[i] = rim * aperture;
                vertices[far] = rim + Vector3.forward;
                normals[i] = normals[far] = new Vector3(x, y, (aperture - 1f) * 0.5f).normalized;
                uv[far] = Vector2.up;

                int next = (i + 1) % sides;
                int farNext = next + sides;
                int index = i * 12;
                triangles[index] = i;
                triangles[index + 1] = next;
                triangles[index + 2] = far;
                triangles[index + 3] = next;
                triangles[index + 4] = farNext;
                triangles[index + 5] = far;
                triangles[index + 6] = nearCenter;
                triangles[index + 7] = next;
                triangles[index + 8] = i;
                triangles[index + 9] = farCenter;
                triangles[index + 10] = far;
                triangles[index + 11] = farNext;
            }

            var mesh = new Mesh { name = "Scanner beam volume", vertices = vertices, normals = normals, uv = uv, triangles = triangles };
            mesh.UploadMeshData(true);
            return mesh;
        }

        internal void Prepare(OreDeposit ore)
        {
            if (targets.ContainsKey(ore)) return;

            var parts = new List<(MeshRenderer renderer, Mesh mesh)>();
            foreach (var renderer in ore.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
                Mesh original = filter.sharedMesh;
                if (!meshes.TryGetValue(original, out Mesh overlay))
                {
                    overlay = BarycentricWireframeUtility.CreateBarycentricMesh(original);
                    meshes.Add(original, overlay);
                }
                if (overlay != null) parts.Add((renderer, overlay));
            }
            targets.Add(ore, parts);
        }

        internal void DrawScanned(OreDeposit scanning)
        {
            foreach (var ore in targets.Keys)
            {
                if (ore == null || ore == scanning || !ore.isActiveAndEnabled || ore.IsDepleted || !ore.WasScanned)
                    continue;
                DrawOre(ore, 1f, 0.24f, 0f, false, ore.ReleasesPoison ? ToxicOutline : NeutralOutline);
            }
        }

        internal void DrawOre(OreDeposit ore, float progress, float opacity, float flash, bool illuminated, Color? tint = null)
        {
            if (opacity <= 0f && (!illuminated || impact.x <= 0f)) return;
            if (!targets.TryGetValue(ore, out var parts) || parts.Count == 0) return;

            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;
            foreach (var (renderer, _) in parts)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Bounds bounds = renderer.bounds;
                bottom = Mathf.Min(bottom, bounds.min.y);
                top = Mathf.Max(top, bounds.max.y);
            }
            if (top <= bottom) return;

            oreProperties.SetVector(Sweep, new Vector4(bottom, 1f / (top - bottom), progress, flash));
            oreProperties.SetFloat(Opacity, opacity);
            oreProperties.SetColor(Tint, tint ?? wireframe.GetColor(Tint));
            oreProperties.SetVector(Impact, illuminated ? impact : Vector4.zero);
            oreProperties.SetMatrix(WorldToBeam, worldToBeam);
            foreach (var (renderer, mesh) in parts)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var render = new RenderParams(wireframe)
                {
                    camera = camera, layer = renderer.gameObject.layer, matProps = oreProperties,
                    worldBounds = renderer.bounds, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false
                };
                Graphics.RenderMesh(render, mesh, 0, renderer.localToWorldMatrix);
            }
        }

        internal void DrawBeam(Vector3 origin, Vector3 end, float opacity, float maxRange, float extension, bool hitsOre)
        {
            impact = Vector4.zero;
            if (opacity <= 0f) return;
            Vector3 direction = end - origin;
            float distance = direction.magnitude;
            float length = Mathf.Min(distance, maxRange) * extension;
            if (length < 0.04f || Vector3.Dot(end - camera.transform.position, camera.transform.forward) <=
                Vector3.Dot(origin - camera.transform.position, camera.transform.forward)) return;
            end = origin + direction * (length / distance);

            beamProperties.SetFloat(Opacity, opacity);
            float width = Mathf.Clamp(length * 0.19f, 0.12f, 0.82f);
            var render = new RenderParams(beamMaterial)
            {
                camera = camera, matProps = beamProperties,
                worldBounds = new Bounds((origin + end) * 0.5f, Vector3.one * (length + width)),
                shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false
            };
            var pose = Matrix4x4.TRS(origin, Quaternion.LookRotation(direction, camera.transform.up),
                new Vector3(width, width, length));
            float contact = hitsOre && distance <= maxRange ? Mathf.SmoothStep(0f, 1f, (extension - 0.94f) / 0.06f) : 0f;
            beamProperties.SetFloat(Contact, contact);
            if (contact > 0f)
            {
                worldToBeam = pose.inverse;
                impact = new Vector4(contact * opacity, length / Mathf.Max(width * 0.65f, 0.12f), distance / length, 0f);
            }
            Graphics.RenderMesh(render, beam, 0, pose);
        }

        public void Dispose()
        {
            foreach (var mesh in meshes.Values)
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
            UnityEngine.Object.Destroy(beam);
            targets.Clear();
            meshes.Clear();
        }
    }
}
