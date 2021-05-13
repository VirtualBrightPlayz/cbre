﻿using System;
using ImGuiNET;

namespace CBRE.Editor.Brushes.Controls {
    public partial class BooleanControl : BrushControl {
        public bool Checked { get; set; }

        public string LabelText { get; set; }

        public bool ControlEnabled { get; set; }

        public BooleanControl(IBrush brush) : base(brush) { }

        public bool GetValue() {
            return Checked;
        }

        private void ValueChanged(object sender, EventArgs e) {
            OnValuesChanged(Brush);
        }

        public override void Draw() {
            bool tmp = Checked;
            if (ImGui.Checkbox(LabelText, ref tmp)) {
                Checked = tmp;
            }
        }
    }
}
