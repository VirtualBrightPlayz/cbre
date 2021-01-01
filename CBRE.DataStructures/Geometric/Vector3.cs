using CBRE.Extensions;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public class Vector3 : ISerializable {
        public readonly static Vector3 MaxValue = new Vector3(Decimal.MaxValue, Decimal.MaxValue, Decimal.MaxValue);
        public readonly static Vector3 MinValue = new Vector3(Decimal.MinValue, Decimal.MinValue, Decimal.MinValue);
        public readonly static Vector3 Zero = new Vector3(0, 0, 0);
        public readonly static Vector3 One = new Vector3(1, 1, 1);
        public readonly static Vector3 UnitX = new Vector3(1, 0, 0);
        public readonly static Vector3 UnitY = new Vector3(0, 1, 0);
        public readonly static Vector3 UnitZ = new Vector3(0, 0, 1);

        #region X, Y, Z
        private decimal _z;
        private decimal _y;
        private decimal _x;

        private double _dx;
        private double _dy;
        private double _dz;

        public decimal X {
            get { return _x; }
            set {
                _x = value;
                _dx = (double)value;
            }
        }

        public decimal Y {
            get { return _y; }
            set {
                _y = value;
                _dy = (double)value;
            }
        }

        public decimal Z {
            get { return _z; }
            set {
                _z = value;
                _dz = (double)value;
            }
        }

        public double DX {
            get { return _dx; }
            set {
                _dx = value;
                _x = (decimal)value;
            }
        }

        public double DY {
            get { return _dy; }
            set {
                _dy = value;
                _y = (decimal)value;
            }
        }

        public double DZ {
            get { return _dz; }
            set {
                _dz = value;
                _z = (decimal)value;
            }
        }
        #endregion

        public Vector3(decimal x, decimal y, decimal z) {
            _x = x;
            _y = y;
            _z = z;
            _dx = (double)x;
            _dy = (double)y;
            _dz = (double)z;
        }

        public Vector3(Vector3F other) {
            _x = (decimal)other.X;
            _y = (decimal)other.Y;
            _z = (decimal)other.Z;
            _dx = other.X;
            _dy = other.Y;
            _dy = other.Z;
        }

        protected Vector3(SerializationInfo info, StreamingContext context) {
            X = info.GetDecimal("X");
            Y = info.GetDecimal("Y");
            Z = info.GetDecimal("Z");
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("X", X);
            info.AddValue("Y", Y);
            info.AddValue("Z", Z);
        }

        public bool EquivalentTo(Vector3 test, decimal delta = 0.0001m) {
            var xd = Math.Abs(_x - test._x);
            var yd = Math.Abs(_y - test._y);
            var zd = Math.Abs(_z - test._z);
            return (xd < delta) && (yd < delta) && (zd < delta);
        }

        public bool Equals(Vector3 other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EquivalentTo(other);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(Vector3) && Equals((Vector3)obj);
        }

        public override int GetHashCode() {
            unchecked {
                var result = _x.GetHashCode();
                result = (result * 397) ^ _y.GetHashCode();
                result = (result * 397) ^ _z.GetHashCode();
                return result;
            }
        }

        public decimal Dot(Vector3 c) {
            return ((_x * c._x) + (_y * c._y) + (_z * c._z));
        }

        public Vector3 Cross(Vector3 that) {
            var xv = (_y * that._z) - (_z * that._y);
            var yv = (_z * that._x) - (_x * that._z);
            var zv = (_x * that._y) - (_y * that._x);
            return new Vector3(xv, yv, zv);
        }

        public Vector3 Round(int num = 8) {
            return new Vector3(Math.Round(_x, num), Math.Round(_y, num), Math.Round(_z, num));
        }

        public Vector3 Snap(decimal snapTo) {
            return new Vector3(
                Math.Round(_x / snapTo) * snapTo,
                Math.Round(_y / snapTo) * snapTo,
                Math.Round(_z / snapTo) * snapTo
            );
        }

        public decimal VectorMagnitude() {
            return (decimal)Math.Sqrt(Math.Pow(_dx, 2) + Math.Pow(_dy, 2) + Math.Pow(_dz, 2));
        }

        public decimal LengthSquared() {
            return (decimal)(Math.Pow(_dx, 2) + Math.Pow(_dy, 2) + Math.Pow(_dz, 2));
        }

        public Vector3 Normalise() {
            var len = VectorMagnitude();
            return len == 0 ? new Vector3(0, 0, 0) : new Vector3(_x / len, _y / len, _z / len);
        }

        public Vector3 Absolute() {
            return new Vector3(Math.Abs(_x), Math.Abs(_y), Math.Abs(_z));
        }

        public Vector3 XYZ() {
            return new Vector3(_x, _y, _z);
        }

        public Vector3 ZXY() {
            return new Vector3(_z, _x, _y);
        }

        public Vector3 YZX() {
            return new Vector3(_y, _z, _x);
        }

        public Vector3 ZYX() {
            return new Vector3(_z, _y, _x);
        }

        public Vector3 YXZ() {
            return new Vector3(_y, _x, _z);
        }

        public Vector3 XZY() {
            return new Vector3(_x, _z, _y);
        }


        public static bool operator ==(Vector3 c1, Vector3 c2) {
            return Equals(c1, null) ? Equals(c2, null) : c1.Equals(c2);
        }

        public static bool operator !=(Vector3 c1, Vector3 c2) {
            return Equals(c1, null) ? !Equals(c2, null) : !c1.Equals(c2);
        }

        public static Vector3 operator +(Vector3 c1, Vector3 c2) {
            return new Vector3(c1._x + c2._x, c1._y + c2._y, c1._z + c2._z);
        }

        public static Vector3 operator -(Vector3 c1, Vector3 c2) {
            return new Vector3(c1._x - c2._x, c1._y - c2._y, c1._z - c2._z);
        }

        public static Vector3 operator -(Vector3 c1) {
            return new Vector3(-c1._x, -c1._y, -c1._z);
        }

        public static Vector3 operator /(Vector3 c, decimal f) {
            return f == 0 ? new Vector3(0, 0, 0) : new Vector3(c._x / f, c._y / f, c._z / f);
        }

        public static Vector3 operator *(Vector3 c, decimal f) {
            return new Vector3(c._x * f, c._y * f, c._z * f);
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
            return new Vector3(_x * c._x, _y * c._y, _z * c._z);
        }

        public Vector3 ComponentDivide(Vector3 c) {
            var x = c._x == 0 ? 1 : c._x;
            var y = c._y == 0 ? 1 : c._y;
            var z = c._z == 0 ? 1 : c._z;
            return new Vector3(_x / x, _y / y, _z / z);
        }

        /// <summary>
        /// Treats this vector as a directional unit vector and constructs a euler angle representation of that angle (in radians)
        /// </summary>
        /// <returns></returns>
        public Vector3 ToEulerAngles() {
            // http://www.gamedev.net/topic/399701-convert-vector-to-euler-cardan-angles/#entry3651854
            var yaw = DMath.Atan2(_y, _x);
            var pitch = DMath.Atan2(-_z, DMath.Sqrt(_x * _x + _y * _y));
            return new Vector3(0, pitch, yaw); // HL FGD has X = roll, Y = pitch, Z = yaw
        }

        public override string ToString() {
            return "(" + _x.ToString("0.0000") + " " + _y.ToString("0.0000") + " " + _z.ToString("0.0000") + ")";
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
            return toStringNoTrailing(_x) + " " + toStringNoTrailing(_y) + " " + toStringNoTrailing(_z);
        }

        public Vector3 Clone() {
            return new Vector3(_x, _y, _z);
        }

        public static Vector3 Parse(string x, string y, string z) {
            const NumberStyles ns = NumberStyles.Float;
            return new Vector3(decimal.Parse(x, ns), decimal.Parse(y, ns), decimal.Parse(z, ns));
        }

        public Vector3F ToVector3F() {
            return new Vector3F((float)_dx, (float)_dy, (float)_dz);
        }
    }


}
