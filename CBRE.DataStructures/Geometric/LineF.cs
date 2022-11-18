using CBRE.DataStructures.Transformations;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric {
    [Serializable]
    public class LineF : ISerializable {
        public Vector3F Start { get; set; }
        public Vector3F End { get; set; }

        public readonly static LineF AxisX = new LineF(Vector3F.Zero, Vector3F.UnitX);
        public readonly static LineF AxisY = new LineF(Vector3F.Zero, Vector3F.UnitY);
        public static readonly LineF AxisZ = new LineF(Vector3F.Zero, Vector3F.UnitZ);

        public LineF(Vector3F start, Vector3F end) {
            Start = start;
            End = end;
        }

        protected LineF(SerializationInfo info, StreamingContext context) {
            Start = (Vector3F)info.GetValue("Start", typeof(Vector3F));
            End = (Vector3F)info.GetValue("End", typeof(Vector3F));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Start", Start);
            info.AddValue("End", End);
        }

        public LineF Reverse() {
            return new LineF(End, Start);
        }

        public Vector3F ClosestPoint(Vector3F point) {
            // http://paulbourke.net/geometry/pointline/

            var delta = End - Start;
            var den = delta.LengthSquared();
            if (den == 0) return Start; // Start and End are the same

            var numPoint = (point - Start).ComponentMultiply(delta);
            var num = numPoint.X + numPoint.Y + numPoint.Z;
            var u = num / den;

            if (u < 0) return Start; // Point is before the segment start
            if (u > 1) return End;   // Point is after the segment end
            return Start + u * delta;
        }
        
        public float DistanceFrom(Vector3F point)
            => (point - ClosestPoint(point)).VectorMagnitude();

        /// <summary>
        /// Determines if this line is behind, in front, or spanning a plane.
        /// </summary>
        /// <param name="p">The plane to test against</param>
        /// <returns>A PlaneClassification value.</returns>
        public PlaneClassification ClassifyAgainstPlane(PlaneF p) {
            var start = p.OnPlane(Start);
            var end = p.OnPlane(End);

            if (start == 0 && end == 0) return PlaneClassification.OnPlane;
            if (start <= 0 && end <= 0) return PlaneClassification.Back;
            if (start >= 0 && end >= 0) return PlaneClassification.Front;
            return PlaneClassification.Spanning;
        }

        public LineF Transform(IUnitTransformation transform) {
            return new LineF(transform.Transform(Start), transform.Transform(End));
        }

        public bool EquivalentTo(LineF other, float delta = 0.0001f) {
            return (Start.EquivalentTo(other.Start, delta) && End.EquivalentTo(other.End, delta))
                || (End.EquivalentTo(other.Start, delta) && Start.EquivalentTo(other.End, delta));
        }

        public bool Equals(LineF other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return (Equals(other.Start, Start) && Equals(other.End, End))
                || (Equals(other.End, Start) && Equals(other.Start, End));
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(LineF)) return false;
            return Equals((LineF)obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Start != null ? Start.GetHashCode() : 0) * 397) ^ (End != null ? End.GetHashCode() : 0);
            }
        }

        public static bool operator ==(LineF left, LineF right) {
            return Equals(left, right);
        }

        public static bool operator !=(LineF left, LineF right) {
            return !Equals(left, right);
        }
    }
}
