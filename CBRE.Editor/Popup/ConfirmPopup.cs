using System;
using System.Collections.Immutable;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class ConfirmPopup : PopupUI {
        private readonly string message;

        protected override bool canBeClosed => true;
        protected override bool canBeDefocused => false;
        protected override bool hasOkButton => false;

        public readonly record struct Button(string Label, Action Action);
        
        public ImmutableArray<Button> Buttons { get; init; }

        public ConfirmPopup(string title, string message, ImColor? color = null) : base(title, color) {
            this.message = message;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            ImGui.Text(message);

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
