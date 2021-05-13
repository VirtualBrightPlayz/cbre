using System;
using ImGuiNET;

namespace CBRE.Editor.Brushes.Controls {
    public partial class NumericControl : BrushControl {
        public decimal Minimum { get; set; }

        public decimal Maximum { get; set; }

        public decimal Value { get; set; }

        public string LabelText { get; set; }

        public bool ControlEnabled { get; set; }

        public int Precision { get; set; }

        public decimal Increment { get; set; }

        public NumericControl(IBrush brush) : base(brush) { }

        public decimal GetValue() {
            return Value;
        }

        private void ValueChanged(object sender, EventArgs e) {
            OnValuesChanged(Brush);
        }

        public override void Draw() {
            string tmp = Value.ToString();
            if (ImGui.InputText(LabelText, ref tmp, 1024) && decimal.TryParse(tmp, out decimal tmpval) && (Maximum == Minimum || (tmpval <= Maximum && tmpval >= Minimum))) {
                Value = tmpval;
            }
        }
    }
}
