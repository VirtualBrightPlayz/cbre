using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public struct Vector3F : ISerializable {
        public readonly static Vector3F MaxValue = new Vector3F(float.MaxValue, float.MaxValue, float.MaxValue);
        public readonly static Vector3F MinValue = new Vector3F(float.MinValue, float.MinValue, float.MinValue);
        public readonly static Vector3F Zero = new Vector3F(0, 0, 0);
        public readonly static Vector3F One = new Vector3F(1, 1, 1);
        public readonly static Vector3F UnitX = new Vector3F(1, 0, 0);
        public readonly static Vector3F UnitY = new Vector3F(0, 1, 0);
        public readonly static Vector3F UnitZ = new Vector3F(0, 0, 1);

        public float X;
        public float Y;
        public float Z;

        public Vector3F(Vector3 c) {
            X = (float)c.X;
            Y = (float)c.Y;
            Z = (float)c.Z;
        }

        public Vector3F(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        internal Vector3F(SerializationInfo info, StreamingContext context) {
            X = info.GetSingle("X");
            Y = info.GetSingle("Y");
            Z = info.GetSingle("Z");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("X", X);
            info.AddValue("Y", Y);
            info.AddValue("Z", Z);
        }

        public bool EquivalentTo(Vector3F test, float delta = 0.0001f) {
            var xd = Math.Abs(X - test.X);
            var yd = Math.Abs(Y - test.Y);
            var zd = Math.Abs(Z - test.Z);
            return (xd < delta) && (yd < delta) && (zd < delta);
        }

        public bool Equals(Vector3F other) {
            return EquivalentTo(other);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) { return false; }
            return obj is Vector3F otherVec && Equals(otherVec);
        }

        public override int GetHashCode() {
            unchecked {
                var result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Z.GetHashCode();
                return result;
            }
        }

        public float Dot(Vector3F c) {
            return ((X * c.X) + (Y * c.Y) + (Z * c.Z));
        }

        public Vector3F Cross(Vector3F that) {
            var xv = (Y * that.Z) - (Z * that.Y);
            var yv = (Z * that.X) - (X * that.Z);
            var zv = (X * that.Y) - (Y * that.X);
            return new Vector3F(xv, yv, zv);
        }

        public Vector3F Round(int num = 4) {
            return new Vector3F(
                (float)Math.Round(X, num),
                (float)Math.Round(Y, num),
                (float)Math.Round(Z, num));
        }

        public Vector3F Snap(float snapTo) {
            return new Vector3F(
                (float)Math.Round(X / snapTo) * snapTo,
                (float)Math.Round(Y / snapTo) * snapTo,
                (float)Math.Round(Z / snapTo) * snapTo);
        }

        public float LengthSquared() {
            return (float)(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
        }

        public float VectorMagnitude() {
            return (float)Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
        }

        public Vector3F Normalise() {
            var len = VectorMagnitude();
            return Math.Abs(len - 0) < 0.0001 ? new Vector3F(0, 0, 0) : new Vector3F(X / len, Y / len, Z / len);
        }

        public Vector3F Absolute() {
            return new Vector3F(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        public static bool operator ==(Vector3F c1, Vector3F c2) {
            return c1.Equals(c2);
        }

        public static bool operator !=(Vector3F c1, Vector3F c2) {
            return !c1.Equals(c2);
        }

        public static Vector3F operator +(Vector3F c1, Vector3F c2) {
            return new Vector3F(c1.X + c2.X, c1.Y + c2.Y, c1.Z + c2.Z);
        }

        public static Vector3F operator -(Vector3F c1, Vector3F c2) {
            return new Vector3F(c1.X - c2.X, c1.Y - c2.Y, c1.Z - c2.Z);
        }

        public static Vector3F operator -(Vector3F c1) {
            return new Vector3F(-c1.X, -c1.Y, -c1.Z);
        }

        public static Vector3F operator /(Vector3F c, float f) {
            return Math.Abs(f - 0) < 0.0001f ? new Vector3F(0, 0, 0) : new Vector3F(c.X / f, c.Y / f, c.Z / f);
        }

        public static Vector3F operator *(Vector3F c, float f) {
            return new Vector3F(c.X * f, c.Y * f, c.Z * f);
        }

        public static Vector3F operator *(float f, Vector3F c) {
            return c * f;
        }

        public Vector3F ComponentMultiply(Vector3F c) {
            return new Vector3F(X * c.X, Y * c.Y, Z * c.Z);
        }

        public Vector3F ComponentDivide(Vector3F c) {
            if (Math.Abs(c.X - 0) < 0.0001) c.X = 1;
            if (Math.Abs(c.Y - 0) < 0.0001) c.Y = 1;
            if (Math.Abs(c.Z - 0) < 0.0001) c.Z = 1;
            return new Vector3F(X / c.X, Y / c.Y, Z / c.Z);
        }

        public override string ToString() {
            return "(" + X.ToString("0.0000") + " " + Y.ToString("0.0000") + " " + Z.ToString("0.0000") + ")";
        }

        [Obsolete("Redundant because Vector3F is a value type")]
        public Vector3F Clone() {
            return this;
        }

        public static Vector3F Parse(string x, string y, string z) {
            return new Vector3F(float.Parse(x), float.Parse(y), float.Parse(z));
        }

        public float DistanceFrom(Vector3F other)
            => (this - other).VectorMagnitude();
        
        public static Vector3F Lerp(Vector3F from, Vector3F to, float amount)
            => from * (1.0f - amount) + to * amount;
    }
}
