using System;
using System.Diagnostics;
using System.Linq;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class StatsWindow : DockableWindow {
        public StatsWindow() : base("stats", ImGuiWindowFlags.None) { }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            var window = GameMain.Instance.Window;
            ImGui.SetWindowPos(new Num.Vector2(ViewportManager.VPRect.Right, window.ClientBounds.Height - 60), ImGuiCond.FirstUseEver);
            ImGui.SetWindowSize(new Num.Vector2(window.ClientBounds.Width - ViewportManager.VPRect.Right, 60), ImGuiCond.FirstUseEver);
            
            foreach (string statLine in DebugStats.Get()) {
                ImGui.Text(statLine);
            }
        }
    }
}
