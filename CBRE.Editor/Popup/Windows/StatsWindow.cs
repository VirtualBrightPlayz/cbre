using System;
using System.Diagnostics;
using System.Linq;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class StatsWindow : WindowUI {
        public StatsWindow() : base("") { }

        public override bool Draw() {
            if (ImGui.Begin("stats", ref open)) {
                var Window = GameMain.Instance.Window;
                ImGui.SetWindowPos(new Num.Vector2(ViewportManager.VPRect.Right, Window.ClientBounds.Height - 60), ImGuiCond.FirstUseEver);
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width - ViewportManager.VPRect.Right, 60), ImGuiCond.FirstUseEver);
                
                foreach (string statLine in DebugStats.Get()) {
                    ImGui.Text(statLine);
                }

                ImGui.End();
            }
            return open;
        }
    }
}
