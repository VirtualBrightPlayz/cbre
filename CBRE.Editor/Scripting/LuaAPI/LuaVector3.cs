using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

namespace CBRE.Editor.Scripting.LuaAPI {
    public class LuaVector3 {
        public double X;
        public double Y;
        public double Z;

        public LuaVector3(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }

        [MoonSharpUserDataMetamethod("__call")]
        public static LuaVector3 Call(object caller, double x, double y, double z) => new LuaVector3(x, y, z);

        public static LuaVector3 operator +(LuaVector3 a, LuaVector3 b) {
            return new LuaVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static LuaVector3 operator -(LuaVector3 a, LuaVector3 b) {
            return new LuaVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static LuaVector3 operator *(LuaVector3 a, LuaVector3 b) {
            return new LuaVector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static LuaVector3 operator /(LuaVector3 a, LuaVector3 b) {
            return new LuaVector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        }

        public double this[int id] {
            get {
                switch (id) {
                    case 1: return X;
                    case 2: return Y;
                    case 3: return Z;
                    default: return default(double);
                }
            }
        }

        public static bool operator ==(LuaVector3 a, LuaVector3 b) {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(LuaVector3 a, LuaVector3 b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (ReferenceEquals(obj, null)) {
                return false;
            }

            if (obj is LuaVector3 lv) {
                return this == lv;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}