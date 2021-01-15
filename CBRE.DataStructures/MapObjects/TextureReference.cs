using CBRE.Common;
using CBRE.DataStructures.Geometric;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.MapObjects {
    [Serializable]
    public class TextureReference : ISerializable {
        public string Name { get; set; }
        public ITexture Texture { get; set; }

        public decimal Rotation { get; set; }

        private Vector3 _uAxis;
        public Vector3 UAxis {
            get { return _uAxis; }
            set { _uAxis = value.Normalise(); }
        }

        private Vector3 _vAxis;
        public Vector3 VAxis {
            get { return _vAxis; }
            set { _vAxis = value.Normalise(); }
        }

        public decimal XShift { get; set; }
        public decimal XScale { get; set; }

        public decimal YShift { get; set; }
        public decimal YScale { get; set; }

        public TextureReference() {
            Name = "";
            Texture = null;
            Rotation = 0;
            _uAxis = -Vector3.UnitZ;
            _vAxis = Vector3.UnitX;
            XShift = YShift = 0;
            XScale = YScale = 1;
        }

        protected TextureReference(SerializationInfo info, StreamingContext context) {
            Name = info.GetString("Name");
            Rotation = info.GetInt32("Rotation");
            _uAxis = (Vector3)info.GetValue("UAxis", typeof(Vector3));
            _vAxis = (Vector3)info.GetValue("VAxis", typeof(Vector3));
            XShift = info.GetDecimal("XShift");
            XScale = info.GetDecimal("XScale");
            YShift = info.GetDecimal("YShift");
            YScale = info.GetDecimal("YScale");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Name", Name);
            info.AddValue("Rotation", Rotation);
            info.AddValue("UAxis", _uAxis);
            info.AddValue("VAxis", _vAxis);
            info.AddValue("XShift", XShift);
            info.AddValue("XScale", XScale);
            info.AddValue("YShift", YShift);
            info.AddValue("YScale", YScale);
        }

        public Vector3 GetNormal() {
            return UAxis.Cross(VAxis).Normalise();
        }

        public TextureReference Clone() {
            return new TextureReference {
                Name = Name,
                Texture = Texture,
                Rotation = Rotation,
                UAxis = UAxis.Clone(),
                VAxis = VAxis.Clone(),
                XShift = XShift,
                XScale = XScale,
                YShift = YShift,
                YScale = YScale
            };
        }
    }
}
