using System;
using System.Linq;
using System.Reflection;
using ImGuiNET;

namespace CBRE.Editor.Popup
{
    public abstract class DockableWindow {
        protected readonly string title;
        protected readonly ImGuiWindowFlags flags;

        private bool open = true;
        public bool Open => open;

        public static DockableWindow OpenFromFullName(string name) {
            var ctor = typeof(DockableWindow).Assembly.GetType(name).GetConstructor(Type.EmptyTypes);
            if (ctor == null) { return null; }
            return ctor.Invoke(null) as DockableWindow;
        }

        public DockableWindow(string title, ImGuiWindowFlags flags) {
            this.title = $"{title}##{GameMain.Instance.Dockables.Count(x => x.GetType() == GetType())}";
            this.flags = flags;
        }

        protected abstract void ImGuiLayout(out bool shouldBeOpen);
        public virtual void Update() { }
        
        public void Draw(out bool shouldBeOpen) {
            shouldBeOpen = open;
            if (ImGui.Begin(title, ref open, flags)) {
                ImGuiLayout(out shouldBeOpen);
                open &= shouldBeOpen;
                ImGui.End();
            }
        }

        public virtual void Dispose() {
        }
    }
}
