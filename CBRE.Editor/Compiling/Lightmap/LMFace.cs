using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Compiling.Lightmap {
    public class LMFace {
        public PlaneF Plane { get; set; }
        public Vector3F Normal;
        public Vector3F Tangent;
        public Vector3F Bitangent;

        public Vector3F LightBasis0;
        public Vector3F LightBasis1;
        public Vector3F LightBasis2;

        public bool CastsShadows;

        public int LmIndex;

        public class Vertex {
            public Vertex(DataStructures.MapObjects.Vertex original) {
                OriginalVertex = original;
                Location = new Vector3F(original.Location);
                DiffU = (float)original.TextureU; DiffV = (float)original.TextureV;
                LMU = original.LMU; LMV = original.LMV;
            }
            public Vector3F Location;
            public float DiffU; public float DiffV;
            public float LMU; public float LMV;
            public DataStructures.MapObjects.Vertex OriginalVertex;
        };

        public List<Vertex> Vertices { get; set; }

        public BoxF BoundingBox { get; set; }

        public BoxF PaddedBoundingBox(float padding = 3.0f) {
            Vector3F boxPadding = new Vector3F(padding, padding, padding);
            return new BoxF(BoundingBox.Start - boxPadding, BoundingBox.End + boxPadding);
        }

        public string Texture;

        public Face OriginalFace;

        public LMFace(Face face, Solid solid) {
            Plane = new PlaneF(face.Plane);

            Normal = Plane.Normal;

            Vertices = face.Vertices.Select(x => new Vertex(x)).ToList();

            CastsShadows = !(solid?.Parent?.GetEntityData()?.Name.Equals("noshadow", StringComparison.OrdinalIgnoreCase) ?? false);

            int i1 = 0;
            int i2 = 1;
            int i3 = 2;

            Vector3F v1 = Vertices[i1].Location;
            Vector3F v2 = Vertices[i2].Location;
            Vector3F v3 = Vertices[i3].Location;

            float w1x = Vertices[i1].DiffU; float w1y = Vertices[i1].DiffV;
            float w2x = Vertices[i2].DiffU; float w2y = Vertices[i2].DiffV;
            float w3x = Vertices[i3].DiffU; float w3y = Vertices[i3].DiffV;

            float x1 = v2.X - v1.X;
            float x2 = v3.X - v1.X;
            float y1 = v2.Y - v1.Y;
            float y2 = v3.Y - v1.Y;
            float z1 = v2.Z - v1.Z;
            float z2 = v3.Z - v1.Z;

            float s1 = w2x - w1x;
            float s2 = w3x - w1x;
            float t1 = w2y - w1y;
            float t2 = w3y - w1y;

            float r = 1.0f / (s1 * t2 - s2 * t1);
            Vector3F sdir = new Vector3F((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3F tdir = new Vector3F((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            Tangent = (sdir - Normal * Normal.Dot(sdir)).Normalise();
            Bitangent = (tdir - Normal * Normal.Dot(tdir)).Normalise();

            LightBasis0 = Tangent * (-1.0f / (float)Math.Sqrt(6.0)) + Bitangent * (-1.0f / (float)Math.Sqrt(2.0)) + Normal * (1.0f / (float)Math.Sqrt(3.0));
            LightBasis1 = Tangent * (-1.0f / (float)Math.Sqrt(6.0)) + Bitangent * (1.0f / (float)Math.Sqrt(2.0)) + Normal * (1.0f / (float)Math.Sqrt(3.0));
            LightBasis2 = Tangent * ((float)Math.Sqrt(2.0 / 3.0)) + Normal * (1.0f / (float)Math.Sqrt(3.0));

            Texture = face.Texture.Name;

            OriginalFace = face;

            UpdateBoundingBox();
        }

        public virtual IEnumerable<LineF> GetLines() {
            return GetEdges();
        }

        public virtual IEnumerable<LineF> GetEdges() {
            for (var i = 0; i < Vertices.Count; i++) {
                yield return new LineF(Vertices[i].Location, Vertices[(i + 1) % Vertices.Count].Location);
            }
        }

        public virtual IEnumerable<Vertex> GetIndexedVertices() {
            return Vertices;
        }

        public virtual IEnumerable<uint> GetTriangleIndices() {
            for (uint i = 1; i < Vertices.Count - 1; i++) {
                yield return 0;
                yield return i;
                yield return i + 1;
            }
        }

        public virtual IEnumerable<Vertex[]> GetTriangles() {
            for (var i = 1; i < Vertices.Count - 1; i++) {
                yield return new[]
                {
                    Vertices[0],
                    Vertices[i],
                    Vertices[i + 1]
                };
            }
        }

        public virtual void UpdateBoundingBox() {
            BoundingBox = new BoxF(Vertices.Select(x => x.Location));
        }

        /// <summary>
        /// Returns the point that this line intersects with this face.
        /// </summary>
        /// <param name="line">The intersection line</param>
        /// <returns>The point of intersection between the face and the line.
        /// Returns null if the line does not intersect this face.</returns>
        public virtual Vector3F GetIntersectionPoint(LineF line) {
            return GetIntersectionPoint(this, line);
        }

        /// <summary>
        /// Test all the edges of this face against a bounding box to see if they intersect.
        /// </summary>
        /// <param name="box">The box to intersect</param>
        /// <returns>True if one of the face's edges intersects with the box.</returns>
        public bool IntersectsWithLine(BoxF box) {
            // Shortcut through the bounding box to avoid the line computations if they aren't needed
            return BoundingBox.IntersectsWith(box) && GetLines().Any(box.IntersectsWith);
        }

        /// <summary>
        /// Test this face to see if the given bounding box intersects with it
        /// </summary>
        /// <param name="box">The box to test against</param>
        /// <returns>True if the box intersects</returns>
        public bool IntersectsWithBox(BoxF box) {
            var verts = Vertices.ToList();
            return box.GetBoxLines().Any(x => GetIntersectionPoint(this, x, true) != null);
        }

        protected static Vector3F GetIntersectionPoint(LMFace face, LineF line, bool ignoreDirection = false) {
            var plane = face.Plane;
            var intersect = plane.GetIntersectionPoint(line, ignoreDirection);
            List<Vector3F> coordinates = face.Vertices.Select(x => x.Location).ToList();
            if (intersect == null) return null;
            BoxF bbox = new BoxF(face.BoundingBox.Start - new Vector3F(0.5f, 0.5f, 0.5f), face.BoundingBox.End + new Vector3F(0.5f, 0.5f, 0.5f));
            if (!bbox.Vector3IsInside(intersect)) return null;

            Vector3F centerPoint = face.BoundingBox.Center;
            for (var i = 0; i < coordinates.Count; i++) {
                var i1 = i;
                var i2 = (i + 1) % coordinates.Count;

                var lineMiddle = (coordinates[i1] + coordinates[i2]) * 0.5f;
                var middleToCenter = centerPoint - lineMiddle;
                var v = coordinates[i1] - coordinates[i2];
                var lineNormal = face.Plane.Normal.Cross(v);

                if ((middleToCenter - lineNormal).LengthSquared() > (middleToCenter + lineNormal).LengthSquared()) {
                    lineNormal = -lineNormal;
                }

                if (lineNormal.Dot(intersect - lineMiddle) < 0.0f) return null;
            }
            return intersect;
        }

        public static void FindFacesAndGroups(Map map, out List<LMFace> faces, out List<LightmapGroup> lmGroups) {
            faces = new List<LMFace>();
            lmGroups = new List<LightmapGroup>();
            foreach (Solid solid in map.WorldSpawn.Find(x => x is Solid).OfType<Solid>()) {
                foreach (Face solidFace in solid.Faces) {
                    solidFace.Vertices.ForEach(v => { v.LMU = -500.0f; v.LMV = -500.0f; });
                    solidFace.UpdateBoundingBox();
                    if (solidFace.Texture?.Texture is null) { continue; }
                    if (solidFace.Texture.Name.StartsWith("tooltextures/", StringComparison.OrdinalIgnoreCase)) { continue; }
                    if (solidFace.Texture.Texture.HasTransparency()) { continue; } //TODO: use translucent textures for lighting effects!
                    LMFace face = new LMFace(solidFace, solid);
                    LightmapGroup group = LightmapGroup.FindCoplanar(lmGroups, face);
                    if (group is null) {
                        group = new LightmapGroup();
                        lmGroups.Add(group);
                    }
                    group.AddFace(face);
                }
            }
        }
    }
}
