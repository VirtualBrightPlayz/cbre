using System;
using System.Drawing;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Num = System.Numerics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CBRE.Editor.Popup {
    public class ViewportWindow : WindowUI
    {
        public ViewportBase viewport { get; private set; }
        public Rectangle view { get; private set; }
        public RenderTarget2D renderTarget { get; private set; }
        public IntPtr renderTargetPtr { get; private set; }
        public BasicEffect basicEffect { get; private set; }
        public string name { get; private set; }

        public ViewportWindow(ViewportBase view) : base("") {
            viewport = view;
            basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            name = Guid.NewGuid().ToString();
            // ResetRenderTarget();
        }

        public void ResetRenderTarget() {
            if (renderTargetPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(renderTargetPtr);
            }
            renderTarget?.Dispose();
            renderTarget = new RenderTarget2D(GlobalGraphics.GraphicsDevice, view.Width, view.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            renderTargetPtr = GlobalGraphics.ImGuiRenderer.BindTexture(renderTarget);
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Begin(name, ref open)) {
                // ImGui.SetWindowPos(new Num.Vector2(47, 47), ImGuiCond.FirstUseEver);
                // ImGui.SetWindowSize(new Num.Vector2(ViewportManager.vpRect.Right - 47, 30), ImGuiCond.FirstUseEver);
                Num.Vector2 pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
                Num.Vector2 siz = ImGui.GetWindowSize() - ImGui.GetCursorPos() * 1.5f;
                Rectangle tmpview = new Rectangle((int)pos.X, (int)pos.Y, (int)siz.X + (int)pos.X, (int)siz.Y + (int)pos.Y);
                if (tmpview != view) {
                    view = tmpview;
                    ResetRenderTarget();
                }
                GlobalGraphics.GraphicsDevice.SetRenderTarget(renderTarget);
                GlobalGraphics.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

                var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;

                basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0.5f, renderTarget.Width + 0.5f, renderTarget.Height + 0.5f, 0.5f, -1f, 1f);
                basicEffect.View = Microsoft.Xna.Framework.Matrix.Identity;
                basicEffect.World = Microsoft.Xna.Framework.Matrix.Identity;
                ViewportManager.Render(viewport, new Viewport(view));
                GlobalGraphics.GraphicsDevice.Viewport = prevViewport;
                GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
                if (ImGui.BeginChildFrame(3, new Num.Vector2(view.Size.X, view.Size.Y))) {
                    ImGui.Image(renderTargetPtr, new Num.Vector2(view.Size.X, view.Size.Y));
                }
                ImGui.EndChildFrame();
            }
            ImGui.End();
            return open;
        }

        public override void Close() {
            base.Close();
            basicEffect.Dispose();
            if (renderTargetPtr != null) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(renderTargetPtr);
            }
            renderTarget?.Dispose();
        }
    }
}