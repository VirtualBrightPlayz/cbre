using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Brushes.Controls;
using CBRE.Extensions;
using System.Collections.Generic;
using System.Linq;
using CBRE.Graphics;

namespace CBRE.Editor.Brushes {
    public class CylinderBrush : IBrush {
        private readonly NumericControl _numSides;

        public CylinderBrush() {
            _numSides = new NumericControl(this) { LabelText = "Number of sides" };
        }

        public string Name {
            get { return "Cylinder"; }
        }

        public bool CanRound { get { return true; } }

        public IEnumerable<BrushControl> GetControls() {
            yield return _numSides;
        }

        public IEnumerable<MapObject> Create(IDGenerator generator, Box box, ITexture texture, int roundDecimals) {
            var numSides = (int)_numSides.GetValue();
            if (numSides < 3) yield break;

            var faces = ShapeGenerator.Cylinder(box, numSides, roundDecimals);
            
            // Nothing new here, move along
            var solid = new Solid(generator.GetNextObjectID()) { Colour = Colour.GetRandomBrushColour() };
            foreach (var arr in faces) {
                var face = new Face(generator.GetNextFaceID()) {
                    Parent = solid,
                    Plane = new Plane(arr[0], arr[1], arr[2]),
                    Colour = solid.Colour,
                    Texture = { Texture = texture }
                };
                face.Vertices.AddRange(arr.Select(x => new Vertex(x, face)));
                face.UpdateBoundingBox();
                face.AlignTextureToFace();
                solid.Faces.Add(face);
            }
            solid.UpdateBoundingBox();
            yield return solid;
        }
    }
}
