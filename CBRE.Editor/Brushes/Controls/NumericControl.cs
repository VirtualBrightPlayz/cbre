using System;

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
    }
}
