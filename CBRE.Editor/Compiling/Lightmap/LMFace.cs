using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CBRE.Settings;

namespace CBRE.Editor.Compiling.Lightmap {
    sealed class LMFace {
        public readonly PlaneF Plane;
        public readonly Vector3F Normal;
        public readonly Vector3F Tangent;
        public readonly Vector3F Bitangent;

        public readonly bool CastsShadows;

        public Vector3F LightBasis0;
        public Vector3F LightBasis1;
        public Vector3F LightBasis2;

        public int LmIndex {
            get => OriginalFace.LmIndex;
            set => OriginalFace.LmIndex = value;
        }

        public class Vertex {
            public Vertex(DataStructures.MapObjects.Vertex original) {
                OriginalVertex = original;
            }

            public Vector3F Location => OriginalVertex.Location.ToVector3F();
            public float DiffU => (float)OriginalVertex.TextureU;
            public float DiffV => (float)OriginalVertex.TextureV;

            public float LMU {
                get => OriginalVertex.LMU;
                set => OriginalVertex.LMU = value;
            }

            public float LMV {
                get => OriginalVertex.LMV;
                set => OriginalVertex.LMV = value;
            }
            
            public readonly DataStructures.MapObjects.Vertex OriginalVertex;
        };

        public readonly ImmutableArray<Vertex> Vertices;

        public readonly BoxF BoundingBox;

        public BoxF PaddedBoundingBox(float padding = 3.0f) {
            Vector3F boxPadding = new Vector3F(padding, padding, padding);
            return new BoxF(BoundingBox.Start - boxPadding, BoundingBox.End + boxPadding);
        }

        public TextureReference Texture => OriginalFace.Texture;

        public readonly Face OriginalFace;

        public LMFace(Face face) {
            var solid = face.Parent;
            
            Plane = new PlaneF(face.Plane);

            Normal = Plane.Normal;

            Vertices = face.Vertices.Select(x => new Vertex(x)).ToImmutableArray();

            CastsShadows
                = solid?.Parent?.GetEntityData()?.Name is not { } solidEntityName
                  || !string.Equals(solidEntityName, "noShadow", StringComparison.OrdinalIgnoreCase);

            const int i1 = 0;
            const int i2 = 1;
            const int i3 = 2;

            Vector3F v1 = Vertices[i1].Location;
            Vector3F v2 = Vertices[i2].Location;
            Vector3F v3 = Vertices[i3].Location;

            Vector2F uv1 = new(Vertices[i1].DiffU, Vertices[i1].DiffV);
            Vector2F uv2 = new(Vertices[i2].DiffU, Vertices[i2].DiffV);
            Vector2F uv3 = new(Vertices[i3].DiffU, Vertices[i3].DiffV);

            Vector3F d21 = v2 - v1;
            Vector3F d31 = v3 - v1;

            Vector2F st21 = uv2 - uv1;
            Vector2F st31 = uv3 - uv1;

            float r = 1.0f / (st21.X * st31.Y - st31.X * st21.Y);
            Vector3F sDir = (d21 * st31.Y - d31 * st21.Y) * r;
            Vector3F tDir = (d31 * st21.X - d21 * st31.X) * r;

            Tangent = (sDir - Normal * Normal.Dot(sDir)).Normalise();
            Bitangent = (tDir - Normal * Normal.Dot(tDir)).Normalise();

            float sqrt2 = MathF.Sqrt(2.0f);
            float sqrt3 = MathF.Sqrt(3.0f);
            float sqrt6 = sqrt2 * sqrt3;
            float invSqrt2 = 1.0f / sqrt2;
            float invSqrt3 = 1.0f / sqrt3;
            float invSqrt6 = 1.0f / sqrt6;
            
            LightBasis0 = Tangent * (-invSqrt6) + Bitangent * (-invSqrt2) + Normal * invSqrt3;
            LightBasis1 = Tangent * (-invSqrt6) + Bitangent * invSqrt2 + Normal * invSqrt3;
            LightBasis2 = Tangent * (sqrt2 / sqrt3) + Normal * invSqrt3;

            OriginalFace = face;

            BoundingBox = new BoxF(Vertices.Select(x => x.Location));
        }

        public void UpdateLmUv(LightmapGroup group, int lmIndex) {
            LmIndex = lmIndex;
            foreach (var vertex in Vertices) {
                var u = vertex.Location.Dot(group.UvProjectionAxes.UAxis);
                var v = vertex.Location.Dot(group.UvProjectionAxes.VAxis);
                vertex.LMU = (group.StartWriteUV.U + (u - group.ProjectedBounds.Min.U) / LightmapConfig.DownscaleFactor) / LightmapConfig.TextureDims;
                vertex.LMV = (group.StartWriteUV.V + (v - group.ProjectedBounds.Min.V) / LightmapConfig.DownscaleFactor) / LightmapConfig.TextureDims;
            }
        }

        public IEnumerable<LineF> GetEdges() {
            for (var i = 0; i < Vertices.Length; i++) {
                yield return new LineF(Vertices[i].Location, Vertices[(i + 1) % Vertices.Length].Location);
            }
        }

        public IEnumerable<Vertex> GetIndexedVertices() {
            return Vertices;
        }

        public IEnumerable<uint> GetTriangleIndices() {
            for (uint i = 1; i < Vertices.Length - 1; i++) {
                yield return 0;
                yield return i;
                yield return i + 1;
            }
        }

        public IEnumerable<Vertex[]> GetTriangles() {
            for (var i = 1; i < Vertices.Length - 1; i++) {
                yield return new[]
                {
                    Vertices[0],
                    Vertices[i],
                    Vertices[i + 1]
                };
            }
        }

        /// <summary>
        /// Test all the edges of this face against a bounding box to see if they intersect.
        /// </summary>
        /// <param name="box">The box to intersect</param>
        /// <returns>True if one of the face's edges intersects with the box.</returns>
        public bool IntersectsWithEdge(BoxF box) {
            // Shortcut through the bounding box to avoid the line computations if they aren't needed
            return BoundingBox.IntersectsWith(box) && GetEdges().Any(box.IntersectsWith);
        }

        /// <summary>
        /// Test this face to see if the given bounding box intersects with it
        /// </summary>
        /// <param name="box">The box to test against</param>
        /// <returns>True if the box intersects</returns>
        public bool IntersectsWithBox(BoxF box) {
            return box.GetBoxLines().Any(x => GetIntersectionPoint(x, true) != null);
        }

        /// <summary>
        /// Returns the point where this line intersects with this face.
        /// </summary>
        /// <param name="line">The intersection line</param>
        /// <returns>The point of intersection between the face and the line.
        /// Returns null if the line does not intersect this face.</returns>
        public Vector3F? GetIntersectionPoint(LineF line, bool ignoreDirection = false) {
            var plane = Plane;
            var intersect = plane.GetIntersectionPoint(line, ignoreDirection);
            List<Vector3F> coordinates = Vertices.Select(x => x.Location).ToList();
            if (intersect == null) { return null; }
            BoxF bbox = PaddedBoundingBox(0.5f);
            if (!bbox.Vector3IsInside(intersect.Value)) { return null; }

            Vector3F centerPoint = BoundingBox.Center;
            for (var i = 0; i < coordinates.Count; i++) {
                var i1 = i;
                var i2 = (i + 1) % coordinates.Count;

                var lineMiddle = (coordinates[i1] + coordinates[i2]) * 0.5f;
                var middleToCenter = centerPoint - lineMiddle;
                var lineDirection = coordinates[i1] - coordinates[i2];
                var lineNormal = Plane.Normal.Cross(lineDirection);

                if ((middleToCenter - lineNormal).LengthSquared() > (middleToCenter + lineNormal).LengthSquared()) {
                    lineNormal = -lineNormal;
                }

                if (lineNormal.Dot(intersect.Value - lineMiddle) < 0.0f) { return null; }
            }
            return intersect;
        }
    }
}
