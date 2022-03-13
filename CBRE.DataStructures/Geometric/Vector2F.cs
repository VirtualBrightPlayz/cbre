using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public struct Vector2F : ISerializable {
        public readonly static Vector2F MaxValue = new Vector2F(float.MaxValue, float.MaxValue);
        public readonly static Vector2F MinValue = new Vector2F(float.MinValue, float.MinValue);
        public readonly static Vector2F Zero = new Vector2F(0, 0);
        public readonly static Vector2F One = new Vector2F(1, 1);
        public readonly static Vector2F UnitX = new Vector2F(1, 0);
        public readonly static Vector2F UnitY = new Vector2F(0, 1);

        public float X;
        public float Y;

        public Vector2F(Vector2d c) {
            X = (float)c.X;
            Y = (float)c.Y;
        }

        public Vector2F(float x, float y) {
            X = x;
            Y = y;
        }

        internal Vector2F(SerializationInfo info, StreamingContext context) {
            X = info.GetSingle("X");
            Y = info.GetSingle("Y");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("X", X);
            info.AddValue("Y", Y);
        }

        public bool EquivalentTo(Vector2F test, float delta = 0.0001f) {
            var xd = Math.Abs(X - test.X);
            var yd = Math.Abs(Y - test.Y);
            return (xd < delta) && (yd < delta);
        }

        public bool Equals(Vector2F other) {
            return EquivalentTo(other);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) { return false; }
            return obj is Vector2F otherVec && Equals(otherVec);
        }

        public override int GetHashCode() {
            unchecked {
                var result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                return result;
            }
        }

        public float Dot(Vector2F c) {
            return ((X * c.X) + (Y * c.Y));
        }

        public Vector2F Round(int num = 4) {
            return new Vector2F(
                (float)Math.Round(X, num),
                (float)Math.Round(Y, num));
        }

        public Vector2F Snap(float snapTo) {
            return new Vector2F(
                (float)Math.Round(X / snapTo) * snapTo,
                (float)Math.Round(Y / snapTo) * snapTo);
        }

        public float LengthSquared() {
            return (float)(Math.Pow(X, 2) + Math.Pow(Y, 2));
        }

        public float VectorMagnitude() {
            return (float)Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
        }

        public Vector2F Normalise() {
            var len = VectorMagnitude();
            return Math.Abs(len - 0) < 0.0001 ? new Vector2F(0, 0) : new Vector2F(X / len, Y / len);
        }

        public Vector2F Absolute() {
            return new Vector2F(Math.Abs(X), Math.Abs(Y));
        }

        public static bool operator ==(Vector2F c1, Vector2F c2) {
            return Equals(c1, null) ? Equals(c2, null) : c1.Equals(c2);
        }

        public static bool operator !=(Vector2F c1, Vector2F c2) {
            return Equals(c1, null) ? !Equals(c2, null) : !c1.Equals(c2);
        }

        public static Vector2F operator +(Vector2F c1, Vector2F c2) {
            return new Vector2F(c1.X + c2.X, c1.Y + c2.Y);
        }

        public static Vector2F operator -(Vector2F c1, Vector2F c2) {
            return new Vector2F(c1.X - c2.X, c1.Y - c2.Y);
        }

        public static Vector2F operator -(Vector2F c1) {
            return new Vector2F(-c1.X, -c1.Y);
        }

        public static Vector2F operator /(Vector2F c, float f) {
            return Math.Abs(f - 0) < 0.0001f ? new Vector2F(0, 0) : new Vector2F(c.X / f, c.Y / f);
        }

        public static Vector2F operator *(Vector2F c, float f) {
            return new Vector2F(c.X * f, c.Y * f);
        }

        public static Vector2F operator *(float f, Vector2F c) {
            return c * f;
        }

        public Vector2F ComponentMultiply(Vector2F c) {
            return new Vector2F(X * c.X, Y * c.Y);
        }

        public Vector2F ComponentDivide(Vector2F c) {
            if (Math.Abs(c.X - 0) < 0.0001) { c.X = 1; }
            if (Math.Abs(c.Y - 0) < 0.0001) { c.Y = 1; }
            return new Vector2F(X / c.X, Y / c.Y);
        }

        public override string ToString() {
            return "(" + X.ToString("0.0000") + " " + Y.ToString("0.0000") + ")";
        }

        public Vector2F Clone() {
            return new Vector2F(X, Y);
        }

        public static Vector2F Parse(string x, string y) {
            return new Vector2F(float.Parse(x), float.Parse(y));
        }
    }
}
