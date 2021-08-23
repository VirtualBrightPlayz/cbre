using System;
using System.Reflection;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessageConfigPopup<T> : PopupUI where T : class {
        private string _message;
        private T _data;
        private Action<MessageConfigPopup<T>, T> _callback;
        private FieldInfo[] infos;

        public MessageConfigPopup(string title, string message, T data, Action<MessageConfigPopup<T>, T> callback) : base(title) {
            _message = message;
            _data = data;
            _callback = callback;
            GameMain.Instance.PopupSelected = true;
        }

        public MessageConfigPopup(string title, string message, T data, Action<MessageConfigPopup<T>, T> callback, ImColor color) : base(title, color) {
            _message = message;
            _data = data;
            _callback = callback;
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

        public override void Close() {
            _callback?.Invoke(this, _data);
            base.Close();
        }
    }
}