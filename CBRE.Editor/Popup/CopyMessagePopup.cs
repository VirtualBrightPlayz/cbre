using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class CopyMessagePopup : PopupUI {
        private string _message;

        public CopyMessagePopup(string title, string message, ImColor? color = null) : base(title, color) {
            _message = message;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            if (ImGui.Selectable(_message)) {
                ImGui.SetClipboardText(_message);
            }
        }
    }
}
