using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public struct Vector {
        public Vector3 Normal { get; set; }
        public decimal Distance { get; set; }

        public Vector(Vector3 normal, decimal distance) {
            Normal = normal.Normalise();
            Distance = distance;
        }

        public Vector(Vector3 offsets) {
            Distance = offsets.VectorMagnitude();
            if (Distance == 0) {
                Normal = Vector3.Zero;
            } else {
                Normal = new Vector3(
                    offsets.X / Distance,
                    offsets.Y / Distance,
                    offsets.Z / Distance);
            }
        }

        internal Vector(SerializationInfo info, StreamingContext context) {
            Normal = (Vector3)info.GetValue("Normal", typeof(Vector3));
            Distance = info.GetDecimal("Distance");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Normal", Normal);
            info.AddValue("Distance", Distance);
        }

        public void SetToZero() {
            Distance = 0;
            Normal = Vector3.Zero;
        }

        public void Set(Vector3 offsets) {
            Vector newVec = new Vector(offsets);
            Normal = newVec.Normal;
            Distance = newVec.Distance;
        }

        public static implicit operator Vector3(Vector vec) => vec.Normal * vec.Distance;
    }
}
