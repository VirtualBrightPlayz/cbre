using ImGuiNET;

namespace CBRE.Editor.Popup
{
    public abstract class DockableWindow {
        protected readonly string title;
        protected readonly ImGuiWindowFlags flags;

        private bool open = true;
        public bool Open => open;

        public DockableWindow(string title, ImGuiWindowFlags flags) {
            this.title = title;
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

        public virtual void Dispose() { }
    }
}
