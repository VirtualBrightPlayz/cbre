using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public class Vector : Vector3 {
        public Vector3 Normal { get; set; }
        public decimal Distance { get; set; }

        public Vector(Vector3 normal, decimal distance)
            : base(0, 0, 0) {
            Normal = normal.Normalise();
            Distance = distance;
            var temp = Normal * Distance;
            X = temp.X;
            Y = temp.Y;
            Z = temp.Z;
        }

        public Vector(Vector3 offsets)
            : base(0, 0, 0) {
            Set(offsets);
        }

        protected Vector(SerializationInfo info, StreamingContext context) : base(info, context) {
            Normal = (Vector3)info.GetValue("Normal", typeof(Vector3));
            Distance = info.GetDecimal("Distance");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("Normal", Normal);
            info.AddValue("Distance", Distance);
        }

        public void SetToZero() {
            X = Y = Z = Distance = 0;
        }

        public void Set(Vector3 offsets) {
            Distance = offsets.VectorMagnitude();
            if (Distance == 0) {
                X = Y = Z = 0;
            } else {
                X = offsets.X / Distance;
                Y = offsets.Y / Distance;
                Z = offsets.Z / Distance;
            }
        }
    }
}
