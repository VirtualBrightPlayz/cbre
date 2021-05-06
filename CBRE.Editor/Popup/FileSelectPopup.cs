using System.IO;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class FileSelectPopup : PopupUI {
        private string _path;
        public string Filter { get; set; } = "";
        public string FileName { get; set; } = "";

        public FileSelectPopup(string title, string path) : base(title) {
            _path = path;
            if (_path == null)
                _path = Directory.GetCurrentDirectory();
            if (string.IsNullOrEmpty(_path))
                _path = Path.GetDirectoryName(typeof(FileSelectPopup).Assembly.Location);
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {

            ImGui.Text(_path);
            ImGui.NewLine();


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
                    _path = Path.GetFullPath(Path.Combine(_path, ".."));
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
                if (ImGui.Selectable(Path.GetFileNameWithoutExtension(files[i]), false, ImGuiSelectableFlags.SpanAllColumns)) {
                    FileName = files[i];
                }
                bool hovered = ImGui.IsItemHovered();
                ImGui.NextColumn();
                ImGui.Text(Path.GetExtension(files[i]));
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
            if (ImGui.Button("Select")) {
                FileSelected(FileName);
                return false;
            }
            return true;
        }

        protected virtual void FileSelected(string file)
        {
        }
    }
}