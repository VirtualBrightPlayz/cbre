using System.Diagnostics;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class StatsWindow : WindowUI {
        private Process proc;
        
        public StatsWindow() : base("") {
            proc = Process.GetCurrentProcess();
        }

        public override bool Draw() {
            if (ImGui.Begin("stats", ref open)) {
                var Window = GameMain.Instance.Window;
                ImGui.SetWindowPos(new Num.Vector2(ViewportManager.vpRect.Right, Window.ClientBounds.Height - 60), ImGuiCond.FirstUseEver);
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width - ViewportManager.vpRect.Right, 60), ImGuiCond.FirstUseEver);
                
                proc.Refresh();
                ImGui.Text($"Working set: {proc.WorkingSet64 / 1024 / 1024} MB");
                ImGui.Text($"Private mem: {proc.PrivateMemorySize64 / 1024 / 1024} MB");
                ImGui.Text($"Paged mem: {proc.PagedMemorySize64 / 1024 / 1024} MB");
            }
            ImGui.End();
            return open;
        }
    }
}
