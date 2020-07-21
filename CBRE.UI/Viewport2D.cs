using CBRE.DataStructures.Geometric;
using CBRE.Extensions;
using CBRE.Graphics;
using CBRE.Graphics.Helpers;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Linq;

namespace CBRE.UI {
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

        private static readonly Matrix4 TopMatrix = Matrix4.Identity;
        private static readonly Matrix4 FrontMatrix = new Matrix4(Vector4.UnitZ, Vector4.UnitX, Vector4.UnitY, Vector4.UnitW);
        private static readonly Matrix4 SideMatrix = new Matrix4(Vector4.UnitX, Vector4.UnitZ, Vector4.UnitY, Vector4.UnitW);

        private static Matrix4 GetMatrixFor(ViewDirection dir) {
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
                Listeners.OfType<IViewport2DEventListener>()
                    .ToList().ForEach(l => l.PositionChanged(old, _position));
            }
        }

        private decimal _zoom;
        public decimal Zoom {
            get { return _zoom; }
            set {
                var old = _zoom;
                _zoom = DMath.Clamp(value, 0.001m, 10000m);
                Listeners.OfType<IViewport2DEventListener>()
                    .ToList().ForEach(l => l.ZoomChanged(old, _zoom));
            }
        }

        private Vector3 CenterScreen { get; set; }

        public Viewport2D(ViewDirection direction) {
            Zoom = 1;
            Position = new Vector3(0, 0, 0);
            Direction = direction;
            CenterScreen = new Vector3(Width / 2m, Height / 2m, 0);
        }

        public Viewport2D(ViewDirection direction, RenderContext context) : base(context) {
            Zoom = 1;
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

        public override void SetViewport() {
            base.SetViewport();
            Viewport.Orthographic(0, 0, Width, Height, -50000, 50000);
        }

        public override Matrix4 GetViewportMatrix() {
            const float near = -1000000;
            const float far = 1000000;
            return Matrix4.CreateOrthographic(Width, Height, near, far);
        }

        public override Matrix4 GetCameraMatrix() {
            var translate = Matrix4.CreateTranslation((float)-Position.X, (float)-Position.Y, 0);
            var scale = Matrix4.CreateScale(new Vector3((float)Zoom, (float)Zoom, 0));
            return translate * scale;
        }

        public override Matrix4 GetModelViewMatrix() {
            return GetMatrixFor(Direction);
        }

        protected override void OnResize(EventArgs e) {
            CenterScreen = new Vector3(Width / 2m, Height / 2m, 0);
            base.OnResize(e);
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
            GL.Scale(new Vector3((float)Zoom, (float)Zoom, 0));
            GL.Translate((float)-Position.X, (float)-Position.Y, 0);
            base.UpdateBeforeRender();
        }

        protected override void UpdateAfterRender() {
            Listeners.ForEach(x => x.Render2D());
            base.UpdateAfterRender();
        }
    }
}
