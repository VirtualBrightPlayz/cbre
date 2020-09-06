using CBRE.DataStructures.Geometric;
using CBRE.Editor.Documents;
using CBRE.Extensions;
using CBRE.Graphics;
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

        private static Vector3 GetUnusedVector3(Vector3 c, ViewDirection direction) {
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

        private static Vector3 ZeroUnusedVector3(Vector3 c, ViewDirection direction) {
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

        private Vector3 CenterScreen { get; set; }

        public Viewport2D(ViewDirection direction) {
            Zoom = 0.5m;
            Position = new Vector3(0, 0, 0);
            Direction = direction;
            CenterScreen = new Vector3(Width / 2m, Height / 2m, 0);
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

        public Vector3 GetUnusedVector3(Vector3 c) {
            return GetUnusedVector3(c, Direction);
        }

        public Vector3 ZeroUnusedVector3(Vector3 c) {
            return ZeroUnusedVector3(c, Direction);
        }

        public override Matrix GetViewportMatrix() {
            throw new NotImplementedException();
            const float near = -1000000;
            const float far = 1000000;
            //return Matrix.CreateOrthographic(Width, Height, near, far);
        }

        public override Matrix GetCameraMatrix() {
            var translate = Matrix.Translation(new DataStructures.Geometric.Vector3(-Position.X, -Position.Y, 0));
            var scale = Matrix.Scale(new Vector3(Zoom, Zoom, 0));
            return translate * scale;
        }

        public override Matrix GetModelViewMatrix() {
            return GetMatrixFor(Direction);
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
                var brushRenderer = DocumentManager.CurrentDocument.BrushRenderer;
                brushRenderer.Projection = Microsoft.Xna.Framework.Matrix.CreateOrthographic((float)Width, (float)Height, -100000.0f, 100000.0f);
                var matrix = GetModelViewMatrix();

                brushRenderer.View = new Microsoft.Xna.Framework.Matrix(
                    (float)matrix[0], (float)matrix[1], (float)matrix[2], (float)matrix[3],
                    (float)matrix[4], (float)matrix[5], (float)matrix[6], (float)matrix[7],
                    (float)matrix[8], (float)matrix[9], (float)matrix[10], (float)matrix[11],
                    (float)matrix[12], (float)matrix[13], (float)matrix[14], (float)matrix[15]);
                brushRenderer.World = Microsoft.Xna.Framework.Matrix.CreateScale((float)Zoom);

                GlobalGraphics.GraphicsDevice.BlendFactor = Microsoft.Xna.Framework.Color.White;
                GlobalGraphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

                brushRenderer.RenderWireframe();

                GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            }
        }
    }
}
