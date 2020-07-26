using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Brushes {
    public static class BrushManager {
        public static IBrush CurrentBrush { get; private set; }

        private static readonly List<IBrush> Brushes;
        //private static ComboBox _comboBox;
        public static bool RoundCreatedVertices;

        static BrushManager() {
            Brushes = new List<IBrush>();
            RoundCreatedVertices = true;
        }

        public static void Init() {
            Brushes.Add(new BlockBrush());
            Brushes.Add(new TetrahedronBrush());
            Brushes.Add(new PyramidBrush());
            Brushes.Add(new WedgeBrush());
            Brushes.Add(new CylinderBrush());
            Brushes.Add(new ConeBrush());
            Brushes.Add(new PipeBrush());
            Brushes.Add(new ArchBrush());
            Brushes.Add(new SphereBrush());
            Brushes.Add(new TorusBrush());
        }

        public static void Register(IBrush brush) {
            Brushes.Add(brush);
        }
    }
}
