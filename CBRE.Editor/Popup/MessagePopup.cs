using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessagePopup : PopupUI {
        private readonly string message;

        public MessagePopup(string title, string message, ImColor? color = null) : base(title, color) {
            this.message = message;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            ImGui.Text(message);
        }
    }
}
