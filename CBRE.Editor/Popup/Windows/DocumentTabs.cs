using System.Linq;
using CBRE.Common.Mediator;
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

        public const int Height = 20;
        public void ImGuiLayout() {
            ImGuiViewportPtr viewportPtr = ImGui.GetMainViewport();
            ImGui.SetCursorPos(new Num.Vector2(0, GameMain.MenuBarHeight + GameMain.TopBarHeight));
            if (ImGui.BeginChild("DocumentTabs", new Num.Vector2(viewportPtr.Size.X, Height))) {
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
                    using (ColorPush.RedButton()) {
                        if (ImGui.Button($"X##doc{DocumentManager.Documents.IndexOf(doc)}")) {
                            var currDoc = DocumentManager.CurrentDocument;
                            DocumentManager.SwitchTo(doc);
                            Mediator.Publish(CBRE.Settings.HotkeysMediator.FileClose);
                            DocumentManager.SwitchTo(currDoc);
                        }
                    }
                    ImGui.SameLine();
                }
            }
            ImGui.EndChild();
        }
    }
}
