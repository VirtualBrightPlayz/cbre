using System.Reflection;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessageConfigPopup : PopupUI {
        private string _message;
        private object _data;
        private FieldInfo[] infos;

        public MessageConfigPopup(string title, string message, object data) : base(title) {
            _message = message;
            _data = data;
            GameMain.Instance.PopupSelected = true;
        }

        public MessageConfigPopup(string title, string message, object data, ImColor color) : base(title, color) {
            _message = message;
            _data = data;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            ImGui.Text(_message);
            if (infos == null) {
                infos = _data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            }
            foreach (FieldInfo field in infos) {
                object fdata = field.GetValue(_data);
                switch (fdata)
                {
                    case bool b:
                    {
                        bool val = b;
                        if (ImGui.Checkbox(field.Name, ref val))
                        {
                            field.SetValue(_data, val);
                        }
                    }
                    break;
                    case float f:
                    {
                        float val = f;
                        if (ImGui.InputFloat(field.Name, ref val))
                        {
                            field.SetValue(_data, val);
                        }
                    }
                    break;
                    case int i:
                    {
                        int val = i;
                        if (ImGui.InputInt(field.Name, ref val))
                        {
                            field.SetValue(_data, val);
                        }
                    }
                    break;
                    case string s:
                    {
                        string val = s;
                        if (ImGui.InputText(field.Name, ref val, 1024))
                        {
                            field.SetValue(_data, val);
                        }
                    }
                    break;
                }
            }
            return base.ImGuiLayout();
        }
    }
}