using CBRE.DataStructures.Geometric;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Transformations {
    [Serializable]
    public class UnitTranslate : IUnitTransformation {
        public Vector3 Translation { get; set; }

        public UnitTranslate(Vector3 translation) {
            Translation = translation;
        }

        protected UnitTranslate(SerializationInfo info, StreamingContext context) {
            Translation = (Vector3)info.GetValue("Translation", typeof(Vector3));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Translation", Translation);
        }

        public Vector3 Transform(Vector3 c) {
            return c + Translation;
        }

        public Vector3F Transform(Vector3F c) {
            return c + new Vector3F(Translation);
        }
    }
}
