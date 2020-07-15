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

        public Coordinate Transform(Coordinate c) {
            return Transform(c, 1);
        }

        public Coordinate Transform(Coordinate c, decimal w) {
            var x = Matrix[0] * c.X + Matrix[1] * c.Y + Matrix[2] * c.Z + Matrix[3] * w;
            var y = Matrix[4] * c.X + Matrix[5] * c.Y + Matrix[6] * c.Z + Matrix[7] * w;
            var z = Matrix[8] * c.X + Matrix[9] * c.Y + Matrix[10] * c.Z + Matrix[11] * w;
            return new Coordinate(x, y, z);
        }

        public CoordinateF Transform(CoordinateF c) {
            var x = (float)Matrix[0] * c.X + (float)Matrix[1] * c.Y + (float)Matrix[2] * c.Z + (float)Matrix[3];
            var y = (float)Matrix[4] * c.X + (float)Matrix[5] * c.Y + (float)Matrix[6] * c.Z + (float)Matrix[7];
            var z = (float)Matrix[8] * c.X + (float)Matrix[9] * c.Y + (float)Matrix[10] * c.Z + (float)Matrix[11];
            return new CoordinateF(x, y, z);
        }
    }
}
