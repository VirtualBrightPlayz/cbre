using System;
using System.Collections.Immutable;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class ConfirmPopup : PopupUI {
        private string _message;

        protected override bool canBeClosed => true;
        protected override bool canBeDefocused => false;

        public readonly struct Button {
            public readonly string Label;
            public readonly Action Action;

            public Button(string label, Action action) {
                Label = label;
                Action = action;
            }
        }
        public ImmutableArray<Button> Buttons { get; init; }

        public ConfirmPopup(string title, string message, ImColor? color = null) : base(title, color) {
            _message = message;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            ImGui.Text(_message);

            for (int i=0; i<Buttons.Length; i++) {
                var btn = Buttons[i];
                if (i > 0) { ImGui.SameLine(); }
                if (ImGui.Button($"{btn.Label}##popup{popupIndex}btn{i}")) {
                    btn.Action();
                    shouldBeOpen = false;
                    return;
                }
            }
        }
    }
}
