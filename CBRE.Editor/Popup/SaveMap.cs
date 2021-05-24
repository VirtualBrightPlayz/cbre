using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers;
using CBRE.Providers.Map;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SaveMap : FileSelectPopup
    {
        private Document _map;
        private bool _close;

        public SaveMap(string path, Document map, bool closeOnSave) : base("Save Map", path)
        {
            _map = map;
            _close = closeOnSave;
        }

        protected override bool ImGuiButtons() {
            if (ImGui.Button("Cancel")) {
                FileSelected(string.Empty);
                return false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Save")) {
                FileSelected(FileName);
                return false;
            }
            if (!_close) {
                return true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Don't Save")) {
                FileSelected(string.Empty);
                DocumentManager.Remove(_map);
                if (DocumentManager.Documents.Count == 0) {
                    DocumentManager.AddAndSwitch(new Document(Document.NewDocumentName, new Map()));
                }
                return false;
            }
            return true;
        }

        protected override void FileSelected(string file) {
            if (string.IsNullOrEmpty(file))
                return;
            try
            {
                _map.SaveFile(file);
            }
            catch (ProviderNotFoundException e)
            {
                new MessagePopup("Error", e.Message, new ImColor() { Value = new Num.Vector4(1f, 0f, 0f, 1f) });
            }
        }
    }
}