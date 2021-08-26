using System;
using ImGuiNET;

namespace CBRE.Editor.Brushes.Controls {
    public partial class NumericControl : BrushControl {
        public decimal Minimum { get; set; }

        public decimal Maximum { get; set; }

        public decimal Value { get; set; }

        public string LabelText { get; set; }

        public bool ControlEnabled { get; set; } = true;

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
            double val = (double)Value;
            float val2 = (float)Value;
            if (ControlEnabled) {
                ImGui.Text(LabelText);
                if (Minimum == Maximum) {
                    if (ImGui.InputDouble(LabelText, ref val)) {
                        // val = Math.Clamp(val, (double)Minimum, (double)Maximum);
                        Value = (decimal)val;
                    }
                }
                else {
                    if (ImGui.SliderFloat(LabelText, ref val2, (float)Minimum, (float)Maximum)) {
                        Value = (decimal)val2;
                    }
                }
            }
        }
    }
}
