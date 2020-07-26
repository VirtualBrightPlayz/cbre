using System;

namespace CBRE.Editor.Brushes.Controls {
    public partial class TextControl : BrushControl {
        public string EnteredText { get; set; }

        public TextControl(IBrush brush) : base(brush) {

        }

        public string GetValue() {
            return EnteredText;
        }

        private void ValueChanged(object sender, EventArgs e) {
            OnValuesChanged(Brush);
        }
    }
}
