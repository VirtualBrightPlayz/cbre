using CBRE.DataStructures.MapObjects;
using System.Collections.Generic;

namespace CBRE.Editor.Actions.MapObjects.Selection {
    public class DeselectFace : ChangeFaceSelection {
        public DeselectFace(IEnumerable<Face> objects) : base(new Face[0], objects) {
        }

        public DeselectFace(params Face[] objects) : base(new Face[0], objects) {
        }
    }
}