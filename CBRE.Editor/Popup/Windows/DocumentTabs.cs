using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class DocumentTabs
    {
        public DocumentTabs()
        {
            /*if (GameMain.Instance.Popups.FindIndex(p => p is DocumentTabWindow && p != this) != -1) {
                Dispose();
            }*/
        }

        public void ImGuiLayout() {
            ImGuiViewportPtr viewportPtr = ImGui.GetMainViewport();
            if (ImGui.Begin("DocumentTabs", ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse)) {
                ImGui.SetWindowPos(new Num.Vector2(0, 20), ImGuiCond.Always);
                ImGui.SetWindowSize(new Num.Vector2(viewportPtr.Size.X, 40), ImGuiCond.Always);
                ImGui.BeginTabBar("doc_tabber");
                for (int i = 0; i < DocumentManager.Documents.Count; i++) {
                    Document doc = DocumentManager.Documents[i];
                    if (ImGui.BeginTabItem(doc.MapFileName)) {
                        if (DocumentManager.CurrentDocument != doc) {
                            DocumentManager.SwitchTo(doc);
                        }
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }
    }
}
