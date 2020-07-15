using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Brushes.Controls;
using System.Collections.Generic;

namespace CBRE.Editor.Brushes {
    public interface IBrush {
        string Name { get; }
        bool CanRound { get; }
        IEnumerable<BrushControl> GetControls();
        IEnumerable<MapObject> Create(IDGenerator generator, Box box, ITexture texture, int roundDecimals);
    }
}
