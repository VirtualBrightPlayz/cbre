using CBRE.DataStructures.MapObjects;
using System.Collections.Generic;

namespace CBRE.Editor.Actions.MapObjects.Selection {
    public class SelectFace : ChangeFaceSelection {
        public SelectFace(IEnumerable<Face> objects) : base(objects, new Face[0]) {
        }

        public SelectFace(params Face[] objects) : base(objects, new Face[0]) {
        }
    }
}