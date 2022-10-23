using System;
using System.Drawing;
using System.Linq;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public partial class ViewportWindow {
        protected void UpdateSubView(int index) {
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            var keysDown = keyboardState.GetPressedKeys();
            bool mouse1Down = mouseState.LeftButton == ButtonState.Pressed;
            bool mouse1Hit = mouse1Down && !prevMouse1Down;
            bool mouse2Down = mouseState.RightButton == ButtonState.Pressed;
            bool mouse2Hit = mouse2Down && !prevMouse2Down;
            bool mouse3Down = mouseState.MiddleButton == ButtonState.Pressed;
            bool mouse3Hit = mouse3Down && !prevMouse3Down;
            int scrollWheelValue = mouseState.ScrollWheelValue;

            var mousePos = mouseState.Position;

            int i = index;

            // for (int i = 0; i < Viewports.Length; i++) {
                var viewport = Viewports[i];

                bool mouseOver = RenderTargetSelected[i];
                if (mouseOver) {
                    if (!mouse1Down && prevMouse1Down) {
                        GameMain.Instance.SelectedTool?.MouseLifted(viewport, new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Left,
                            X = mouseState.X - viewport.X,
                            Y = mouseState.Y - viewport.Y,
                            LastX = viewport.PrevMouseX,
                            LastY = viewport.PrevMouseY,
                        });
                        ViewportManager.MarkForRerender();
                        focusedViewport = -1;
                    }
                    if (!mouse2Down && prevMouse2Down) {
                        GameMain.Instance.SelectedTool?.MouseLifted(viewport, new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Right,
                            X = mouseState.X - viewport.X,
                            Y = mouseState.Y - viewport.Y,
                            LastX = viewport.PrevMouseX,
                            LastY = viewport.PrevMouseY,
                        });
                        ViewportManager.MarkForRerender();
                        focusedViewport = -1;
                    }

                    GameMain.Instance.SelectedTool?.UpdateFrame(viewport,
                        new FrameInfo(0)); //TODO: fix FrameInfo

                    foreach (var key in keysDown.Where(k => !prevKeysDown.Contains(k))) {
                        GameMain.Instance.SelectedTool?.KeyHit(viewport, new ViewportEvent() {
                            Handled = false,
                            KeyCode = key,
                            MouseOver = mouseOver,
                            Control = ViewportManager.Ctrl,
                            Alt = ViewportManager.Alt,
                            Shift = ViewportManager.Shift
                        });
                        foreach (var tool in GameMain.Instance.ToolBarItems.Select(tbi => tbi.Tool)) {
                            tool.KeyHitBackground(viewport, new ViewportEvent() {
                                Handled = false,
                                KeyCode = key,
                                MouseOver = mouseOver,
                                Control = ViewportManager.Ctrl,
                                Alt = ViewportManager.Alt,
                                Shift = ViewportManager.Shift
                            });
                        }
                    }

                    foreach (var key in prevKeysDown.Where(k => !keysDown.Contains(k))) {
                        GameMain.Instance.SelectedTool?.KeyLift(viewport, new ViewportEvent() {
                            Handled = false,
                            KeyCode = key
                        });
                        foreach (var tool in GameMain.Instance.ToolBarItems.Select(tbi => tbi.Tool)) {
                            tool.KeyUpBackground(viewport, new ViewportEvent() {
                                Handled = false,
                                KeyCode = key
                            });
                        }
                    }

                    if (viewport is Viewport3D vp3d && !ViewportManager.AnyModifiers) {
                        bool shiftDown = mouse2Down;
                        bool mustRerender = false;
                        // WASD
                        if (keyboardState.IsKeyDown(Keys.A)) {
                            vp3d.Camera.Strafe(-5m - (shiftDown ? 5m : 0m));
                            mustRerender = true;
                        }

                        if (keyboardState.IsKeyDown(Keys.D)) {
                            vp3d.Camera.Strafe(5m + (shiftDown ? 5m : 0m));
                            mustRerender = true;
                        }

                        if (keyboardState.IsKeyDown(Keys.W)) {
                            vp3d.Camera.Advance(5m + (shiftDown ? 5m : 0m));
                            mustRerender = true;
                        }

                        if (keyboardState.IsKeyDown(Keys.S)) {
                            vp3d.Camera.Advance(-5m - (shiftDown ? 5m : 0m));
                            mustRerender = true;
                        }

                        // look around
                        var fovdiv = (vp3d.Width / 60m) / 2.5m;
                        if (keyboardState.IsKeyDown(Keys.Left)) {
                            vp3d.Camera.Pan(5m / fovdiv);
                            mustRerender = true;
                        }

                        if (keyboardState.IsKeyDown(Keys.Right)) {
                            vp3d.Camera.Pan(-5m / fovdiv);
                            mustRerender = true;
                        }

                        if (keyboardState.IsKeyDown(Keys.Up)) {
                            vp3d.Camera.Tilt(-5m / fovdiv);
                            mustRerender = true;
                        }

                        if (keyboardState.IsKeyDown(Keys.Down)) {
                            vp3d.Camera.Tilt(5m / fovdiv);
                            mustRerender = true;
                        }

                        if (mustRerender) {
                            var map = Documents.DocumentManager.CurrentDocument.Map;
                            if (map.ActiveCamera == null) { map.ActiveCamera = map.Cameras.FirstOrDefault(); }

                            if (map.ActiveCamera != null) {
                                map.ActiveCamera.EyePosition = vp3d.Camera.EyePosition;
                                map.ActiveCamera.LookPosition = vp3d.Camera.LookPosition;
                            }

                            ViewportManager.MarkForRerender();
                        }
                    }

                    int currMouseX = mouseState.X - viewport.X;
                    int currMouseY = mouseState.Y - viewport.Y;
                    if (viewport.PrevMouseX != currMouseX ||
                        viewport.PrevMouseY != currMouseY) {
                        var ev = new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Left,
                            X = currMouseX,
                            Y = currMouseY,
                            LastX = viewport.PrevMouseX,
                            LastY = viewport.PrevMouseY,
                        };
                        GameMain.Instance.SelectedTool?.MouseMove(viewport, ev);
                        foreach (var tool in GameMain.Instance.ToolBarItems.Select(tbi => tbi.Tool)) {
                            tool.MouseMoveBackground(viewport, ev);
                        }
                        ViewportManager.MarkForRerender();
                    }

                    if (mouse1Hit) {
                        GameMain.Instance.SelectedTool?.MouseClick(viewport, new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Left,
                            X = currMouseX,
                            Y = currMouseY,
                            LastX = viewport.PrevMouseX,
                            LastY = viewport.PrevMouseY,
                            Clicks = 1
                        });

                        focusedViewport = i;
                    }

                    if (mouse2Hit) {
                        GameMain.Instance.SelectedTool?.MouseClick(viewport, new ViewportEvent() {
                            Handled = false,
                            Button = MouseButtons.Right,
                            X = currMouseX,
                            Y = currMouseY,
                            LastX = viewport.PrevMouseX,
                            LastY = viewport.PrevMouseY,
                            Clicks = 1
                        });
                    }

                    if (mouse3Down) {
                        if (viewport is Viewport2D vp) {
                            ViewportManager.MarkForRerender();
                            vp.Position -= new DataStructures.Geometric.Vector3(
                                (decimal)(currMouseX - vp.PrevMouseX) / vp.Zoom,
                                -(decimal)(currMouseY - vp.PrevMouseY) / vp.Zoom, 0m);
                        }
                    }


                    if (scrollWheelValue != prevScrollWheelValue) {
                        if (viewport is Viewport2D vp) {
                            var pos0 = vp.ScreenToWorld(new Point(mouseState.X - viewport.X,
                                mouseState.Y - viewport.Y));
                            decimal scrollWheelDiff = (scrollWheelValue - prevScrollWheelValue) * 0.001m;
                            if (scrollWheelDiff > 0m) {
                                vp.Zoom *= 1.0m + scrollWheelDiff;
                            } else {
                                vp.Zoom /= 1.0m - scrollWheelDiff;
                            }

                            var pos1 = vp.ScreenToWorld(new Point(mouseState.X - viewport.X,
                                mouseState.Y - viewport.Y));
                            vp.Position -=
                                new DataStructures.Geometric.Vector3(pos1.X - pos0.X, pos0.Y - pos1.Y, 0m);
                            ViewportManager.MarkForRerender();
                        }
                    }
                }

                //Reset the mouse state since the tool update methods can change it!
                mouseState = Mouse.GetState();
                viewport.PrevMouseOver = mouseOver;
                viewport.PrevMouseX = mouseState.X - viewport.X;
                viewport.PrevMouseY = mouseState.Y - viewport.Y;
            // }
        }

        protected void RenderSubView(int index, bool image) {
            // var siz = GetXnaRectangle(index);
            var siz = GetXnaViewport(index);
            if (FullscreenViewport == -1)
                ImGui.SetCursorPos(new Num.Vector2(siz.X, siz.Y));
            else
                ImGui.SetCursorPos(Num.Vector2.Zero);
            // if (ImGui.BeginChildFrame(ImGui.GetID(((uint)(index /*+ (image ? 0 : Viewports.Length + 1)*/)).ToString()), new Num.Vector2(siz.Width, siz.Height), flags /*| (image ? ImGuiWindowFlags.NoInputs : ImGuiWindowFlags.None)*/) && RenderTargetImGuiPtr[index] != IntPtr.Zero) {
                ImGui.Image(RenderTargetImGuiPtr[index], new Num.Vector2(siz.Width, siz.Height));
                RenderTargetSelected[index] = ImGui.IsItemHovered() && ImGui.IsWindowHovered();
                // ImGui.EndChildFrame();
            // } else {
                // selected = false;
            // }
        }
    }
}
