using System;
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

        public LuaBrush(Script lua) {
            this.lua = lua;
        }

        private DynValue NewVector3(Vector3 vector) {
            return DynValue.FromObject(lua, new LuaVector3((double)vector.X, (double)vector.Y, (double)vector.Z));
        }

        private Vector3 NewVector3(DynValue value) {
            LuaVector3 lv = value.ToObject<LuaVector3>();
            return new Vector3((decimal)lv.X, (decimal)lv.Y, (decimal)lv.Z);
        }

        private DynValue NewBox(Box box) {
            DynValue tbl = DynValue.NewTable(lua);
            tbl.Table.Set("Start", NewVector3(box.Start));
            tbl.Table.Set("End", NewVector3(box.End));
            tbl.Table.Set("Center", NewVector3(box.Center));
            return tbl;
        }

        public string Name => (string)lua.Globals["Name"];

        public bool CanRound => throw new System.NotImplementedException();

        public IEnumerable<MapObject> Create(IDGenerator generator, Box box, ITexture texture, int roundDecimals) {
            var solid = new Solid(generator.GetNextObjectID()) { Colour = Colour.GetRandomBrushColour() };
            DynValue val = lua.Call(lua.Globals["Create"], NewBox(box));
            Table faces = val.Table;
            foreach (var arr in faces.Values) {
                var titem = arr.Table;
                var face = new Face(generator.GetNextFaceID()) {
                    Parent = solid,
                    Plane = new Plane(NewVector3(titem.Get(1)), NewVector3(titem.Get(2)), NewVector3(titem.Get(3))),
                    Colour = solid.Colour,
                    Texture = { Texture = texture }
                };
                face.Vertices.AddRange(titem.Values.Select(x => new Vertex(NewVector3(x).Round(roundDecimals), face)));
                face.UpdateBoundingBox();
                face.AlignTextureToFace();
                solid.Faces.Add(face);
            }
            solid.UpdateBoundingBox();
            yield return solid;
        }

        public IEnumerable<BrushControl> GetControls() {
            yield break;
        }
    }
}