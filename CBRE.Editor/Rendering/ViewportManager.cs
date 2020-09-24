using System;
using System.Collections.Generic;
using System.Text;
using CBRE.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Rendering {
    public enum MouseButtons {
        Left = 0x1,
        Right = 0x2,
        Middle = 0x4,
        Mouse4 = 0x8,
        Mouse5 = 0x10
    }

    public class ViewportEvent : EventArgs {
        public bool Handled { get; set; }

        // Key
        //public Keys Modifiers { get; set; }
        public bool Control { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        
        public Keys KeyCode { get; set; }
        public int KeyValue { get; set; }
        public char KeyChar { get; set; }

        // Mouse
        public MouseButtons Button { get; set; }
        public int Clicks { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Delta { get; set; }

        public Vector3 Location => new Vector3(X, Y, 0);

        // Mouse movement
        public int LastX { get; set; }
        public int LastY { get; set; }

        public int DeltaX => X - LastX;
        public int DeltaY => Y - LastY;

        // Click and drag
        public bool Dragging { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }

        // 2D Camera
        public Vector3 CameraPosition { get; set; }
        public decimal CameraZoom { get; set; }
    }

    static class ViewportManager {
        static ViewportBase[] Viewports = new ViewportBase[4];
        static Vector2 splitPoint = new Vector2(0.45f, 0.55f);
        static VertexPositionColor[] backgroundVertices = new VertexPositionColor[12];
        static short[] backgroundIndices = new short[18];
        static BasicEffect backgroundEffect = null;

        static bool prevMouseDown; static bool draggingX; static bool draggingY;

        public static bool Ctrl { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Alt { get; private set; }

        public static void Init() {
            prevMouseDown = false;
            draggingX = false;
            draggingY = false;

            Viewports[0] = new Viewport3D(Viewport3D.ViewType.Shaded);
            Viewports[1] = new Viewport2D(Viewport2D.ViewDirection.Top);
            Viewports[2] = new Viewport2D(Viewport2D.ViewDirection.Side);
            Viewports[3] = new Viewport2D(Viewport2D.ViewDirection.Front);

            backgroundEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            backgroundEffect.World = Matrix.Identity;
            backgroundEffect.View = Matrix.Identity;
            backgroundEffect.TextureEnabled = false;
            backgroundEffect.VertexColorEnabled = true;

            backgroundVertices[0] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Black };
            backgroundVertices[1] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Black };
            backgroundVertices[2] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Black };
            backgroundVertices[3] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Black };

            backgroundVertices[4] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.White };
            backgroundVertices[5] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.White };
            backgroundVertices[6] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Gray };
            backgroundVertices[7] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Gray };

            backgroundVertices[8] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.White };
            backgroundVertices[9] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.White };
            backgroundVertices[10] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Gray };
            backgroundVertices[11] = new VertexPositionColor() { Position = new Vector3(46, 47, 0), Color = Color.Gray };

            backgroundIndices[0] = 0;
            backgroundIndices[1] = 1;
            backgroundIndices[2] = 2;
            backgroundIndices[3] = 1;
            backgroundIndices[4] = 2;
            backgroundIndices[5] = 3;

            backgroundIndices[6] = 4;
            backgroundIndices[7] = 5;
            backgroundIndices[8] = 6;
            backgroundIndices[9] = 5;
            backgroundIndices[10] = 6;
            backgroundIndices[11] = 7;

            backgroundIndices[12] = 8;
            backgroundIndices[13] = 9;
            backgroundIndices[14] = 10;
            backgroundIndices[15] = 9;
            backgroundIndices[16] = 10;
            backgroundIndices[17] = 11;
        }

        public static void Render() {
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            Ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            Shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            Alt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

            var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;
            int splitX = (int)((GlobalGraphics.Window.ClientBounds.Width - 46.0f) * splitPoint.X) + 46;
            int splitY = (int)((GlobalGraphics.Window.ClientBounds.Height - 47.0f) * splitPoint.Y) + 47;

            if (!mouseDown) { draggingX = false; draggingY = false; }
            if (mouseDown && !prevMouseDown) {
                draggingX = (mouseState.X >= (splitX - 3)) && (mouseState.X <= (splitX + 2));
                draggingY = (mouseState.Y >= (splitY - 3)) && (mouseState.Y <= (splitY + 2));
            }

            if (draggingX) {
                splitPoint.X = (mouseState.X - 46.0f) / (GlobalGraphics.Window.ClientBounds.Width - 46.0f);
                splitPoint.X = Math.Clamp(splitPoint.X, 0.01f, 0.99f);
            }
            if (draggingY) {
                splitPoint.Y = (mouseState.Y - 47.0f) / (GlobalGraphics.Window.ClientBounds.Height - 47.0f);
                splitPoint.Y = Math.Clamp(splitPoint.Y, 0.01f, 0.99f);
            }

            backgroundEffect.Projection = Matrix.CreateOrthographicOffCenter(0.5f, GlobalGraphics.Window.ClientBounds.Width + 0.5f, GlobalGraphics.Window.ClientBounds.Height + 0.5f, 0.5f, -1f, 1f);

            backgroundVertices[1].Position.X = GlobalGraphics.Window.ClientBounds.Width;
            backgroundVertices[2].Position.Y = GlobalGraphics.Window.ClientBounds.Height;
            backgroundVertices[3].Position.X = GlobalGraphics.Window.ClientBounds.Width;
            backgroundVertices[3].Position.Y = GlobalGraphics.Window.ClientBounds.Height;

            backgroundVertices[4].Position.X = splitX - 3;
            backgroundVertices[5].Position.X = splitX + 2;
            backgroundVertices[6].Position.X = splitX - 3;
            backgroundVertices[7].Position.X = splitX + 2;
            backgroundVertices[6].Position.Y = GlobalGraphics.Window.ClientBounds.Height;
            backgroundVertices[7].Position.Y = GlobalGraphics.Window.ClientBounds.Height;

            backgroundVertices[8].Position.Y = splitY - 3;
            backgroundVertices[9].Position.Y = splitY - 3;
            backgroundVertices[10].Position.Y = splitY + 2;
            backgroundVertices[11].Position.Y = splitY + 2;
            backgroundVertices[9].Position.X = GlobalGraphics.Window.ClientBounds.Width;
            backgroundVertices[11].Position.X = GlobalGraphics.Window.ClientBounds.Width;

            GlobalGraphics.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, GlobalGraphics.Window.ClientBounds.Width, GlobalGraphics.Window.ClientBounds.Height);
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            backgroundEffect.CurrentTechnique.Passes[0].Apply();
            GlobalGraphics.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, backgroundVertices, 0, 12, backgroundIndices, 0, 6);

            for (int i=0;i<Viewports.Length;i++) {
                if (Viewports[i] == null) { return; }
                bool left = i % 2 == 0;
                bool top = i < 2;

                Viewports[i].X = left ? 46 : splitX + 3;
                Viewports[i].Y = top ? 47 : splitY + 3;
                Viewports[i].Width = left ? splitX-50 : GlobalGraphics.Window.ClientBounds.Width - (splitX + 3);
                Viewports[i].Height = top ? splitY-51 : GlobalGraphics.Window.ClientBounds.Height - (splitY + 3);

                GlobalGraphics.GraphicsDevice.Viewport = new Viewport(Viewports[i].X, Viewports[i].Y, Viewports[i].Width, Viewports[i].Height);
                Viewports[i].Render();
            }
            GlobalGraphics.GraphicsDevice.Viewport = prevViewport;

            prevMouseDown = mouseDown;
        }
    }
}
