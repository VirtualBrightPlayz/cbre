using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class DocumentTabWindow : WindowUI
    {
        public DocumentTabWindow() : base("")
        {
            if (GameMain.Instance.Popups.FindIndex(p => p is DocumentTabWindow && p != this) != -1) {
                Dispose();
            }
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Begin("tabber")) {
                ImGui.SetWindowPos(new Num.Vector2(47, 47), ImGuiCond.FirstUseEver);
                ImGui.SetWindowSize(new Num.Vector2(ViewportManager.vpRect.Right - 47, 30), ImGuiCond.FirstUseEver);
                ImGui.BeginTabBar("doc_tabber");
                for (int i = 0; i < DocumentManager.Documents.Count; i++) {
                    Document doc = DocumentManager.Documents[i];
                    if (ImGui.BeginTabItem(doc.MapFileName)) {
                        if (DocumentManager.CurrentDocument != doc) {
                            DocumentManager.SwitchTo(doc);
                        }
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
                /*Num.Vector2 pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
                Num.Vector2 siz = ImGui.GetWindowSize() - ImGui.GetCursorPos() * 1.5f;
                ViewportManager.vpRect = new Microsoft.Xna.Framework.Rectangle((int)pos.X, (int)pos.Y, (int)siz.X, (int)siz.Y);
                ViewportManager.DrawRenderTarget();*/
            }
            ImGui.End();
            return true;
        }
    }
}