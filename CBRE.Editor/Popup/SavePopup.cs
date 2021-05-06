using System.IO;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers;
using CBRE.Providers.Map;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SavePopup : PopupUI {
        private string _message;
        private Map _map;
        private string _path;
        public string Filter { get; set; } = "";
        public string FileName { get; set; } = "";

        public SavePopup(string title, string message, string path, Map map) : base(title) {
            _message = message;
            _path = path;
            if (_path == null)
                _path = Directory.GetCurrentDirectory();
            _map = map;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            ImGui.BeginChild("Files", new Num.Vector2(500, ImGui.GetTextLineHeightWithSpacing() * 12));
            ImGui.Columns(2, "files", true);
            ImGui.Separator();
            ImGui.Text("File Name");
            ImGui.NextColumn();
            ImGui.Text("File extension");
            ImGui.NextColumn();
            ImGui.Separator();

            string[] special = new string[] { ".." };
            
            for (int i = 0; i < special.Length; i++) {
                if (ImGui.Selectable(special[i], false, ImGuiSelectableFlags.SpanAllColumns)) {
                    _path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_path, ".."));
                }
                bool hovered = ImGui.IsItemHovered();
                ImGui.NextColumn();
                ImGui.Text("/Special/");
                ImGui.NextColumn();
            }

            string[] dirs = Directory.GetDirectories(_path);

            for (int i = 0; i < dirs.Length; i++) {
                if (ImGui.Selectable(dirs[i], false, ImGuiSelectableFlags.SpanAllColumns)) {
                    _path = dirs[i];
                }
                bool hovered = ImGui.IsItemHovered();
                ImGui.NextColumn();
                ImGui.Text("/Dir/");
                ImGui.NextColumn();
            }

            string[] files = Directory.GetFiles(_path);

            for (int i = 0; i < files.Length; i++) {
                if (ImGui.Selectable(System.IO.Path.GetFileNameWithoutExtension(files[i]), false, ImGuiSelectableFlags.SpanAllColumns)) {
                    FileName = files[i];
                }
                bool hovered = ImGui.IsItemHovered();
                ImGui.NextColumn();
                ImGui.Text(System.IO.Path.GetExtension(files[i]));
                ImGui.NextColumn();
            }

            ImGui.Columns(1);
            ImGui.Separator();
            ImGui.EndChild();

            ImGui.NewLine();
            ImGui.Text(Filter);
            ImGui.Text(FileName);
            if (ImGui.Button("Cancel")) {
                return false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Save")) {
                try
                {
                    MapProvider.SaveMapToFile(_path, _map);
                }
                catch (ProviderNotFoundException e)
                {
                    new MessagePopup("Error", e.Message, new ImColor() { Value = new Num.Vector4(1f, 0f, 0f, 1f) });
                }
                return false;
            }
            return true;
        }
    }
}