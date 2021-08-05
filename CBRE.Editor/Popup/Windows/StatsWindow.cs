using System.Diagnostics;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class StatsWindow : WindowUI
    {
        public StatsWindow() : base("")
        {
        }

        public override bool Draw() {
            if (ImGui.Begin("stats")) {
                var Window = GameMain.Instance.Window;
                ImGui.SetWindowPos(new Num.Vector2(ViewportManager.vpRect.Right, Window.ClientBounds.Height - 60), ImGuiCond.FirstUseEver);
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width - ViewportManager.vpRect.Right, 60), ImGuiCond.FirstUseEver);
                Process proc = Process.GetCurrentProcess();

                proc.Refresh();
                ImGui.Text($"Working set: {proc.WorkingSet64 / 1024 / 1024} MB");
                ImGui.Text($"Private mem: {proc.PrivateMemorySize64 / 1024 / 1024} MB");
                ImGui.Text($"Paged mem: {proc.PagedMemorySize64 / 1024 / 1024} MB");
            }
            ImGui.End();
            return true;
        }
    }
}