using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessagePopup : PopupUI {
        private readonly bool forceFocus;
        protected override bool canBeDefocused => !forceFocus;
        private readonly string message;

        public MessagePopup(string title, string message, ImColor? color = null, bool forceFocus = false) : base(title, color) {
            this.message = message;
            this.forceFocus = forceFocus;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            ImGui.Text(message);
        }
    }
}
