#nullable enable
using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public Vector3F? UAxis;
        public Vector3F? VAxis;
        public float? MinTotalU;
        public float? MinTotalV;
        public float? MaxTotalU;
        public float? MaxTotalV;
        public int WriteU;
        public int WriteV;

        public LightmapGroup() {
            faces = new List<LMFace>();
        }

        public void AddFace(LMFace face) {
            faces.Add(face);
            BoxF faceBox = face.PaddedBoundingBox();
            BoundingBox ??= faceBox;
            BoundingBox = new BoxF(new[] {faceBox, BoundingBox});
            var newPlane = new PlaneF(face.Normal, face.Vertices[0].Location);
            Plane ??= newPlane;
            var otherPointOnPlane = Plane.Project(newPlane.PointOnPlane);
            Plane = new PlaneF(Plane.Normal, (otherPointOnPlane + Plane.PointOnPlane) / 2.0f);
        }

        private void CalculateInitialUv() {
            if (UAxis != null
                && VAxis != null
                && MinTotalU != null
                && MinTotalV != null
                && MaxTotalU != null
                && MaxTotalV != null) {
                return;
            }
            
            var direction = Plane.GetClosestAxisToNormal();
            var tempV = direction == Vector3F.UnitZ ? -Vector3F.UnitY : -Vector3F.UnitZ;
            UAxis = Plane.Normal.Cross(tempV).Normalise();
            VAxis = UAxis.Cross(Plane.Normal).Normalise();

            if (Plane.OnPlane(Plane.PointOnPlane + UAxis * 1000f) != 0) {
                throw new Exception("uAxis is misaligned");
            }
            if (Plane.OnPlane(Plane.PointOnPlane + VAxis * 1000f) != 0) {
                throw new Exception("vAxis is misaligned");
            }

            foreach (LMFace face in Faces) {
                foreach (Vector3F coord in face.Vertices.Select(x => x.Location)) {
                    float u = coord.Dot(UAxis);
                    float v = coord.Dot(VAxis);

                    if (MinTotalU == null || u < MinTotalU) { MinTotalU = u; }
                    if (MinTotalV == null || v < MinTotalV) { MinTotalV = v; }
                    if (MaxTotalU == null || u > MaxTotalU) { MaxTotalU = u; }
                    if (MaxTotalV == null || v > MaxTotalV) { MaxTotalV = v; }
                }
            }

            if (MinTotalU == null || MinTotalV == null || MaxTotalU == null || MaxTotalV == null) {
                throw new Exception("Could not determine face minimum and maximum UVs");
            }

            MinTotalU -= LightmapConfig.DownscaleFactor; MinTotalV -= LightmapConfig.DownscaleFactor;
            MaxTotalU += LightmapConfig.DownscaleFactor; MaxTotalV += LightmapConfig.DownscaleFactor;

            void roundValue(ref float? v)
                => v = (float)Math.Ceiling(v.Value / LightmapConfig.DownscaleFactor) * LightmapConfig.DownscaleFactor;
            
            roundValue(ref MinTotalU);
            roundValue(ref MinTotalV);
            roundValue(ref MaxTotalU);
            roundValue(ref MaxTotalV);

            if ((MaxTotalU - MinTotalU) < (MaxTotalV - MinTotalV)) {
                SwapUv();
            }
        }

        public float Width {
            get {
                CalculateInitialUv();
                return (MaxTotalU - MinTotalU).Value;
            }
        }

        public float Height {
            get {
                CalculateInitialUv();
                return (MaxTotalV - MinTotalV).Value;
            }
        }

        public void SwapUv() {
            void swap<T>(ref T a, ref T b)
                => (a, b) = (b, a);
            
            swap(ref MaxTotalU, ref MaxTotalV);
            swap(ref MinTotalU, ref MinTotalV);
            swap(ref UAxis, ref VAxis);
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
    }
}
