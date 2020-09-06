using CBRE.DataStructures.Geometric;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.MapObjects {
    [Serializable]
    public class Camera {
        public Vector3 EyePosition { get; set; }
        public Vector3 LookPosition { get; set; }

        public Camera() {
            EyePosition = new Vector3(0, 0, 0);
            LookPosition = new Vector3(0, 1, 0);
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
    }
}
