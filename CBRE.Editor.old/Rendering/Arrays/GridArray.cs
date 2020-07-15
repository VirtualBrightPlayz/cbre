using CBRE.Graphics.Arrays;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Drawing;

namespace CBRE.Editor.Rendering.Arrays {
    public class GridArray : VBO<object, MapObjectVertex> {
        private const int Grid = 0;

        public GridArray()
            : base(new object[0]) {
        }

        public void Render(IGraphicsContext context) {
            foreach (var subset in GetSubsets(Grid)) {
                Render(context, PrimitiveType.Lines, subset);
            }
        }

        private decimal _step = 64;
        private int _low = -4096;
        private int _high = 4096;

        public void Update(int low, int high, decimal gridSpacing, decimal zoom, bool force = false) {
            var actualDist = gridSpacing * zoom;
            if (CBRE.Settings.Grid.HideSmallerOn) {
                while (actualDist < CBRE.Settings.Grid.HideSmallerThan) {
                    gridSpacing *= CBRE.Settings.Grid.HideFactor;
                    actualDist *= CBRE.Settings.Grid.HideFactor;
                }
            }
            if (gridSpacing == _step && !force && low == _low && high == _high) return; // This grid is the same as before
            _step = gridSpacing;
            _low = low;
            _high = high;
            Update(new object[0]);
        }

        protected override void CreateArray(IEnumerable<object> objects) {
            StartSubset(Grid);
            for (decimal i = _low; i <= _high; i += _step) {
                var c = CBRE.Settings.Grid.GridLines;
                if (i == 0) c = CBRE.Settings.Grid.ZeroLines;
                else if (i % CBRE.Settings.Grid.Highlight2UnitNum == 0 && CBRE.Settings.Grid.Highlight2On) c = CBRE.Settings.Grid.Highlight2;
                else if (i % (_step * CBRE.Settings.Grid.Highlight1LineNum) == 0 && CBRE.Settings.Grid.Highlight1On) c = CBRE.Settings.Grid.Highlight1;
                var ifloat = (float)i;
                MakePoint(c, _low, ifloat);
                MakePoint(c, _high, ifloat);
                MakePoint(c, ifloat, _low);
                MakePoint(c, ifloat, _high);
            }

            // Top
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _low, _high);
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _high, _high);
            // Left
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _low, _low);
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _low, _high);
            // Right
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _high, _low);
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _high, _high);
            // Bottom
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _low, _low);
            MakePoint(CBRE.Settings.Grid.BoundaryLines, _high, _low);

            PushSubset(Grid, (object)null);
        }

        private void MakePoint(Color colour, float x, float y, float z = 0) {
            PushIndex(Grid, PushData(new[]
            {
                new MapObjectVertex
                {
                    Position = new Vector3(x, y, z),
                    Colour = new Color4(colour.R, colour.G, colour.B, colour.A),
                    Normal = Vector3.Zero,
                    Texture = Vector2.Zero,
                    LightmapUv = new Vector2(-500.0f, -500.0f),
                    IsSelected = 0
                }
            }), new uint[] { 0 });
        }
    }
}