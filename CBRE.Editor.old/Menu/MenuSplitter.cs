using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CBRE.Editor.Menu {
    public class MenuSplitter : IMenuBuilder {
        public bool ShowInMenu { get { return true; } }
        public bool ShowInToolStrip { get; set; }
        public Func<bool> IsVisible { get; set; }

        public IEnumerable<ToolStripItem> Build() {
            //if (IsVisible != null && !IsVisible()) yield break;
            yield return new ToolStripSeparator();
        }

        public IEnumerable<ToolStripItem> BuildToolStrip() {
            //if (IsVisible != null && !IsVisible()) yield break;
            yield return new ToolStripSeparator();
        }
    }
}