using System.Collections.Generic;
using System.Drawing;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Tools.DisplacementTool {
    public class DPDragTool : DisplacementSubTool {
        private DisplacementPoint[] _current;
        private decimal offset;

        public DPDragTool(DisplacementTool tool) : base(tool) {
        }

        public override void Render3D(Viewport3D viewport) {
            if (_current == null)
                return;
            // Get us into 2D rendering
            const float near = -1000000;
            const float far = 1000000;
            var matrix = Microsoft.Xna.Framework.Matrix.CreateOrthographic(viewport.Width, viewport.Height, near, far);
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            BasicEffect basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            basicEffect.LightingEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = matrix;
            basicEffect.View = Microsoft.Xna.Framework.Matrix.Identity;
            basicEffect.World = Microsoft.Xna.Framework.Matrix.Identity;
            basicEffect.CurrentTechnique.Passes[0].Apply();

            var half = new Vector3(viewport.Width, viewport.Height, 0) / 2;
            // Render out the point handles
            PrimitiveDrawing.Begin(PrimitiveType.QuadList);
            foreach (var point in _current) {

                var c = viewport.WorldToScreen(point.CurrentPosition.Location + new Vector3(0, 0, offset));
                if (c == null || c.Z > 1) continue;
                c -= half;

                PrimitiveDrawing.SetColor(Color.DarkBlue);
                PrimitiveDrawing.Vertex2(c.DX - 4, c.DY - 4);
                PrimitiveDrawing.Vertex2(c.DX - 4, c.DY + 4);
                PrimitiveDrawing.Vertex2(c.DX + 4, c.DY + 4);
                PrimitiveDrawing.Vertex2(c.DX + 4, c.DY - 4);

                PrimitiveDrawing.SetColor(Color.OrangeRed);
                PrimitiveDrawing.Vertex2(c.DX - 3, c.DY - 3);
                PrimitiveDrawing.Vertex2(c.DX - 3, c.DY + 3);
                PrimitiveDrawing.Vertex2(c.DX + 3, c.DY + 3);
                PrimitiveDrawing.Vertex2(c.DX + 3, c.DY - 3);
            }
            PrimitiveDrawing.End();

            // Get back into 3D rendering
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            ViewportManager.basicEffect.CurrentTechnique.Passes[0].Apply();
        }

        public override void DragEnd() {
            if (_current == null)
                return;
            foreach (var point in _current) {
                point.CurrentPosition.Location.Z += offset;
                point.Displacement.Distance += offset;
            }
            _current = null;
        }

        public override void DragMove(Vector3 distance) {
            offset -= distance.Y;
        }

        public override void DragStart(List<DisplacementPoint> clickedPoints) {
            _current = clickedPoints.ToArray();
            offset = 0;
        }

        public override string GetContextualHelp() {
            return "Click and drag a dot in the 3D Viewport to move it.\n" +
            "You can only move the dots Up and Down.";
        }

        public override string GetName() {
            return "Displacement Drag Tool";
        }

        public override void KeyDown(ViewportBase viewport, ViewportEvent e) {
        }

        public override void KeyPress(ViewportBase viewport, ViewportEvent e) {
        }

        public override void KeyUp(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseDown(ViewportBase viewport, ViewportEvent e) {
            if (viewport is Viewport3D vp) {
                
            }
        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseUp(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e) {
        }

        public override void Render(ViewportBase viewport) {
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame) {
        }
    }
}