using CBRE.Extensions;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using static CBRE.Common.PrimitiveConversion;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public struct Vector3 : ISerializable {
        public readonly static Vector3 MaxValue = new Vector3(Decimal.MaxValue, Decimal.MaxValue, Decimal.MaxValue);
        public readonly static Vector3 MinValue = new Vector3(Decimal.MinValue, Decimal.MinValue, Decimal.MinValue);
        public readonly static Vector3 Zero = new Vector3(0, 0, 0);
        public readonly static Vector3 One = new Vector3(1, 1, 1);
        public readonly static Vector3 UnitX = new Vector3(1, 0, 0);
        public readonly static Vector3 UnitY = new Vector3(0, 1, 0);
        public readonly static Vector3 UnitZ = new Vector3(0, 0, 1);

        #region X, Y, Z

        public decimal X;
        public decimal Y;
        public decimal Z;

        public double DX {
            get { return (double)X; }
            set { X = (decimal)value; }
        }

        public double DY {
            get { return (double)Y; }
            set { Y = (decimal)value; }
        }

        public double DZ {
            get { return (double)Z; }
            set { Z = (decimal)value; }
        }
        #endregion

        public Vector3(decimal x, decimal y, decimal z) {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector3F other) {
            X = (decimal)other.X;
            Y = (decimal)other.Y;
            Z = (decimal)other.Z;
        }

        internal Vector3(SerializationInfo info, StreamingContext context) {
            X = info.GetDecimal("X");
            Y = info.GetDecimal("Y");
            Z = info.GetDecimal("Z");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("X", X);
            info.AddValue("Y", Y);
            info.AddValue("Z", Z);
        }

        public bool EquivalentTo(Vector3 test, decimal delta = 0.0001m) {
            var xd = Math.Abs(X - test.X);
            var yd = Math.Abs(Y - test.Y);
            var zd = Math.Abs(Z - test.Z);
            return (xd < delta) && (yd < delta) && (zd < delta);
        }

        public bool Equals(Vector3 other) {
            return EquivalentTo(other);
        }

        public override bool Equals(object obj) {
            return obj is Vector3 otherVec && Equals(otherVec);
        }

        public override int GetHashCode() {
            unchecked {
                var result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Z.GetHashCode();
                return result;
            }
        }

        public decimal Dot(Vector3 c) {
            return ((X * c.X) + (Y * c.Y) + (Z * c.Z));
        }

        public Vector3 Cross(Vector3 that) {
            var xv = (Y * that.Z) - (Z * that.Y);
            var yv = (Z * that.X) - (X * that.Z);
            var zv = (X * that.Y) - (Y * that.X);
            return new Vector3(xv, yv, zv);
        }

        public Vector3 Round(int num = 8) {
            return new Vector3(Math.Round(X, num), Math.Round(Y, num), Math.Round(Z, num));
        }

        public Vector3 Snap(decimal snapTo) {
            return new Vector3(
                Math.Round(X / snapTo) * snapTo,
                Math.Round(Y / snapTo) * snapTo,
                Math.Round(Z / snapTo) * snapTo
            );
        }

        public decimal VectorMagnitude() {
            return (decimal)Math.Sqrt(Math.Pow(DX, 2) + Math.Pow(DY, 2) + Math.Pow(DZ, 2));
        }

        public decimal LengthSquared() {
            return (decimal)(Math.Pow(DX, 2) + Math.Pow(DY, 2) + Math.Pow(DZ, 2));
        }

        public Vector3 Normalise() {
            var len = VectorMagnitude();
            return len == 0 ? new Vector3(0, 0, 0) : new Vector3(X / len, Y / len, Z / len);
        }

        public Vector3 Absolute() {
            return new Vector3(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        public Vector3 XYZ() {
            return new Vector3(X, Y, Z);
        }

        public Vector3 ZXY() {
            return new Vector3(Z, X, Y);
        }

        public Vector3 YZX() {
            return new Vector3(Y, Z, X);
        }

        public Vector3 ZYX() {
            return new Vector3(Z, Y, X);
        }

        public Vector3 YXZ() {
            return new Vector3(Y, X, Z);
        }

        public Vector3 XZY() {
            return new Vector3(X, Z, Y);
        }


        public static bool operator ==(Vector3 c1, Vector3 c2) {
            return Equals(c1, null) ? Equals(c2, null) : c1.Equals(c2);
        }

        public static bool operator !=(Vector3 c1, Vector3 c2) {
            return Equals(c1, null) ? !Equals(c2, null) : !c1.Equals(c2);
        }

        public static Vector3 operator +(Vector3 c1, Vector3 c2) {
            return new Vector3(c1.X + c2.X, c1.Y + c2.Y, c1.Z + c2.Z);
        }

        public static Vector3 operator -(Vector3 c1, Vector3 c2) {
            return new Vector3(c1.X - c2.X, c1.Y - c2.Y, c1.Z - c2.Z);
        }

        public static Vector3 operator -(Vector3 c1) {
            return new Vector3(-c1.X, -c1.Y, -c1.Z);
        }

        public static Vector3 operator /(Vector3 c, decimal f) {
            return f == 0 ? new Vector3(0, 0, 0) : new Vector3(c.X / f, c.Y / f, c.Z / f);
        }

        public static Vector3 operator *(Vector3 c, decimal f) {
            return new Vector3(c.X * f, c.Y * f, c.Z * f);
        }

        public static Vector3 operator *(Vector3 c, double f) {
            return c * (decimal)f;
        }

        public static Vector3 operator *(Vector3 c, int i) {
            return c * (decimal)i;
        }

        public static Vector3 operator *(decimal f, Vector3 c) {
            return c * f;
        }

        public static Vector3 operator *(double f, Vector3 c) {
            return c * (decimal)f;
        }

        public Vector3 ComponentMultiply(Vector3 c) {
            return new Vector3(X * c.X, Y * c.Y, Z * c.Z);
        }

        public Vector3 ComponentDivide(Vector3 c) {
            var x = c.X == 0 ? 1 : c.X;
            var y = c.Y == 0 ? 1 : c.Y;
            var z = c.Z == 0 ? 1 : c.Z;
            return new Vector3(X / x, Y / y, Z / z);
        }

        /// <summary>
        /// Treats this vector as a directional unit vector and constructs a euler angle representation of that angle (in radians)
        /// </summary>
        /// <returns></returns>
        public Vector3 ToEulerAngles() {
            // http://www.gamedev.net/topic/399701-convert-vector-to-euler-cardan-angles/#entry3651854
            var yaw = DMath.Atan2(Y, X);
            var pitch = DMath.Atan2(-Z, DMath.Sqrt(X * X + Y * Y));
            return new Vector3(0, pitch, yaw); // HL FGD has X = roll, Y = pitch, Z = yaw
        }

        public override string ToString() {
            return "(" + X.ToString("0.0000") + " " + Y.ToString("0.0000") + " " + Z.ToString("0.0000") + ")";
        }

        public string ToDataString() {
            Func<decimal, string> toStringNoTrailing = (v) => {
                v = Math.Round(v, 5);
                string retVal = v.ToString("F7");
                while (retVal.Contains('.') && (retVal.Last() == '0' || retVal.Last() == '.')) {
                    retVal = retVal.Substring(0, retVal.Length - 1);
                }
                return retVal;
            };
            return toStringNoTrailing(X) + " " + toStringNoTrailing(Y) + " " + toStringNoTrailing(Z);
        }

        public Vector3 Clone() {
            return new Vector3(X, Y, Z);
        }

        public static Vector3 Parse(string x, string y, string z) {
            const NumberStyles ns = NumberStyles.Float;
            return new Vector3(ParseDecimal(x), ParseDecimal(y), ParseDecimal(z));
        }

        public Vector3F ToVector3F() {
            return new Vector3F((float)X, (float)Y, (float)Z);
        }
    }


}
