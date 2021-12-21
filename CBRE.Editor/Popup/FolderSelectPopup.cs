using System.IO;
using System.Text;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class FolderSelectPopup : PopupUI {
        private string _path;
        public string Filter { get; set; } = "";
        public string FileName { get; set; } = "";

        public FolderSelectPopup(string title, string path) : base(title) {
            _path = path;
            if (_path == null)
                _path = Directory.GetCurrentDirectory();
            if (string.IsNullOrEmpty(_path))
                _path = Path.GetDirectoryName(typeof(FolderSelectPopup).Assembly.Location);
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {

            ImGui.InputText("Path", ref _path, 1024);
            ImGui.NewLine();


            if (ImGui.BeginChild("Files", new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 12))) {
                ImGui.Columns(2, "files", true);
                ImGui.Separator();
                ImGui.Text("File Name");
                ImGui.NextColumn();
                ImGui.Text("File extension");
                ImGui.NextColumn();
                ImGui.Separator();

                if (Directory.Exists(_path)) {

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
                        if (ImGui.Selectable(Path.GetFileNameWithoutExtension(files[i]), FileName == files[i],
                            ImGuiSelectableFlags.SpanAllColumns)) {
                            FileName = files[i];
                        }

                        bool hovered = ImGui.IsItemHovered();
                        ImGui.NextColumn();
                        ImGui.Text(Path.GetExtension(files[i]));
                        ImGui.NextColumn();
                    }
                }

                ImGui.Columns(1);
                ImGui.Separator();
            }
            ImGui.EndChild();

            ImGui.NewLine();
            ImGui.Text(Filter);
            ImGui.Text(FileName);
            /*string tmp = FileName;
            if (ImGui.InputText("File Name", ref tmp, 1024)) {
                FileName = Path.Combine(_path, tmp);
            }*/
            return ImGuiButtons();
        }

        protected virtual bool ImGuiButtons() {
            if (ImGui.Button("Cancel")) {
                FileSelected(string.Empty);
                return false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Select")) {
                FileSelected(_path);
                return false;
            }
            return true;

        }

        protected virtual void FileSelected(string file)
        {
        }
    }
}
