using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Drawing;

namespace CBRE.Editor.Tools.Widgets
{
    public abstract class Widget : BaseTool
    {
        protected ViewportBase _activeViewport;

        private Action<Matrix> _transformedCallback = null;
        private Action<Matrix> _transformingCallback = null;

        public Action<Matrix> OnTransformed
        {
            get
            {
                return _transformedCallback ?? (x => { });
            }
            set
            {
                _transformedCallback = value;
            }
        }

        public Action<Matrix> OnTransforming
        {
            get
            {
                return _transformingCallback ?? (x => { });
            }
            set
            {
                _transformingCallback = value;
            }
        }

        public override string GetIcon() { return null; }
        public override string GetName() { return "Widget"; }
        public override HotkeyTool? GetHotkeyToolType() { return null; }
        public override string GetContextualHelp() { return ""; }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters) { return HotkeyInterceptResult.Continue; }
        public override void KeyLift(ViewportBase viewport, ViewportEvent e) { }
        public override void KeyHit(ViewportBase viewport, ViewportEvent e) { }
        public override void MouseClick(ViewportBase viewport, ViewportEvent e) { }
        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e) { }
        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame) { }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e)
        {
            _activeViewport = viewport;
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e)
        {
            _activeViewport = null;
        }

        public abstract void SelectionChanged();
    }
}
