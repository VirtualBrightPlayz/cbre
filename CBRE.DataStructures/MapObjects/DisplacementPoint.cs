﻿using CBRE.DataStructures.Geometric;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.MapObjects {
    [Serializable]
    public class DisplacementPoint : ISerializable {
        public Displacement Parent { get; set; }

        public Vertex CurrentPosition { get; set; }
        public Vector3 InitialPosition { get; set; }

        public Vector Displacement { get; set; }
        public Vector OffsetDisplacement { get; set; }

        public int XIndex { get; set; }
        public int YIndex { get; set; }
        public decimal Alpha { get; set; }

        /// <summary>
        /// Shorthand for CurrentPosition.Location.
        /// </summary>
        public Vector3 Location {
            get { return CurrentPosition.Location; }
        }

        public DisplacementPoint(Displacement parent, int x, int y) {
            Parent = parent;
            XIndex = x;
            YIndex = y;
            CurrentPosition = new Vertex(Vector3.Zero, parent);
            InitialPosition = Vector3.Zero;
            Displacement = new Vector(Vector3.UnitZ, 0);
            OffsetDisplacement = new Vector(Vector3.UnitZ, 0);
            Alpha = 0;
        }

        protected DisplacementPoint(SerializationInfo info, StreamingContext context) {
            XIndex = info.GetInt32("XIndex");
            YIndex = info.GetInt32("YIndex");
            CurrentPosition = (Vertex)info.GetValue("CurrentPosition", typeof(Vertex));
            InitialPosition = (Vector3)info.GetValue("InitialPosition", typeof(Vector3));
            Displacement = (Vector)info.GetValue("Displacement", typeof(Vector));
            OffsetDisplacement = (Vector)info.GetValue("OffsetDisplacement", typeof(Vector));
            Alpha = info.GetDecimal("Alpha");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("XIndex", XIndex);
            info.AddValue("YIndex", YIndex);
            info.AddValue("CurrentPosition", CurrentPosition);
            info.AddValue("InitialPosition", InitialPosition);
            info.AddValue("Displacement", Displacement);
            info.AddValue("OffsetDisplacement", OffsetDisplacement);
            info.AddValue("Alpha", Alpha);
        }

        public IEnumerable<DisplacementPoint> GetAdjacentPoints() {
            yield return Parent.GetPoint(XIndex + 1, YIndex + 0);
            yield return Parent.GetPoint(XIndex - 1, YIndex + 0);
            yield return Parent.GetPoint(XIndex + 0, YIndex + 1);
            yield return Parent.GetPoint(XIndex + 0, YIndex - 1);
            yield return Parent.GetPoint(XIndex - 1, YIndex - 1);
            yield return Parent.GetPoint(XIndex - 1, YIndex + 1);
            yield return Parent.GetPoint(XIndex + 1, YIndex - 1);
            yield return Parent.GetPoint(XIndex + 1, YIndex + 1);
        }
    }
}
