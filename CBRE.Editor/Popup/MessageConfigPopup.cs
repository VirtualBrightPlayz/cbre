using System;
using System.Reflection;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class MessageConfigPopup<T> : PopupUI where T : class {
        private readonly string message;
        private readonly T data;
        private readonly Action<MessageConfigPopup<T>, T> callback;
        private FieldInfo[] infos;

        public MessageConfigPopup(string title, string message, T data, Action<MessageConfigPopup<T>, T> callback, ImColor? color = null) : base(title, color) {
            this.message = message;
            this.data = data;
            this.callback = callback;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            ImGui.Text(message);
            infos ??= data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in infos) {
                object fieldData = field.GetValue(data);
                switch (fieldData) {
                    case bool b: {
                        bool val = b;
                        if (ImGui.Checkbox(field.Name, ref val)) {
                            field.SetValue(data, val);
                        }
                    } break;
                    case float f: {
                        float val = f;
                        if (ImGui.InputFloat(field.Name, ref val)) {
                            field.SetValue(data, val);
                        }
                    } break;
                    case int i:
                    {
                        int val = i;
                        if (ImGui.InputInt(field.Name, ref val)) {
                            field.SetValue(data, val);
                        }
                    } break;
                    case string s: {
                        string val = s;
                        if (ImGui.InputText(field.Name, ref val, 1024)) {
                            field.SetValue(data, val);
                        }
                    } break;
                }
            }
        }

        public override void Dispose() {
            callback?.Invoke(this, data);
        }
    }
}
