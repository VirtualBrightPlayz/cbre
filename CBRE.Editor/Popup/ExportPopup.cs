using CBRE.Editor.Compiling;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Documents;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class ExportPopup : PopupUI {
        private Document _document;

        public ExportPopup(Document document) : base("Export / Compile") {
            _document = document;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Button("Render Lightmaps")) {
                try {
                    Lightmapper.Render(_document, out var faces, out var lmgroups);
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            if (ImGui.Button("Export as .rmesh")) {
                try {
                    new FileCallbackPopup("Save .rmesh", "", s => RMeshExport.SaveToFile(s, _document));
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            if (ImGui.Button("Export as .rm2")) {
                try {
                    new FileCallbackPopup("Save .rm2", "", s => RM2Export.SaveToFile(s, _document));
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            return base.ImGuiLayout();
        }
    }
}