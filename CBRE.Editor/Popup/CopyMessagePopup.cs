using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class CopyMessagePopup : PopupUI {
        private string _message;

        public CopyMessagePopup(string title, string message) : base(title) {
            _message = message;
            GameMain.Instance.PopupSelected = true;
        }

        public CopyMessagePopup(string title, string message, ImColor color) : base(title, color) {
            _message = message;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Selectable(_message))
                ImGui.SetClipboardText(_message);
            return base.ImGuiLayout();
        }
    }
}