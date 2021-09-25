using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Microsoft.Xna.Framework;
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
            ImGui.SetCursorPos(new Num.Vector2(0, 20));
            if (ImGui.BeginChild("DocumentTabs", new Num.Vector2(viewportPtr.Size.X, 20))) {
                for (int i = 0; i < DocumentManager.Documents.Count; i++) {
                    Document doc = DocumentManager.Documents[i];
                    if (ImGui.Button(doc.MapFileName)) {
                        if (DocumentManager.CurrentDocument != doc) {
                            DocumentManager.SwitchTo(doc);
                        }
                    }
                    ImGui.SameLine();
                    var cursorPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(cursorPos - new Num.Vector2(8, 0));
                    using (new ColorPush(ImGuiCol.Button, Color.DarkRed)) {
                        using (new ColorPush(ImGuiCol.ButtonActive, Color.DarkRed)) {
                            using (new ColorPush(ImGuiCol.ButtonHovered, Color.Red)) {
                                if (ImGui.Button($"X##doc{DocumentManager.Documents.IndexOf(doc)}")) {
                                    DocumentManager.Remove(doc);
                                }
                            }
                        }
                    }
                    ImGui.SameLine();
                }
                ImGui.EndChild();
            }
        }
    }
}
