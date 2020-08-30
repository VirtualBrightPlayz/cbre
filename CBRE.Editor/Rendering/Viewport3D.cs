using System;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;

namespace CBRE.Editor.Rendering {
    public class Viewport3D : ViewportBase {
        public enum ViewType {
            /// <summary>
            /// Renders textures and shaded solids with lightmaps if available
            /// </summary>
            Lightmapped,

            /// <summary>
            /// Renders textured and shaded solids
            /// </summary>
            Textured,

            /// <summary>
            /// Renders shaded solids
            /// </summary>
            Shaded,

            /// <summary>
            /// Renders flat solids
            /// </summary>
            Flat,

            /// <summary>
            /// Renders wireframe solids
            /// </summary>
            Wireframe
        }

        public Camera Camera { get; set; }
        public ViewType Type { get; set; }

        public Viewport3D(ViewType type) {
            Type = type;
            Camera = new Camera();
        }

        public override void FocusOn(Box box) {
            var dist = System.Math.Max(System.Math.Max(box.Width, box.Length), box.Height);
            var normal = Camera.EyePosition - Camera.LookPosition;
            var v = new Vector(new Vector3(normal.X, normal.Y, normal.Z), dist);
            FocusOn(box.Center, new Vector3(v.X, v.Y, v.Z));
        }

        public override void FocusOn(Vector3 coordinate) {
            FocusOn(coordinate, Vector3.UnitY * -100);
        }

        public void FocusOn(Vector3 coordinate, Vector3 distance) {
            var pos = coordinate + distance;
            Camera.EyePosition = new Vector3(pos.X, pos.Y, pos.Z);
            Camera.LookPosition = new Vector3(coordinate.X, coordinate.Y, coordinate.Z);
        }

        public override Matrix GetViewportMatrix() {
            const float near = 0.1f;
            var ratio = Width / (float)Height;
            if (ratio <= 0) ratio = 1;
            throw new NotImplementedException();
            //return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FOV), ratio, near, Camera.ClipDistance);
        }

        public override Matrix GetCameraMatrix() {
            throw new NotImplementedException();
            //return Matrix4.LookAt(Camera.Location, Camera.LookAt, Vector3.UnitZ);
        }

        protected override void UpdateBeforeClearViewport() {
            throw new NotImplementedException();
            base.UpdateBeforeClearViewport();
        }

        protected override void UpdateAfterRender() {
            base.UpdateAfterRender();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert a screen space coordinate into a world space coordinate.
        /// The resulting coordinate will be quite a long way from the camera.
        /// </summary>
        /// <param name="screen">The screen coordinate (with Y in OpenGL space)</param>
        /// <returns>The world coordinate</returns>
        public Vector3 ScreenToWorld(Vector3 screen) {
            screen = new Vector3(screen.X, screen.Y, 1);
            var viewport = new[] { 0, 0, Width, Height };
            throw new NotImplementedException();
            /*
            var pm = Matrix4d.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FOV), Width / (float)Height, 0.1f, 50000);
            var vm = Matrix4d.LookAt(
                new Vector3d(Camera.Location.X, Camera.Location.Y, Camera.Location.Z),
                new Vector3d(Camera.LookAt.X, Camera.LookAt.Y, Camera.LookAt.Z),
                Vector3d.UnitZ);
            return MathFunctions.Unproject(screen, viewport, pm, vm);*/
        }

        /// <summary>
        /// Convert a world space coordinate into a screen space coordinate.
        /// </summary>
        /// <param name="world">The world coordinate</param>
        /// <returns>The screen coordinate</returns>
        public Vector3 WorldToScreen(Vector3 world) {
            var viewport = new[] { 0, 0, Width, Height };
            throw new NotImplementedException();
            /*
            var pm = Matrix4d.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FOV), Width / (float)Height, 0.1f, 50000);
            var vm = Matrix4d.LookAt(
                new Vector3d(Camera.Location.X, Camera.Location.Y, Camera.Location.Z),
                new Vector3d(Camera.LookAt.X, Camera.LookAt.Y, Camera.LookAt.Z),
                Vector3d.UnitZ);
            return MathFunctions.Project(world, viewport, pm, vm);*/
        }

        /// <summary>
        /// Project the 2D coordinates from the screen coordinates outwards
        /// from the camera along the lookat vector, taking the frustrum
        /// into account. The resulting line will be run from the camera
        /// position along the view axis and end at the back clipping pane.
        /// </summary>
        /// <param name="x">The X coordinate on screen</param>
        /// <param name="y">The Y coordinate on screen</param>
        /// <returns>A line beginning at the camera location and tracing
        /// along the 3D projection for at least 1,000,000 units.</returns>
        public Line CastRayFromScreen(int x, int y) {
            var near = new Vector3(x, Height - y, 0);
            var far = new Vector3(x, Height - y, 1);
            throw new NotImplementedException();
            /*
            var pm = Matrix4d.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FOV), Width / (float)Height, 0.1f, 50000);
            var vm = Matrix4d.LookAt(
                new Vector3d(Camera.Location.X, Camera.Location.Y, Camera.Location.Z),
                new Vector3d(Camera.LookAt.X, Camera.LookAt.Y, Camera.LookAt.Z),
                Vector3d.UnitZ);
            var viewport = new[] { 0, 0, Width, Height };
            var un = MathFunctions.Unproject(near, viewport, pm, vm);
            var uf = MathFunctions.Unproject(far, viewport, pm, vm);
            return (un == null || uf == null) ? null : new Line(un, uf);*/
        }
    }
}
