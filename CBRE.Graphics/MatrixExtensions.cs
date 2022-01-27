using System;
using System.Collections.Generic;
using System.Text;
using CBRE.DataStructures.Geometric;

namespace CBRE.Graphics {
    public static class MatrixExtensions {
        public static Matrix ToCbre(this Microsoft.Xna.Framework.Matrix m) {
            return new Matrix(
                (decimal)m[0], (decimal)m[1], (decimal)m[2], (decimal)m[3],
                (decimal)m[4], (decimal)m[5], (decimal)m[6], (decimal)m[7],
                (decimal)m[8], (decimal)m[9], (decimal)m[10], (decimal)m[11],
                (decimal)m[12], (decimal)m[13], (decimal)m[14], (decimal)m[15]);
        }

        public static Microsoft.Xna.Framework.Matrix ToXna(this Matrix m) {
            return new Microsoft.Xna.Framework.Matrix(
                (float)m[0], (float)m[4], (float)m[8], (float)m[12],
                (float)m[1], (float)m[5], (float)m[9], (float)m[13],
                (float)m[2], (float)m[6], (float)m[10], (float)m[14],
                (float)m[3], (float)m[7], (float)m[11], (float)m[15]);
        }

        public static Microsoft.Xna.Framework.Matrix ToXna(this MatrixF m) {
            return new Microsoft.Xna.Framework.Matrix(
                (float)m[0], (float)m[4], (float)m[8], (float)m[12],
                (float)m[1], (float)m[5], (float)m[9], (float)m[13],
                (float)m[2], (float)m[6], (float)m[10], (float)m[14],
                (float)m[3], (float)m[7], (float)m[11], (float)m[15]);
        }

        public static Microsoft.Xna.Framework.Vector3 ToXna(this Vector3 vector) {
            return new Microsoft.Xna.Framework.Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
        }

        public static Microsoft.Xna.Framework.Vector3 ToXna(this Vector3F vector) {
            return new Microsoft.Xna.Framework.Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
        }
        
        public static Vector3 ToCbre(this Microsoft.Xna.Framework.Vector3 vector) {
            return new Vector3((decimal)vector.X, (decimal)vector.Y, (decimal)vector.Z);
        }
    }
}
