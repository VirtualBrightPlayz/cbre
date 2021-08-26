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

        public LuaBrush(Script lua)
        {
            this.lua = lua;
            lua.Globals["Vector3"] = DynValue.NewCallback(NewVector3);
        }

        private DynValue NewVector3(ScriptExecutionContext arg1, CallbackArguments arg2) {
            DynValue tbl = DynValue.NewTable(lua);
            tbl.Table.Set(1, arg2[0]);
            tbl.Table.Set(2, arg2[1]);
            tbl.Table.Set(3, arg2[2]);
            return tbl;
        }

        private DynValue NewVector3(Vector3 vector) {
            DynValue tbl = DynValue.NewTable(lua);
            tbl.Table.Set(1, DynValue.NewNumber((double)vector.X));
            tbl.Table.Set(2, DynValue.NewNumber((double)vector.Y));
            tbl.Table.Set(3, DynValue.NewNumber((double)vector.Z));
            return tbl;
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
            Vector3 toV3(Table x) {
                return new Vector3((decimal)x.Get(1).Number, (decimal)x.Get(2).Number, (decimal)x.Get(3).Number);
            }
            foreach (var arr in faces.Values) {
                var titem = arr.Table;
                var face = new Face(generator.GetNextFaceID()) {
                    Parent = solid,
                    Plane = new Plane(toV3(titem.Get(1).Table), toV3(titem.Get(2).Table), toV3(titem.Get(3).Table)),
                    Colour = solid.Colour,
                    Texture = { Texture = texture }
                };
                face.Vertices.AddRange(titem.Values.Select(x => new Vertex(toV3(x.Table).Round(roundDecimals), face)));
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