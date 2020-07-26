using CBRE.DataStructures.MapObjects;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Tools.VMTool
{
    public class VMError
    {
        public Solid Solid { get; set; }
        public List<Face> Faces { get; set; }
        public List<Vertex> Vertices { get; set; }
        public string Message { get; set; }

        public VMError(string message, Solid solid, IEnumerable<Face> faces = null, IEnumerable<Vertex> vertices = null)
        {
            Message = message;
            Solid = solid;
            Faces = (faces ?? new Face[0]).ToList();
            Vertices = (vertices ?? new Vertex[0]).ToList();
        }

        public override string ToString()
        {
            return Message;
        }
    }
}