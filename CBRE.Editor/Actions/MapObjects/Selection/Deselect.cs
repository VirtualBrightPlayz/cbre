using System;
using CBRE.DataStructures.MapObjects;
using System.Collections.Generic;

namespace CBRE.Editor.Actions.MapObjects.Selection {
    public class Deselect : ChangeSelection {
        public Deselect(IEnumerable<MapObject> objects) : base(Array.Empty<MapObject>(), objects) {
        }
    }
}
