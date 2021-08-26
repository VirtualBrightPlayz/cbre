using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Brushes {
    public static class BrushManager {
        public static IBrush CurrentBrush { get; set; }

        private static readonly List<IBrush> brushes;
        public static IEnumerable<IBrush> Brushes => brushes;

        //private static ComboBox _comboBox;
        public static bool RoundCreatedVertices;

        static BrushManager() {
            brushes = new List<IBrush>();
            RoundCreatedVertices = true;
            Init();
        }

        public static void Init() {
            brushes.Clear();
            brushes.Add(new BlockBrush());
            brushes.Add(new TetrahedronBrush());
            brushes.Add(new PyramidBrush());
            brushes.Add(new WedgeBrush());
            brushes.Add(new CylinderBrush());
            brushes.Add(new ConeBrush());
            brushes.Add(new PipeBrush());
            brushes.Add(new ArchBrush());
            brushes.Add(new SphereBrush());
            brushes.Add(new TorusBrush());
        }

        public static void Register(IBrush brush) {
            brushes.Add(brush);
        }
    }
}
