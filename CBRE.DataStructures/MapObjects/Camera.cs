using CBRE.DataStructures.Geometric;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.MapObjects {
    [Serializable]
    public class Camera {
        public Vector3 EyePosition { get; set; }
        public Vector3 LookPosition { get; set; }

        public decimal FOV { get; set; }

        public Camera() {
            EyePosition = new Vector3(0, 0, 0);
            LookPosition = new Vector3(0, 1, 0);
            FOV = 90m;
        }

        protected Camera(SerializationInfo info, StreamingContext context) {
            EyePosition = (Vector3)info.GetValue("EyePosition", typeof(Vector3));
            LookPosition = (Vector3)info.GetValue("LookPosition", typeof(Vector3));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("EyePosition", EyePosition);
            info.AddValue("LookPosition", LookPosition);
        }

        public decimal Length {
            get { return (LookPosition - EyePosition).VectorMagnitude(); }
            set { LookPosition = EyePosition + Direction * value; }
        }

        public Vector3 Direction {
            get { return (LookPosition - EyePosition).Normalise(); }
            set { LookPosition = EyePosition + value.Normalise() * Length; }
        }

        public decimal GetRotation() {
            var temp = (LookPosition - EyePosition).Normalise();
            var rot = Math.Atan2((double)temp.Y, (double)temp.X);
            if (rot < 0) rot += 2 * Math.PI;
            if (rot > 2 * Math.PI) rot = rot % (2 * Math.PI);
            return (decimal)rot;
        }

        public void SetRotation(decimal rotation) {
            var temp = (LookPosition - EyePosition).Normalise();
            var e = GetElevation();
            var x = (decimal)(Math.Cos((double)rotation) * Math.Sin((double)e));
            var y = (decimal)(Math.Sin((double)rotation) * Math.Sin((double)e));
            LookPosition = new Vector3(x + EyePosition.X, y + EyePosition.Y, temp.Z + EyePosition.Z);
        }

        public decimal GetElevation() {
            var temp = (LookPosition - EyePosition).Normalise();
            var elev = Math.Acos((double)temp.Z);
            return (decimal)elev;
        }

        public void SetElevation(decimal elevation) {
            if (elevation > ((decimal)Math.PI * 0.99m)) elevation = (decimal)Math.PI * 0.99m;
            if (elevation < ((decimal)Math.PI * 0.01m)) elevation = (decimal)Math.PI * 0.01m;
            var rotation = GetRotation();
            var x = (decimal)(Math.Cos((double)rotation) * Math.Sin((double)elevation));
            var y = (decimal)(Math.Sin((double)rotation) * Math.Sin((double)elevation));
            var z = (decimal)(Math.Cos((double)elevation));
            LookPosition = new Vector3(x + EyePosition.X, y + EyePosition.Y, z + EyePosition.Z);
        }

        public void Pan(decimal degrees) {
            var rad = degrees * ((decimal)Math.PI / 180);
            var rot = GetRotation();
            SetRotation(rot + rad);
        }

        public void Tilt(decimal degrees) {
            SetElevation(GetElevation() + (degrees * ((decimal)Math.PI / 180)));
        }

        public void Advance(decimal units) {
            var temp = (LookPosition - EyePosition).Normalise();
            var add = temp * units;
            LookPosition += add;
            EyePosition += add;
        }

        public void Strafe(decimal units) {
            var right = GetRight();
            var add = right * units;
            LookPosition += add;
            EyePosition += add;
        }

        public void Ascend(decimal units) {
            var up = GetUp();
            var add = up * units;
            LookPosition += add;
            EyePosition += add;
        }

        public Vector3 GetUp() {
            var temp = (LookPosition - EyePosition).Normalise();
            var normal = GetRight().Cross(temp);
            return normal.Normalise();
        }

        public Vector3 GetRight() {
            var temp = LookPosition - EyePosition;
            temp.Z = 0;
            temp = temp.Normalise();
            var normal = temp.Cross(Vector3.UnitZ);
            return normal.Normalise();
        }
    }
}
