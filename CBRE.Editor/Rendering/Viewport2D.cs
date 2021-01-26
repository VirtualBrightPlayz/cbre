using CBRE.DataStructures.Geometric;
using CBRE.Editor.Documents;
using CBRE.Extensions;
using CBRE.Graphics;
using CBRE.Settings;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using System.Linq;

namespace CBRE.Editor.Rendering {
    public class Viewport2D : ViewportBase {
        public enum ViewDirection {
            /// <summary>
            /// The XY view
            /// </summary>
            Top,

            /// <summary>
            /// The YZ view
            /// </summary>
            Front,

            /// <summary>
            /// The XZ view
            /// </summary>
            Side
        }

        private static readonly Matrix TopMatrix = Matrix.Identity;
        private static readonly Matrix FrontMatrix = new Matrix(0, 0, 1, 0,
                                                                1, 0, 0, 0,
                                                                0, 1, 0, 0,
                                                                0, 0, 0, 1);
        private static readonly Matrix SideMatrix = new Matrix(1, 0, 0, 0,
                                                               0, 0, 1, 0,
                                                               0, 1, 0, 0,
                                                               0, 0, 0, 1);

        private static Matrix GetMatrixFor(ViewDirection dir) {
            switch (dir) {
                case ViewDirection.Top:
                    return TopMatrix;
                case ViewDirection.Front:
                    return FrontMatrix;
                case ViewDirection.Side:
                    return SideMatrix;
                default:
                    throw new ArgumentOutOfRangeException("dir");
            }
        }

        private static Vector3 Flatten(Vector3 c, ViewDirection direction) {
            switch (direction) {
                case ViewDirection.Top:
                    return new Vector3(c.X, c.Y, 0);
                case ViewDirection.Front:
                    return new Vector3(c.Y, c.Z, 0);
                case ViewDirection.Side:
                    return new Vector3(c.X, c.Z, 0);
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        private static Vector3 Expand(Vector3 c, ViewDirection direction) {
            switch (direction) {
                case ViewDirection.Top:
                    return new Vector3(c.X, c.Y, 0);
                case ViewDirection.Front:
                    return new Vector3(0, c.X, c.Y);
                case ViewDirection.Side:
                    return new Vector3(c.X, 0, c.Y);
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        private static Vector3 GetUnusedCoordinate(Vector3 c, ViewDirection direction) {
            switch (direction) {
                case ViewDirection.Top:
                    return new Vector3(0, 0, c.Z);
                case ViewDirection.Front:
                    return new Vector3(c.X, 0, 0);
                case ViewDirection.Side:
                    return new Vector3(0, c.Y, 0);
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        private static Vector3 ZeroUnusedCoordinate(Vector3 c, ViewDirection direction) {
            switch (direction) {
                case ViewDirection.Top:
                    return new Vector3(c.X, c.Y, 0);
                case ViewDirection.Front:
                    return new Vector3(0, c.Y, c.Z);
                case ViewDirection.Side:
                    return new Vector3(c.X, 0, c.Z);
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        public ViewDirection Direction { get; set; }

        private Vector3 _position;
        public Vector3 Position {
            get { return _position; }
            set {
                var old = _position;
                _position = value;
            }
        }

        private decimal _zoom;
        public decimal Zoom {
            get { return _zoom; }
            set {
                var old = _zoom;
                _zoom = DMath.Clamp(value, 0.001m, 10000m);
            }
        }

        private Vector3 CenterScreen {
            get { return new Vector3(Width * 0.5m, Height * 0.5m, 0m); }
        }

        public Viewport2D(ViewDirection direction) {
            Zoom = 0.5m;
            Position = new Vector3(0, 0, 0);
            Direction = direction;
            //CenterScreen = new Vector3(Width / 2m, Height / 2m, 0);
        }

        public override void FocusOn(Vector3 coordinate) {
            Position = Flatten(coordinate);
        }

        public Vector3 Flatten(Vector3 c) {
            return Flatten(c, Direction);
        }

        public Vector3 Expand(Vector3 c) {
            return Expand(c, Direction);
        }

        public Vector3 GetUnusedCoordinate(Vector3 c) {
            return GetUnusedCoordinate(c, Direction);
        }

        public Vector3 ZeroUnusedCoordinate(Vector3 c) {
            return ZeroUnusedCoordinate(c, Direction);
        }

        public override Microsoft.Xna.Framework.Matrix GetViewportMatrix() {
            const float near = -1000000;
            const float far = 1000000;
            return Microsoft.Xna.Framework.Matrix.CreateOrthographic((float)Width, (float)Height, near, far);
        }

        public override Microsoft.Xna.Framework.Matrix GetCameraMatrix() {
            var translate = Microsoft.Xna.Framework.Matrix.CreateTranslation(-(float)Position.X, -(float)Position.Y, 0f);
            var scale = Microsoft.Xna.Framework.Matrix.CreateScale((float)Zoom, (float)Zoom, 0);
            return translate * scale;
        }

        public override Microsoft.Xna.Framework.Matrix GetModelViewMatrix() {
            var modelViewMatrix = GetMatrixFor(Direction);
            return new Microsoft.Xna.Framework.Matrix(
                    (float)modelViewMatrix[0], (float)modelViewMatrix[1], (float)modelViewMatrix[2], (float)modelViewMatrix[3],
                    (float)modelViewMatrix[4], (float)modelViewMatrix[5], (float)modelViewMatrix[6], (float)modelViewMatrix[7],
                    (float)modelViewMatrix[8], (float)modelViewMatrix[9], (float)modelViewMatrix[10], (float)modelViewMatrix[11],
                    (float)modelViewMatrix[12], (float)modelViewMatrix[13], (float)modelViewMatrix[14], (float)modelViewMatrix[15]);
        }

        public Vector3 ScreenToWorld(Point location) {
            return ScreenToWorld(location.X, location.Y);
        }

        public Vector3 ScreenToWorld(decimal x, decimal y) {
            return ScreenToWorld(new Vector3(x, y, 0));
        }

        public Vector3 ScreenToWorld(Vector3 location) {
            return Position + ((location - CenterScreen) / Zoom);
        }

        public Vector3 WorldToScreen(Vector3 location) {
            return CenterScreen + ((location - Position) * Zoom);
        }

        public decimal UnitsToPixels(decimal units) {
            return units * Zoom;
        }

        public decimal PixelsToUnits(decimal pixels) {
            return pixels / Zoom;
        }

        protected override void UpdateBeforeRender() {
            throw new NotImplementedException();
            base.UpdateBeforeRender();
        }

        protected override void UpdateAfterRender() {
            base.UpdateAfterRender();
        }

        public override void Render() {
            if (DocumentManager.CurrentDocument != null) {
                var ObjectRenderer = DocumentManager.CurrentDocument.ObjectRenderer;
                ObjectRenderer.Projection = GetViewportMatrix();
                ObjectRenderer.View = GetModelViewMatrix() * GetCameraMatrix();
                ObjectRenderer.World = Microsoft.Xna.Framework.Matrix.Identity;

                GlobalGraphics.GraphicsDevice.BlendFactor = Microsoft.Xna.Framework.Color.White;
                GlobalGraphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

                ObjectRenderer.RenderWireframe();

                GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            }
        }

        public override void DrawGrid() {
            decimal gridSpacing = DocumentManager.CurrentDocument.Map.GridSpacing;
            while (gridSpacing * Zoom < 4.0m) { gridSpacing *= 4m; }

            int startX = (int)(Position.X - ((Width / 2) / Zoom));
            int startY = (int)(Position.Y - ((Height / 2) / Zoom));
            int endX = (int)(Position.X + ((Width / 2) / Zoom));
            int endY = (int)(Position.Y + ((Height / 2) / Zoom));
            startX = (int)(Math.Round(startX / gridSpacing) * gridSpacing) - (int)gridSpacing;
            startY = (int)(Math.Round(startY / gridSpacing) * gridSpacing) - (int)gridSpacing;
            endX = (int)(Math.Round(endX / gridSpacing) * gridSpacing) + (int)gridSpacing;
            endY = (int)(Math.Round(endY / gridSpacing) * gridSpacing) + (int)gridSpacing;

            PrimitiveDrawing.Begin(PrimitiveType.LineList);
            for (int x = startX; x < endX; x += (int)gridSpacing) {
                if (x == 0) {
                    PrimitiveDrawing.SetColor(Grid.ZeroLines);
                } else {
                    PrimitiveDrawing.SetColor(Color.FromArgb(100, Color.White));
                }
                PrimitiveDrawing.Vertex2(x, startY);
                PrimitiveDrawing.Vertex2(x, endY);
            }

            for (int y = startY; y < endY; y += (int)gridSpacing) {
                if (y == 0) {
                    PrimitiveDrawing.SetColor(Grid.ZeroLines);
                } else {
                    PrimitiveDrawing.SetColor(Color.FromArgb(100, Color.White));
                }
                PrimitiveDrawing.Vertex2(startX, y);
                PrimitiveDrawing.Vertex2(endX, y);
            }

            PrimitiveDrawing.End();
        }
    }
}
