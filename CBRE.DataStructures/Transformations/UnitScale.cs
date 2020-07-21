using CBRE.DataStructures.Geometric;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Transformations {
    [Serializable]
    public class UnitScale : IUnitTransformation {
        public Vector3 Scalar { get; set; }
        public Vector3 Origin { get; set; }

        public UnitScale(Vector3 scalar, Vector3 origin) {
            Scalar = scalar;
            Origin = origin;
        }

        protected UnitScale(SerializationInfo info, StreamingContext context) {
            Scalar = (Vector3)info.GetValue("Scalar", typeof(Vector3));
            Origin = (Vector3)info.GetValue("Origin", typeof(Vector3));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Scalar", Scalar);
            info.AddValue("Origin", Origin);
        }

        public Vector3 Transform(Vector3 c) {
            return (c - Origin).ComponentMultiply(Scalar) + Origin;
        }

        public Vector3F Transform(Vector3F c) {
            return (c - new Vector3F(Origin)).ComponentMultiply(new Vector3F(Scalar)) + new Vector3F(Origin);
        }
    }
}
