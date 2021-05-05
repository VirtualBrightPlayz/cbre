using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessagePopup : PopupGame {
        private string _message;

        public MessagePopup(string title, string message) : base(title) {
            _message = message;
        }

        protected override bool ImGuiLayout() {
            ImGui.Text(_message);
            return base.ImGuiLayout();
        }
    }
}