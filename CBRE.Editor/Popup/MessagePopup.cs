using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessagePopup : PopupUI {
        private string _message;

        public MessagePopup(string title, string message) : base(title) {
            _message = message;
        }

        public MessagePopup(string title, string message, ImColor color) : base(title, color) {
            _message = message;
        }

        protected override bool ImGuiLayout() {
            ImGui.Text(_message);
            return base.ImGuiLayout();
        }
    }
}