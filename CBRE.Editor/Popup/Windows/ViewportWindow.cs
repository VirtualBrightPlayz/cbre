using System;
using System.Drawing;
using System.Linq;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        private bool selected = false;

        public ViewportWindow(ViewportBase view) : base("") {
            viewport = view;
            basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            name = Guid.NewGuid().ToString();
        }

        public ViewportWindow(int viewid) : base("") {
            viewport = ViewportManager.Viewports[viewid];
            basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            name = viewid.ToString();
        }

        public void ResetRenderTarget() {
            if (view.Width <= 0 && view.Height <= 0) { return; }
            if (renderTargetPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(renderTargetPtr);
                renderTargetPtr = IntPtr.Zero;
            }
            renderTarget?.Dispose();
            renderTarget = new RenderTarget2D(GlobalGraphics.GraphicsDevice, view.Width, view.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);

            ViewportManager.Render(viewport, new Viewport(0, 0, renderTarget.Width, renderTarget.Height), renderTarget);
            renderTargetPtr = GlobalGraphics.ImGuiRenderer.BindTexture(renderTarget);
        }

        public virtual bool IsOverAndOpen(MouseState mouseState) {
            return (selected && mouseState.X >= -view.Left && mouseState.X <= view.Right && mouseState.Y >= -view.Top && mouseState.Y <= view.Bottom);
        }

        protected override bool ImGuiLayout() {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;
            if (ImGui.Begin(name, ref open, flags)) {
                Num.Vector2 pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
                Num.Vector2 siz = ImGui.GetWindowSize() - ImGui.GetCursorPos() * 1.5f;
                Rectangle tmpview = new Rectangle((int)pos.X, (int)pos.Y, (int)siz.X, (int)siz.Y);
                if (!view.Equals(tmpview)) {
                    view = tmpview;
                    ResetRenderTarget();
                }
                view = tmpview;
                if (ImGui.BeginChildFrame(3, new Num.Vector2(view.Size.X, view.Size.Y), flags) && renderTargetPtr != IntPtr.Zero && open) {
                    ImGui.Image(renderTargetPtr, new Num.Vector2(view.Size.X, view.Size.Y));
                }
                ImGui.EndChildFrame();
                selected = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);
            }
            ImGui.End();
            return open;
        }

        public override void Close() {
            base.Close();
            basicEffect.Dispose();
            if (renderTargetPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(renderTargetPtr);
            }
            renderTarget?.Dispose();
        }
    }
}