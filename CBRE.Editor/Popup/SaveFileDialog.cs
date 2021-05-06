using System.IO;
using System.Threading.Tasks;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class SaveFileDialog : PopupUI {
        private DialogResult _result;
        public string Filter { get; set; }
        public string FileName { get; set; }
        private int _selected = -1;
        private string _path;
        private bool _closed = false;

        public SaveFileDialog() : base("Save file") {
            _path = Directory.GetCurrentDirectory();
        }

        public virtual async Task<DialogResult> ShowDialog()
        {
            while (!_closed)
                await Task.Delay(100);
            return _result;
        }

        public override void Close() {
            base.Close();
            _closed = true;
        }

        protected override bool ImGuiLayout() {
            ImGui.Columns(2, "files", true);
            ImGui.Separator();
            ImGui.Text("File Name");
            ImGui.NextColumn();
            ImGui.Text("File extension");
            ImGui.NextColumn();
            ImGui.Separator();

            string[] files = Directory.GetFiles(_path);

            for (int i = 0; i < files.Length; i++) {
                if (ImGui.Selectable(Path.GetFileNameWithoutExtension(files[i]), _selected == i, ImGuiSelectableFlags.SpanAllColumns)) {
                    _selected = i;
                }
                bool hovered = ImGui.IsItemHovered();
                ImGui.NextColumn();
                ImGui.Text(Path.GetExtension(files[i]));
                ImGui.NextColumn();
            }

            ImGui.NewLine();
            ImGui.Text(Filter);
            ImGui.Text(FileName);
            if (ImGui.Button("Cancel")) {
                _result = DialogResult.Abort;
                return false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Save")) {
                _result = DialogResult.OK;
                return false;
            }
            return true;
        }
    }
}