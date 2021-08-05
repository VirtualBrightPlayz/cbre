using ImGuiNET;

namespace CBRE.Editor.Popup
{
    public class WindowUI : PopupUI
    {
        public bool open = true;

        public WindowUI(string title) : base(title)
        {
        }

        public WindowUI(string title, ImColor color) : base(title, color)
        {
        }

        public override bool Draw()
        {
            bool shouldBeOpen = true;
            shouldBeOpen = ImGuiLayout();
            return shouldBeOpen;
        }
    }
}