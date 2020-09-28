using System;
using System.Collections.Generic;
using System.Text;
using CBRE.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Rendering {
    public enum MouseButtons {
        None = 0x0,
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

        public static RenderTarget2D renderTarget { get; private set; }
        private static VertexPositionTexture[] renderTargetGeom;
        static BasicEffect renderTargetEffect = null;

        public static bool Ctrl { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Alt { get; private set; }


        readonly static Point vpStartPoint = new Point(46, 66);

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

            renderTargetEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            renderTargetEffect.World = Matrix.Identity;
            renderTargetEffect.View = Matrix.Identity;
            renderTargetEffect.TextureEnabled = true;
            renderTargetEffect.VertexColorEnabled = false;

            backgroundVertices[0] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };
            backgroundVertices[1] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };
            backgroundVertices[2] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };
            backgroundVertices[3] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };

            backgroundVertices[4] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[5] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[6] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };
            backgroundVertices[7] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };

            backgroundVertices[8] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[9] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[10] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };
            backgroundVertices[11] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };

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

            RebuildRenderTarget();

            AsyncTexture.LoadCallback = MarkForRerender;
        }

        private static int knownWindowWidth = 0;
        private static int knownWindowHeight = 0;

        private static void RebuildRenderTarget() {
            renderTargetGeom = new VertexPositionTexture[] {
                new VertexPositionTexture(new Vector3(vpStartPoint.X, vpStartPoint.Y, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(GlobalGraphics.Window.ClientBounds.Width, vpStartPoint.Y, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(vpStartPoint.X, GlobalGraphics.Window.ClientBounds.Height, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(GlobalGraphics.Window.ClientBounds.Width, GlobalGraphics.Window.ClientBounds.Height, 0), new Vector2(1, 1)),
            };
            knownWindowWidth = GlobalGraphics.Window.ClientBounds.Width;
            knownWindowHeight = GlobalGraphics.Window.ClientBounds.Height;
            renderTargetEffect.Projection = Matrix.CreateOrthographicOffCenter(0.5f, GlobalGraphics.Window.ClientBounds.Width + 0.5f, GlobalGraphics.Window.ClientBounds.Height + 0.5f, 0.5f, -1f, 1f);
            renderTarget?.Dispose();
            renderTarget = new RenderTarget2D(GlobalGraphics.GraphicsDevice, GlobalGraphics.Window.ClientBounds.Width - vpStartPoint.X, GlobalGraphics.Window.ClientBounds.Height - vpStartPoint.Y, false, SurfaceFormat.Color, DepthFormat.Depth24);

            int splitX = (int)((GlobalGraphics.Window.ClientBounds.Width - vpStartPoint.X) * splitPoint.X) + vpStartPoint.X;
            int splitY = (int)((GlobalGraphics.Window.ClientBounds.Height - vpStartPoint.Y) * splitPoint.Y) + vpStartPoint.Y;
            for (int i = 0; i < Viewports.Length; i++) {
                if (Viewports[i] == null) { continue; }
                bool left = i % 2 == 0;
                bool top = i < 2;

                Viewports[i].X = left ? vpStartPoint.X : splitX + 3;
                Viewports[i].Y = top ? vpStartPoint.Y : splitY + 3;
                Viewports[i].Width = left ? splitX - vpStartPoint.X - 4 : renderTarget.Width - (splitX - vpStartPoint.X + 3);
                Viewports[i].Height = top ? splitY - vpStartPoint.Y - 4 : renderTarget.Height - (splitY - vpStartPoint.Y + 3);
            }

            Render();
        }

        private static bool shouldRerender = false;

        public static void MarkForRerender() {
            shouldRerender = true;
        }

        public static void Update() {
            if (knownWindowWidth != GlobalGraphics.Window.ClientBounds.Width || knownWindowHeight != GlobalGraphics.Window.ClientBounds.Height) {
                RebuildRenderTarget();
            }
            if (shouldRerender) { Render(); }

            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            bool mouseHit = mouseDown && !prevMouseDown;
            Ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            Shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            Alt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

            int splitX = (int)((GlobalGraphics.Window.ClientBounds.Width - vpStartPoint.X) * splitPoint.X) + vpStartPoint.X;
            int splitY = (int)((GlobalGraphics.Window.ClientBounds.Height - vpStartPoint.Y) * splitPoint.Y) + vpStartPoint.Y;

            if (!mouseDown) { draggingX = false; draggingY = false; }
            if (mouseHit) {
                draggingX = (mouseState.X >= (splitX - 3)) && (mouseState.X <= (splitX + 2));
                draggingY = (mouseState.Y >= (splitY - 3)) && (mouseState.Y <= (splitY + 2));
            }

            if (draggingX) {
                splitPoint.X = (float)(mouseState.X - vpStartPoint.X) / (GlobalGraphics.Window.ClientBounds.Width - vpStartPoint.X);
                splitPoint.X = Math.Clamp(splitPoint.X, 0.01f, 0.99f);
                MarkForRerender();
            }
            if (draggingY) {
                splitPoint.Y = (float)(mouseState.Y - vpStartPoint.Y) / (GlobalGraphics.Window.ClientBounds.Height - vpStartPoint.Y);
                splitPoint.Y = Math.Clamp(splitPoint.Y, 0.01f, 0.99f);
                MarkForRerender();
            }

            for (int i = 0; i < Viewports.Length; i++) {
                if (Viewports[i] == null) { continue; }
                bool left = i % 2 == 0;
                bool top = i < 2;

                Viewports[i].X = left ? vpStartPoint.X : splitX + 3;
                Viewports[i].Y = top ? vpStartPoint.Y : splitY + 3;
                Viewports[i].Width = left ? splitX - vpStartPoint.X - 4 : renderTarget.Width - (splitX - vpStartPoint.X + 3);
                Viewports[i].Height = top ? splitY - vpStartPoint.Y - 4 : renderTarget.Height - (splitY - vpStartPoint.Y + 3);

                if (mouseState.X > Viewports[i].X && mouseState.Y > Viewports[i].Y &&
                    mouseState.X < (Viewports[i].X + Viewports[i].Width) && mouseState.Y < (Viewports[i].Y + Viewports[i].Height)) {
                    if (mouseHit) {
                        GameMain.Instance.SelectedTool?.MouseClick(Viewports[i], new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Left,
                            X = mouseState.X - Viewports[i].X,
                            Y = mouseState.Y - Viewports[i].Y
                        });
                    } else if (mouseDown) {
                        GameMain.Instance.SelectedTool?.MouseDown(Viewports[i], new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Left,
                            X = mouseState.X - Viewports[i].X,
                            Y = mouseState.Y - Viewports[i].Y
                        });
                    }
                }
            }

            prevMouseDown = mouseDown;
        }

        public static void Render() {
            shouldRerender = false;

            int splitX = (int)((GlobalGraphics.Window.ClientBounds.Width - vpStartPoint.X) * splitPoint.X);
            int splitY = (int)((GlobalGraphics.Window.ClientBounds.Height - vpStartPoint.Y) * splitPoint.Y);

            GlobalGraphics.GraphicsDevice.SetRenderTarget(renderTarget);
            GlobalGraphics.GraphicsDevice.Clear(Color.Black);

            var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;

            backgroundEffect.Projection = Matrix.CreateOrthographicOffCenter(0.5f, renderTarget.Width + 0.5f, renderTarget.Height + 0.5f, 0.5f, -1f, 1f);

            backgroundVertices[1].Position.X = renderTarget.Width;
            backgroundVertices[2].Position.Y = renderTarget.Height;
            backgroundVertices[3].Position.X = renderTarget.Width;
            backgroundVertices[3].Position.Y = renderTarget.Height;

            backgroundVertices[4].Position.X = splitX - 3;
            backgroundVertices[5].Position.X = splitX + 2;
            backgroundVertices[6].Position.X = splitX - 3;
            backgroundVertices[7].Position.X = splitX + 2;
            backgroundVertices[6].Position.Y = renderTarget.Height;
            backgroundVertices[7].Position.Y = renderTarget.Height;

            backgroundVertices[8].Position.Y = splitY - 3;
            backgroundVertices[9].Position.Y = splitY - 3;
            backgroundVertices[10].Position.Y = splitY + 2;
            backgroundVertices[11].Position.Y = splitY + 2;
            backgroundVertices[9].Position.X = renderTarget.Width;
            backgroundVertices[11].Position.X = renderTarget.Width;

            GlobalGraphics.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, renderTarget.Width, renderTarget.Height);
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            backgroundEffect.CurrentTechnique.Passes[0].Apply();
            GlobalGraphics.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, backgroundVertices, 0, 12, backgroundIndices, 0, 6);

            for (int i=0;i<Viewports.Length;i++) {
                if (Viewports[i] == null) { continue; }

                GlobalGraphics.GraphicsDevice.Viewport = new Viewport(Viewports[i].X - vpStartPoint.X, Viewports[i].Y - vpStartPoint.Y, Viewports[i].Width, Viewports[i].Height);

                Viewports[i].Render();
            }
            GlobalGraphics.GraphicsDevice.Viewport = prevViewport;

            GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
        }

        public static void DrawRenderTarget() {
            var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;
            GlobalGraphics.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, GlobalGraphics.Window.ClientBounds.Width, GlobalGraphics.Window.ClientBounds.Height);
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            renderTargetEffect.Texture = renderTarget;
            renderTargetEffect.CurrentTechnique.Passes[0].Apply();
            GlobalGraphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, renderTargetGeom, 0, 2, VertexPositionTexture.VertexDeclaration);
            GlobalGraphics.GraphicsDevice.Viewport = prevViewport;
        }
    }
}
