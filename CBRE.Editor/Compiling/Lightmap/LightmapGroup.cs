#nullable enable
using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using Microsoft.Xna.Framework;
using Vector3 = CBRE.DataStructures.Geometric.Vector3;

namespace CBRE.Editor.Compiling.Lightmap {
    /// <summary>
    /// Class representing a group of faces that are
    /// (almost) coplanar and close enough to each
    /// other to justify merging together in a
    /// contiguous region on a lightmap.
    /// </summary>
    sealed class LightmapGroup {
        public PlaneF? Plane { get; private set; }
        public BoxF? BoundingBox { get; private set; }

        private readonly List<LMFace> faces;
        public IReadOnlyList<LMFace> Faces => faces;

        public struct UvAxes {
            public bool Initialized;
            public Vector3F UAxis;
            public Vector3F VAxis;

            public UvAxes(Vector3F uAxis, Vector3F vAxis) {
                Initialized = true;
                UAxis = uAxis;
                VAxis = vAxis;
            }
        }
        public UvAxes UvProjectionAxes;

        public struct UvBounds {
            public bool Initialized { get; private set; }
            public UvPairFloat Min;
            public UvPairFloat Max;

            public float USpan => Max.U - Min.U;
            public float VSpan => Max.V - Min.V;

            public void AddUv(float u, float v) {
                if (!Initialized) {
                    Initialized = true;
                    Min.U = u;
                    Min.V = v;
                    Max.U = u;
                    Max.V = v;
                } else {
                    void updateMin(ref float field, float value)
                        => field = Math.Min(field, value);
                    void updateMax(ref float field, float value)
                        => field = Math.Max(field, value);
                    updateMin(ref Min.U, u);
                    updateMin(ref Min.V, v);
                    updateMax(ref Max.U, u);
                    updateMax(ref Max.V, v);
                }
            }

            public void Inflate(float amount) {
                Min.U -= amount;
                Min.V -= amount;
                Max.U += amount;
                Max.V += amount;
            }

            public void Round(float gridSize) {
                void roundValue(ref float v)
                    => v = (float)Math.Ceiling(v / gridSize) * gridSize;
            
                roundValue(ref Min.U);
                roundValue(ref Min.V);
                roundValue(ref Max.U);
                roundValue(ref Max.V);
            }
        }
        public UvBounds ProjectedBounds;

        public Vector3F GetWorldPosForProjectedUv(float u, float v)
            => GetWorldPosForProjectedUv(new UvPairFloat { U = u, V = v });
        
        public Vector3F GetWorldPosForProjectedUv(UvPairFloat uv) {
            var uAxis = UvProjectionAxes.UAxis;
            var vAxis = UvProjectionAxes.VAxis;
            Vector2 pointOnPlaneUv = new Vector2(
                Plane.PointOnPlane.Dot(uAxis),
                Plane.PointOnPlane.Dot(vAxis));
            Vector3F worldPosition = Plane.PointOnPlane
                                   + (uv.U - pointOnPlaneUv.X - LightmapConfig.DownscaleFactor) * uAxis
                                   + (uv.V - pointOnPlaneUv.Y - LightmapConfig.DownscaleFactor) * vAxis;
            return worldPosition;
        }

        public Vector3F TopLeftWorldPos => GetWorldPosForProjectedUv(ProjectedBounds.Min.U, ProjectedBounds.Min.V);
        public Vector3F TopRightWorldPos => GetWorldPosForProjectedUv(ProjectedBounds.Max.U, ProjectedBounds.Min.V);
        public Vector3F BottomLeftWorldPos => GetWorldPosForProjectedUv(ProjectedBounds.Min.U, ProjectedBounds.Max.V);
        public Vector3F BottomRightWorldPos => GetWorldPosForProjectedUv(ProjectedBounds.Max.U, ProjectedBounds.Max.V);

        public struct UvPairFloat {
            public float U;
            public float V;
        }
        
        public struct UvPairInt {
            public int U;
            public int V;
        }
        
        public UvPairInt StartWriteUV;
        public UvPairInt EndWriteUV => new LightmapGroup.UvPairInt {
            U = StartWriteUV.U + (int)MathF.Ceiling(UvSpaceWidth),
            V = StartWriteUV.V + (int)MathF.Ceiling(UvSpaceHeight)
        };

        public LightmapGroup() {
            faces = new List<LMFace>();
        }

        public void AddFace(LMFace face) {
            faces.Add(face);
            BoxF faceBox = face.PaddedBoundingBox();
            BoundingBox = BoundingBox is null ? faceBox : new BoxF(new[] {faceBox, BoundingBox});
            var newPlane = new PlaneF(
                face.Normal,
                face.Vertices.Select(v => v.Location).Aggregate((v1,v2) => v1+v2)/face.Vertices.Length);
            if (Plane is null) {
                Plane = newPlane;
            } else {
                var otherPointOnPlane = Plane.Project(newPlane.PointOnPlane);
                Plane = new PlaneF(Plane.Normal, (otherPointOnPlane + Plane.PointOnPlane) / 2.0f);
            }
        }

        private void CalculateInitialUv() {
            if (UvProjectionAxes.Initialized && ProjectedBounds.Initialized) {
                return;
            }
            
            var direction = Plane.GetClosestAxisToNormal();
            var tempV = direction == Vector3F.UnitZ ? -Vector3F.UnitY : -Vector3F.UnitZ;
            var uAxis = Plane.Normal.Cross(tempV).Normalise();
            var vAxis = uAxis.Cross(Plane.Normal).Normalise();
            UvProjectionAxes = new UvAxes(uAxis, vAxis);

            void validateAxisAlignment(Vector3F axis, string name) {
                if (Plane.OnPlane(Plane.PointOnPlane + axis * 1000f) != 0) {
                    throw new Exception($"{name} is misaligned");
                }
            }
            
            validateAxisAlignment(uAxis, nameof(uAxis));
            validateAxisAlignment(vAxis, nameof(vAxis));

            foreach (LMFace face in Faces) {
                foreach (Vector3F coord in face.Vertices.Select(x => x.Location)) {
                    float u = coord.Dot(uAxis);
                    float v = coord.Dot(vAxis);

                    ProjectedBounds.AddUv(u, v);
                }
            }

            if (!ProjectedBounds.Initialized) {
                throw new Exception("Could not determine face minimum and maximum UVs");
            }

            ProjectedBounds.Inflate(LightmapConfig.DownscaleFactor);
            ProjectedBounds.Round(LightmapConfig.DownscaleFactor);

            if (ProjectedBounds.USpan < ProjectedBounds.VSpan) {
                SwapUv();
            }
        }

        public float WorldSpaceWidth {
            get {
                CalculateInitialUv();
                return ProjectedBounds.USpan;
            }
        }

        public float WorldSpaceHeight {
            get {
                CalculateInitialUv();
                return ProjectedBounds.VSpan;
            }
        }

        public float UvSpaceWidth => WorldSpaceWidth / LightmapConfig.DownscaleFactor;
        public float UvSpaceHeight => WorldSpaceHeight / LightmapConfig.DownscaleFactor;

        public void SwapUv() {
            void swap<T>(ref T a, ref T b)
                => (a, b) = (b, a);
            
            swap(ref ProjectedBounds.Max.U, ref ProjectedBounds.Max.V);
            swap(ref ProjectedBounds.Min.U, ref ProjectedBounds.Min.V);
            swap(ref UvProjectionAxes.UAxis, ref UvProjectionAxes.VAxis);
        }

        public static LightmapGroup? FindCoplanar(IReadOnlyList<LightmapGroup> lmGroups, LMFace otherFace) {
            foreach (LightmapGroup group in lmGroups) {
                if ((group.Plane.Normal - otherFace.Plane.Normal).LengthSquared() < 0.01f) {
                    PlaneF plane2 = new PlaneF(otherFace.Plane.Normal, otherFace.Vertices[0].Location);
                    if (Math.Abs(plane2.EvalAtPoint((group.Plane.PointOnPlane))) > 4.0f) { continue; }
                    BoxF faceBox = otherFace.PaddedBoundingBox();
                    if (faceBox.IntersectsWith(group.BoundingBox)) { return group; }
                }
            }
            return null;
        }

        public IEnumerable<ObjectRenderer.BrushVertex> GenQuadVerts() {
            var uAxis = UvProjectionAxes.UAxis;
            var vAxis = UvProjectionAxes.VAxis;

            var minPosition = TopLeftWorldPos;
            
            ObjectRenderer.BrushVertex genVert(float u, float v)
                => new ObjectRenderer.BrushVertex(
                    position: (minPosition + uAxis * u + vAxis * v).ToXna(),
                    normal: Plane.Normal.ToXna(),
                    diffUv: Vector2.Zero,
                    lmUv: new Vector2(
                        MathF.Floor(u / LightmapConfig.DownscaleFactor + StartWriteUV.U) / LightmapConfig.TextureDims,
                        MathF.Floor(v / LightmapConfig.DownscaleFactor + StartWriteUV.V) / LightmapConfig.TextureDims),
                    color: Color.White,
                    selected: false);

            yield return genVert(0.0f, 0.0f);
            yield return genVert(0.0f, WorldSpaceHeight);
            yield return genVert(WorldSpaceWidth, 0.0f);
            yield return genVert(WorldSpaceWidth, WorldSpaceHeight);
        }
    }
}
