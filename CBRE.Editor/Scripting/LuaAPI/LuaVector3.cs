using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

namespace CBRE.Editor.Scripting.LuaAPI {
    public class LuaVector3 {
        public float X;
        public float Y;
        public float Z;

        public LuaVector3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

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

        public float this[int id] {
            get {
                switch (id) {
                    case 1: return X;
                    case 2: return Y;
                    case 3: return Z;
                    default: return default(float);
                }
            }
        }

        public static bool operator ==(LuaVector3 a, LuaVector3 b) {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(LuaVector3 a, LuaVector3 b) {
            return !(a == b);
        }
    }
}