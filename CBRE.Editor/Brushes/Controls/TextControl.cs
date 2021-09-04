using System;
using ImGuiNET;

namespace CBRE.Editor.Brushes.Controls {
    public partial class TextControl : BrushControl {
        public string EnteredText { get; set; }
        public string LabelText { get; set; }

        public TextControl(IBrush brush) : base(brush) {

        }

        public string GetValue() {
            return EnteredText;
        }

        private void ValueChanged(object sender, EventArgs e) {
            OnValuesChanged(Brush);
        }

        public override void Draw() {
            string val = EnteredText;
            ImGui.Text(LabelText);
            if (ImGui.InputText(LabelText, ref val, 1024))
                EnteredText = val;
        }
    }
}
