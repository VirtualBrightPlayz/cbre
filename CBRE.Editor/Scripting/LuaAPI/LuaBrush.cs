using System.Collections.Generic;
using System.Linq;
using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Brushes;
using CBRE.Editor.Brushes.Controls;
using MoonSharp.Interpreter;

namespace CBRE.Editor.Scripting.LuaAPI {
    public class LuaBrush : IBrush {
        private Script lua;

        public LuaBrush(Script lua)
        {
            this.lua = lua;
        }

        public string Name => (string)lua.Globals["Name"];

        public int FaceCount => (int)lua.Globals["FaceCount"];
        public int VertCount => (int)lua.Globals["VertCount"];

        public bool CanRound => throw new System.NotImplementedException();

        public IEnumerable<MapObject> Create(IDGenerator generator, Box box, ITexture texture, int roundDecimals) {
            var solid = new Solid(generator.GetNextObjectID()) { Colour = Colour.GetRandomBrushColour() };
            DynValue val = lua.Call(lua.Globals["Create"], box);
            float[][][] faces = val.ToObject<float[][][]>();
            Vector3 toV3(float[] x) {
                return new Vector3((decimal)x[0], (decimal)x[1], (decimal)x[2]);
            }
            foreach (var arr in faces) {
                var face = new Face(generator.GetNextFaceID()) {
                    Parent = solid,
                    Plane = new Plane(toV3(arr[0]), toV3(arr[1]), toV3(arr[2])),
                    Colour = solid.Colour,
                    Texture = { Texture = texture }
                };
                face.Vertices.AddRange(arr.Select(x => new Vertex(toV3(x).Round(roundDecimals), face)));
                face.UpdateBoundingBox();
                face.AlignTextureToFace();
                solid.Faces.Add(face);
            }
            solid.UpdateBoundingBox();
            yield return solid;
        }

        public IEnumerable<BrushControl> GetControls() {
            throw new System.NotImplementedException();
        }
    }
}