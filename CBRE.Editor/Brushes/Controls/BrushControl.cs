﻿namespace CBRE.Editor.Brushes.Controls {
    public class BrushControl {
        public delegate void ValuesChangedEventHandler(object sender, IBrush brush);

        public event ValuesChangedEventHandler ValuesChanged;

        protected virtual void OnValuesChanged(IBrush brush) {
            if (ValuesChanged != null) {
                ValuesChanged(this, brush);
            }
        }

        public virtual void Draw() {
        }

        protected readonly IBrush Brush;

        private BrushControl() {
        }

        public virtual void Draw() {
        }

        protected BrushControl(IBrush brush) {
            Brush = brush;
        }
    }
}
