using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Brushes.Controls;
using CBRE.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CBRE.Editor.Brushes {
    public class TerrainBrush : IBrush {
        private readonly NumericControl _powerOf;

        public TerrainBrush() {
            _powerOf = new NumericControl(this) { LabelText = "Power Of" };
        }

        public string Name {
            get { return "Terrain"; }
        }

        public bool CanRound { get { return true; } }

        public IEnumerable<BrushControl> GetControls() {
            yield return _powerOf;
        }

        private Solid MakeSolid(IDGenerator generator, IEnumerable<Face> faces, ITexture texture, Color col) {
            var solid = new Solid(generator.GetNextObjectID()) { Colour = col };
            foreach (var arr in faces) {
                var face = arr;
                face.UpdateBoundingBox();
                face.AlignTextureToWorld();
                solid.Faces.Add(face);
                face.Parent = solid;
            }
            solid.UpdateBoundingBox();
            return solid;
        }

        public IEnumerable<MapObject> Create(IDGenerator generator, Box box, ITexture texture, int roundDecimals) {
            var powerOf = (int)_powerOf.GetValue();

            var width = box.Width;
            var length = box.Length;
            var height = box.Height;

            var faces = new List<Face>();
            var bottom = new Vector3(box.Center.X, box.Center.Y, box.Start.Z).Round(roundDecimals);
            var top = new Vector3(box.Center.X, box.Center.Y, box.End.Z).Round(roundDecimals);

            var disp = new Displacement(generator.GetNextFaceID());
            disp.StartPosition = box.Start;
            disp.Vertices.Clear();
            disp.Vertices.Add(new Vertex(disp.StartPosition + new Vector3(0, (decimal)length * 1, 0), disp));
            disp.Vertices.Add(new Vertex(disp.StartPosition + new Vector3((decimal)width * 1, (decimal)length * 1, 0), disp));
            disp.Vertices.Add(new Vertex(disp.StartPosition + new Vector3((decimal)width * 1, 0, 0), disp));
            disp.Vertices.Add(new Vertex(disp.StartPosition + new Vector3(0, 0, 0), disp));
            disp.Plane = new Plane(disp.Vertices[1].Location, disp.Vertices[2].Location, disp.Vertices[3].Location);
            disp.Elevation = (decimal)0;
            // disp.AlignTextureToWorld();
            disp.SetPower(powerOf);
            disp.CalculatePoints();
            disp.CalculateNormals();

            faces.Add(disp);

            yield return MakeSolid(generator, faces, texture, Colour.GetRandomBrushColour());
        }
    }
}
