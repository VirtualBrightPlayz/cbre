using CBRE.DataStructures.Geometric;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Transformations {
    [Serializable]
    public class UnitMatrixMult : IUnitTransformation {
        public Matrix Matrix { get; set; }

        public UnitMatrixMult(decimal[] matrix) {
            Matrix = new Matrix(matrix);
        }

        public UnitMatrixMult(Matrix matrix) {
            Matrix = matrix;
        }

        protected UnitMatrixMult(SerializationInfo info, StreamingContext context) {
            Matrix = (Matrix)info.GetValue("Matrix", typeof(Matrix));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Matrix", Matrix);
        }

        public Vector3 Transform(Vector3 c) {
            return Transform(c, 1);
        }

        public Vector3 Transform(Vector3 c, decimal w) {
            var x = Matrix[0] * c.X + Matrix[1] * c.Y + Matrix[2] * c.Z + Matrix[3] * w;
            var y = Matrix[4] * c.X + Matrix[5] * c.Y + Matrix[6] * c.Z + Matrix[7] * w;
            var z = Matrix[8] * c.X + Matrix[9] * c.Y + Matrix[10] * c.Z + Matrix[11] * w;
            return new Vector3(x, y, z);
        }

        public Vector3F Transform(Vector3F c) {
            var x = (float)Matrix[0] * c.X + (float)Matrix[1] * c.Y + (float)Matrix[2] * c.Z + (float)Matrix[3];
            var y = (float)Matrix[4] * c.X + (float)Matrix[5] * c.Y + (float)Matrix[6] * c.Z + (float)Matrix[7];
            var z = (float)Matrix[8] * c.X + (float)Matrix[9] * c.Y + (float)Matrix[10] * c.Z + (float)Matrix[11];
            return new Vector3F(x, y, z);
        }
    }
}
