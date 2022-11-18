using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Compiling.Lightmap.Legacy {
    public class LightmapGroup {
        public PlaneF Plane;
        public BoxF BoundingBox;
        public List<LMFace> Faces;

        public Vector3F? uAxis;
        public Vector3F? vAxis;
        public float? minTotalX;
        public float? minTotalY;
        public float? maxTotalX;
        public float? maxTotalY;
        public int writeX;
        public int writeY;

        private void CalculateInitialUV() {
            if (uAxis == null || vAxis == null) {
                var direction = Plane.GetClosestAxisToNormal();
                var tempV = direction == Vector3F.UnitZ ? -Vector3F.UnitY : -Vector3F.UnitZ;
                uAxis = Plane.Normal.Cross(tempV).Normalise();
                vAxis = uAxis.Value.Cross(Plane.Normal).Normalise();

                if (Plane.OnPlane(Plane.PointOnPlane + uAxis.Value * 1000f) != 0) {
                    throw new Exception("uAxis is misaligned");
                }
                if (Plane.OnPlane(Plane.PointOnPlane + vAxis.Value * 1000f) != 0) {
                    throw new Exception("vAxis is misaligned");
                }
            }

            if (minTotalX == null || minTotalY == null || maxTotalX == null || maxTotalY == null) {
                foreach (LMFace face in Faces) {
                    foreach (Vector3F coord in face.Vertices.Select(x => x.Location)) {
                        float x = coord.Dot(uAxis.Value);
                        float y = coord.Dot(vAxis.Value);

                        if (minTotalX == null || x < minTotalX) minTotalX = x;
                        if (minTotalY == null || y < minTotalY) minTotalY = y;
                        if (maxTotalX == null || x > maxTotalX) maxTotalX = x;
                        if (maxTotalY == null || y > maxTotalY) maxTotalY = y;
                    }
                }

                minTotalX -= LightmapConfig.DownscaleFactor; minTotalY -= LightmapConfig.DownscaleFactor;
                maxTotalX += LightmapConfig.DownscaleFactor; maxTotalY += LightmapConfig.DownscaleFactor;

                minTotalX /= LightmapConfig.DownscaleFactor; minTotalX = (float)Math.Ceiling(minTotalX.Value); minTotalX *= LightmapConfig.DownscaleFactor;
                minTotalY /= LightmapConfig.DownscaleFactor; minTotalY = (float)Math.Ceiling(minTotalY.Value); minTotalY *= LightmapConfig.DownscaleFactor;
                maxTotalX /= LightmapConfig.DownscaleFactor; maxTotalX = (float)Math.Ceiling(maxTotalX.Value); maxTotalX *= LightmapConfig.DownscaleFactor;
                maxTotalY /= LightmapConfig.DownscaleFactor; maxTotalY = (float)Math.Ceiling(maxTotalY.Value); maxTotalY *= LightmapConfig.DownscaleFactor;

                if ((maxTotalX - minTotalX) < (maxTotalY - minTotalY)) {
                    SwapUV();
                }
            }
        }

        public float Width {
            get {
                CalculateInitialUV();
                return (maxTotalX - minTotalX).Value;
            }
        }

        public float Height {
            get {
                CalculateInitialUV();
                return (maxTotalY - minTotalY).Value;
            }
        }

        public void SwapUV() {
            float maxSwap = maxTotalX.Value; float minSwap = minTotalX.Value;
            maxTotalX = maxTotalY; minTotalX = minTotalY;
            maxTotalY = maxSwap; minTotalY = minSwap;

            Vector3F swapAxis = uAxis.Value;
            uAxis = vAxis;
            vAxis = swapAxis;
        }

        public static LightmapGroup FindCoplanar(List<LightmapGroup> lmGroups, LMFace otherFace) {
            foreach (LightmapGroup group in lmGroups) {
                if ((group.Plane.Normal - otherFace.Plane.Normal).LengthSquared() < 0.01f) {
                    PlaneF plane2 = new PlaneF(otherFace.Plane.Normal, otherFace.Vertices[0].Location);
                    if (Math.Abs(plane2.EvalAtPoint((group.Plane.PointOnPlane))) > 4.0f) continue;
                    BoxF faceBox = new BoxF(otherFace.BoundingBox.Start - new Vector3F(3.0f, 3.0f, 3.0f), otherFace.BoundingBox.End + new Vector3F(3.0f, 3.0f, 3.0f));
                    if (faceBox.IntersectsWith(group.BoundingBox)) return group;
                }
            }
            return null;
        }
    }
}
