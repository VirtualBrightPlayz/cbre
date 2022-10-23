using System;
using System.Collections.Generic;
using System.Text;
using CBRE.DataStructures.Geometric;

namespace CBRE.Graphics {
    public static class MatrixExtensions {
        public static Matrix ToCbre(this System.Numerics.Matrix4x4 m) {
            return new Matrix(
                (decimal)m.M11, (decimal)m.M12, (decimal)m.M13, (decimal)m.M14,
                (decimal)m.M21, (decimal)m.M22, (decimal)m.M23, (decimal)m.M24,
                (decimal)m.M31, (decimal)m.M32, (decimal)m.M33, (decimal)m.M34,
                (decimal)m.M41, (decimal)m.M42, (decimal)m.M43, (decimal)m.M44
            );
            /*
            return new Matrix(
                (decimal)m[0], (decimal)m[1], (decimal)m[2], (decimal)m[3],
                (decimal)m[4], (decimal)m[5], (decimal)m[6], (decimal)m[7],
                (decimal)m[8], (decimal)m[9], (decimal)m[10], (decimal)m[11],
                (decimal)m[12], (decimal)m[13], (decimal)m[14], (decimal)m[15]);
            */
        }

        public static System.Numerics.Matrix4x4 ToXna(this Matrix m) {
            return new System.Numerics.Matrix4x4(
                (float)m[0], (float)m[4], (float)m[8], (float)m[12],
                (float)m[1], (float)m[5], (float)m[9], (float)m[13],
                (float)m[2], (float)m[6], (float)m[10], (float)m[14],
                (float)m[3], (float)m[7], (float)m[11], (float)m[15]);
        }

        public static System.Numerics.Matrix4x4 ToXna(this MatrixF m) {
            return new System.Numerics.Matrix4x4(
                (float)m[0], (float)m[4], (float)m[8], (float)m[12],
                (float)m[1], (float)m[5], (float)m[9], (float)m[13],
                (float)m[2], (float)m[6], (float)m[10], (float)m[14],
                (float)m[3], (float)m[7], (float)m[11], (float)m[15]);
        }

        public static System.Numerics.Vector3 ToXna(this Vector3 vector) {
            return new System.Numerics.Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
        }

        public static System.Numerics.Vector3 ToXna(this Vector3F vector) {
            return new System.Numerics.Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
        }

        public static Vector3 ToCbre(this System.Numerics.Vector3 vector) {
            return new Vector3((decimal)vector.X, (decimal)vector.Y, (decimal)vector.Z);
        }

        public static Vector3F ToCbreF(this System.Numerics.Vector3 vector) {
            return new Vector3F(vector.X, vector.Y, vector.Z);
        }
    }
}
