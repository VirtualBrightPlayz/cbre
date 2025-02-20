using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class ToolsWindow : DockableWindow
    {
        public ToolsWindow() : base("tools", ImGuiWindowFlags.None)
        {
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            ImGui.SetWindowPos(new Num.Vector2(-1, 27), ImGuiCond.FirstUseEver);
            ImGui.SetWindowSize(new Num.Vector2(47, GameMain.Instance.Window.ClientBounds.Height - 27), ImGuiCond.FirstUseEver);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Num.Vector2(0, 1));

            GameMain.Instance.ToolBarItems.ForEach(it => it.Draw());

            ImGui.PopStyleVar();
        }
    }
}
