﻿using CBRE.DataStructures.GameData;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.Transformations;
using CBRE.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.MapObjects {
    [Serializable]
    public class Entity : MapObject {
        public GameDataObject GameData { get; set; }
        public EntityData EntityData { get; set; }
        public Vector3 Origin {
            get {
                return EntityData.GetPropertyVector3("position", Vector3.Zero).XZY();
            }

            set {
                EntityData.SetPropertyValue("position", value.XZY().ToDataString());
            }
        }

        public Matrix RightHandedWorldMatrix {
            get {
                var scale = EntityData.GetPropertyVector3("scale", Vector3.One);
                scale = new Vector3(scale.X, scale.Z, scale.Y);
                var angles = EntityData.GetPropertyVector3("angles", Vector3.Zero);
                Matrix pitch = Matrix.Rotation(Quaternion.EulerAngles(DMath.DegreesToRadians(angles.X), 0, 0));
                Matrix yaw = Matrix.Rotation(Quaternion.EulerAngles(0, 0, -DMath.DegreesToRadians(angles.Y)));
                Matrix roll = Matrix.Rotation(Quaternion.EulerAngles(0, DMath.DegreesToRadians(angles.Z), 0));
                var tform = ((yaw * roll * pitch) * Matrix.Scale(scale)).Translate(Origin);
                return tform;
            }
        }

        public Matrix LeftHandedWorldMatrix {
            get {
                var scale = EntityData.GetPropertyVector3("scale", Vector3.One);
                scale = new Vector3(scale.X, scale.Z, scale.Y);
                var angles = EntityData.GetPropertyVector3("angles", Vector3.Zero);
                Matrix pitch = Matrix.Rotation(Quaternion.EulerAngles(DMath.DegreesToRadians(angles.X), 0, 0));
                Matrix yaw = Matrix.Rotation(Quaternion.EulerAngles(0, 0, -DMath.DegreesToRadians(angles.Y)));
                Matrix roll = Matrix.Rotation(Quaternion.EulerAngles(0, DMath.DegreesToRadians(angles.Z), 0));
                var tform = ((yaw * roll * pitch) * Matrix.Scale(scale)).Transpose().Translate(Origin);
                var yzflip = new Matrix(
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                );
                // tform *= yzflip;
                // tform = tform.Transpose();
                /*
                tform = new Matrix(
                    tform[0], tform[1], tform[2], tform[3],
                    tform[4], tform[5], tform[6], tform[7],
                    tform[8], tform[9], tform[10], tform[11],
                    tform[12], tform[13], tform[14], tform[15]
                );
                */
                /*
                tform = new Matrix(
                    tform[0], tform[2], tform[1], tform[3],
                    tform[8], tform[10], tform[9], tform[11],
                    tform[4], tform[6], tform[5], tform[7],
                    tform[12], tform[14], tform[13], tform[15]
                );
                */
                return tform;
            }
        }

        public Entity(long id) : base(id) {
            EntityData = new EntityData();
        }

        protected Entity(SerializationInfo info, StreamingContext context) : base(info, context) {
            EntityData = (EntityData)info.GetValue("EntityData", typeof(EntityData));
            Origin = (Vector3)info.GetValue("Origin", typeof(Vector3));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("EntityData", EntityData);
            info.AddValue("Origin", Origin);
        }

        public override MapObject Copy(IDGenerator generator) {
            var e = new Entity(generator.GetNextObjectID()) {
                GameData = GameData,
                EntityData = EntityData.Clone()
            };
            CopyBase(e, generator);
            return e;
        }

        public override void Paste(MapObject o, IDGenerator generator) {
            PasteBase(o, generator);
            var e = o as Entity;
            if (e == null) return;
            GameData = e.GameData;
            EntityData = e.EntityData.Clone();
        }

        public override MapObject Clone() {
            var e = new Entity(ID) { GameData = GameData, EntityData = EntityData.Clone() };
            CopyBase(e, null, true);
            return e;
        }

        public override void Unclone(MapObject o) {
            PasteBase(o, null, true);
            var e = o as Entity;
            if (e == null) return;
            GameData = e.GameData;
            EntityData = e.EntityData.Clone();
        }

        public override void UpdateBoundingBox(bool cascadeToParent = true) {
            if (GameData == null && !Children.Any()) {
                var sub = new Vector3(-16, -16, -16);
                var add = new Vector3(16, 16, 16);
                BoundingBox = new Box(Origin + sub, Origin + add);
            } else if (MetaData.Has<Box>("BoundingBox")) {
                var scale = EntityData.GetPropertyVector3("scale", Vector3.One);
                scale = new Vector3(scale.X, scale.Z, scale.Y);
                var angles = EntityData.GetPropertyVector3("angles", Vector3.Zero);
                Matrix pitch = Matrix.Rotation(Quaternion.EulerAngles(DMath.DegreesToRadians(angles.X), 0, 0));
                Matrix yaw = Matrix.Rotation(Quaternion.EulerAngles(0, 0, -DMath.DegreesToRadians(angles.Y)));
                Matrix roll = Matrix.Rotation(Quaternion.EulerAngles(0, DMath.DegreesToRadians(angles.Z), 0));
                var tform = ((yaw * roll * pitch) * Matrix.Scale(scale)).Translate(Origin);
                if (MetaData.Has<bool>("RotateBoundingBox") && !MetaData.Get<bool>("RotateBoundingBox")) tform = Matrix.Translation(Origin);
                BoundingBox = MetaData.Get<Box>("BoundingBox").Transform(new UnitMatrixMult(tform));
            } else if (GameData != null && GameData.ClassType == ClassType.Point) {
                var sub = new Vector3(-16, -16, -16);
                var add = new Vector3(16, 16, 16);
                var behav = GameData.Behaviours.SingleOrDefault(x => x.Name == "size");
                if (behav != null && behav.Values.Count >= 6) {
                    sub = behav.GetVector3(0) ?? Vector3.Zero;
                    add = behav.GetVector3(1) ?? Vector3.Zero;
                } else if (GameData.Name == "infodecal") {
                    sub = Vector3.One * -4m;
                    add = Vector3.One * 4m;
                }
                BoundingBox = new Box(Origin + sub, Origin + add);
            } else if (Children.Any()) {
                BoundingBox = new Box(GetChildren().SelectMany(x => new[] { x.BoundingBox.Start, x.BoundingBox.End }));
            } else {
                BoundingBox = new Box(Origin, Origin);
            }
            base.UpdateBoundingBox(cascadeToParent);
        }

        public new Color Colour {
            get {
                if (GameData != null && GameData.ClassType == ClassType.Point) {
                    var behav = GameData.Behaviours.LastOrDefault(x => x.Name == "color");
                    if (behav != null && behav.Values.Count == 3) {
                        return behav.GetColour(0);
                    }
                }
                return base.Colour;
            }
            set { base.Colour = value; }
        }

        public IEnumerable<Face> GetBoxFaces() {
            var faces = new List<Face>();
            if (Children.Any()) return faces;

            var box = BoundingBox.GetBoxFaces();
            var dummySolid = new Solid(-1) {
                IsCodeHidden = IsCodeHidden,
                IsRenderHidden2D = IsRenderHidden2D,
                IsSelected = IsSelected,
                IsRenderHidden3D = IsRenderHidden3D,
                IsVisgroupHidden = IsVisgroupHidden
            };
            foreach (var ca in box) {
                var face = new Face(0) {
                    Plane = new Plane(ca[0], ca[1], ca[2]),
                    Colour = Colour,
                    IsSelected = IsSelected,
                    Parent = dummySolid
                };
                face.Vertices.AddRange(ca.Select(x => new Vertex(x, face)));
                face.UpdateBoundingBox();
                faces.Add(face);
            }
            return faces;
        }

        public override void Transform(IUnitTransformation transform, TransformFlags flags) {
            Origin = transform.Transform(Origin);
            Vector3 angles = EntityData.GetPropertyVector3("angles");
            if (angles != null && transform is UnitMatrixMult uTransform) {
                var finalAngles = TransformToEuler(uTransform, angles);

                EntityData.SetPropertyValue("angles", finalAngles.ToDataString());
            }
            base.Transform(transform, flags);
        }

        public static Vector3 TransformToEuler(UnitMatrixMult uTransform, Vector3 angles) {
            Matrix _pitch = Matrix.Rotation(Quaternion.EulerAngles(DMath.DegreesToRadians(angles.X), 0, 0));
            Matrix _yaw = Matrix.Rotation(Quaternion.EulerAngles(0, 0, -DMath.DegreesToRadians(angles.Y)));
            Matrix _roll = Matrix.Rotation(Quaternion.EulerAngles(0, DMath.DegreesToRadians(angles.Z), 0));
            var existingRotation = new UnitMatrixMult(_yaw * _roll * _pitch);

            Vector3 unitX = uTransform.Transform(existingRotation.Transform(new Vector3(1, 0, 0)), 0).Normalise();
            Vector3 unitY = uTransform.Transform(existingRotation.Transform(new Vector3(0, 1, 0)), 0).Normalise();
            Vector3 unitZ = uTransform.Transform(existingRotation.Transform(new Vector3(0, 0, 1)), 0).Normalise();

            var tempAngles = ToEuler(unitZ.YXZ(), unitY.YXZ(), unitX.YXZ()).XZY() * 180m / DMath.PI;
            return new Vector3(
                90 - tempAngles.X,
                tempAngles.Y,
                90 - tempAngles.Z);
        }

        //http://geom3d.com/data/documents/Calculation=20of=20Euler=20angles.pdf
        private static Vector3 ToEuler(Vector3 X1, Vector3 Y1, Vector3 Z1) {
            decimal Z1xy = DMath.Sqrt(Z1.X * Z1.X + Z1.Y * Z1.Y);
            if (Z1xy > 0.0001m) {
                return new Vector3(
                    DMath.Atan2(Y1.X * Z1.Y - Y1.Y * Z1.X, X1.X * Z1.Y - X1.Y * Z1.X),
                    DMath.Atan2(Z1xy, Z1.Z),
                    -DMath.Atan2(-Z1.X, Z1.Y));
            } else {
                return new Vector3(
                    0m,
                    (Z1.Z > 0m) ? 0m : DMath.PI,
                    -DMath.Atan2(X1.Y, X1.X));
            }
        }

        /// <summary>
        /// Returns the intersection point closest to the start of the line.
        /// </summary>
        /// <param name="line">The intersection line</param>
        /// <returns>The closest intersecting point, or null if the line doesn't intersect.</returns>
        public override Vector3? GetIntersectionPoint(Line line) {
            var faces = GetBoxFaces().Union(MetaData.GetAll<List<Face>>().SelectMany(x => x));
            return faces.Select(x => x.GetIntersectionPoint(line))
                .Where(x => x != null)
                .OrderBy(x => (x.Value - line.Start).VectorMagnitude())
                .FirstOrDefault();
        }

        public override Box GetIntersectionBoundingBox() {
            return new Box(new[] { BoundingBox }.Union(MetaData.GetAll<Box>()));
        }

        public override EntityData GetEntityData() {
            return EntityData;
        }
    }
}
