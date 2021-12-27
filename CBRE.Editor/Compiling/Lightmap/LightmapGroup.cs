#nullable enable
using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Compiling.Lightmap {
    public class LightmapGroup {
        public PlaneF? Plane { get; private set; }
        public BoxF? BoundingBox { get; private set; }

        private readonly HashSet<LMFace> faces;
        public IEnumerable<LMFace> Faces => faces;

        public Vector3F? UAxis;
        public Vector3F? VAxis;
        public float? MinTotalU;
        public float? MinTotalV;
        public float? MaxTotalU;
        public float? MaxTotalV;
        public int WriteU;
        public int WriteV;

        public LightmapGroup() {
            faces = new HashSet<LMFace>();
        }

        public void AddFace(LMFace face) {
            faces.Add(face);
            BoxF faceBox = face.PaddedBoundingBox();
            BoundingBox ??= faceBox;
            BoundingBox = new BoxF(new[] {faceBox, BoundingBox});
            var newPlane = new PlaneF(face.Normal, face.Vertices[0].Location);
            Plane ??= newPlane;
            Plane = new PlaneF(Plane.Normal, (newPlane.PointOnPlane + Plane.PointOnPlane) / 2.0f);
        }

        private void CalculateInitialUV() {
            if (UAxis == null || VAxis == null) {
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
            }

            if (MinTotalU == null || MinTotalV == null || MaxTotalU == null || MaxTotalV == null) {
                foreach (LMFace face in Faces) {
                    foreach (Vector3F coord in face.Vertices.Select(x => x.Location)) {
                        float x = coord.Dot(UAxis);
                        float y = coord.Dot(VAxis);

                        if (MinTotalU == null || x < MinTotalU) { MinTotalU = x; }
                        if (MinTotalV == null || y < MinTotalV) { MinTotalV = y; }
                        if (MaxTotalU == null || x > MaxTotalU) { MaxTotalU = x; }
                        if (MaxTotalV == null || y > MaxTotalV) { MaxTotalV = y; }
                    }
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
                CalculateInitialUV();
                return (MaxTotalU - MinTotalU).Value;
            }
        }

        public float Height {
            get {
                CalculateInitialUV();
                return (MaxTotalV - MinTotalV).Value;
            }
        }

        public void SwapUv() {
            (MaxTotalU, MaxTotalV) = (MaxTotalV, MaxTotalU);
            (MinTotalU, MinTotalV) = (MinTotalV, MinTotalU);
            (UAxis, VAxis) = (VAxis, UAxis);
        }

        public static LightmapGroup? FindCoplanar(IEnumerable<LightmapGroup> lmGroups, LMFace otherFace) {
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
