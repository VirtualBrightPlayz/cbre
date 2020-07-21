using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    /// <summary>
    /// A cloud is a wrapper around a collection of points, allowing
    /// various useful operations to be performed on them.
    /// </summary>
    [Serializable]
    public class Cloud : ISerializable {
        public List<Vector3> Points { get; private set; }
        public Box BoundingBox { get; private set; }

        public Vector3 MinX { get; private set; }
        public Vector3 MinY { get; private set; }
        public Vector3 MinZ { get; private set; }
        public Vector3 MaxX { get; private set; }
        public Vector3 MaxY { get; private set; }
        public Vector3 MaxZ { get; private set; }

        public Cloud(IEnumerable<Vector3> points) {
            Points = new List<Vector3>(points);
            BoundingBox = new Box(points);
            MinX = MinY = MinZ = MaxX = MaxY = MaxZ = null;
            foreach (var p in points) {
                if (MinX == null || p.X < MinX.X) MinX = p;
                if (MinY == null || p.Y < MinY.Y) MinY = p;
                if (MinZ == null || p.Z < MinZ.Z) MinZ = p;
                if (MaxX == null || p.X > MaxX.X) MaxX = p;
                if (MaxY == null || p.Y > MaxY.Y) MaxY = p;
                if (MaxZ == null || p.Z > MaxZ.Z) MaxZ = p;
            }
        }

        protected Cloud(SerializationInfo info, StreamingContext context) : this((Vector3[])info.GetValue("Points", typeof(Vector3[]))) {

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Points", Points.ToArray());
        }

        /// <summary>
        /// Get a list of the 6 points that define the outermost extents of this cloud.
        /// </summary>
        /// <returns>A list of the 6 (Min|Max)(X|Y|Z) values of this cloud.</returns>
        public IEnumerable<Vector3> GetExtents() {
            return new[]
                       {
                           MinX, MinY, MinZ,
                           MaxX, MaxY, MaxZ
                        };
        }
    }
}
