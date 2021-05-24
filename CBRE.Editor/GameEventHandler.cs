using System;
using CBRE.Common.Mediator;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Providers.Map;
using CBRE.Settings;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain : IMediatorListener {
        public void Notify(string message, object data) {
            /*if (Enum.TryParse(message, true, out HotkeysMediator hotkeys)) {

            }*/
            if (!Mediator.ExecuteDefault(this, message, data)) {
                throw new Exception("Invalid GameMain message: " + message + ", with data: " + data);
            }
        }

        public void MediatorError(object sender, MediatorExceptionEventArgs e) {
            Logging.Logger.ShowException(e.Exception, e.Message);
        }

        public void Subscribe() {
            Mediator.Subscribe(HotkeysMediator.FileNew, this);
            Mediator.Subscribe(HotkeysMediator.FileOpen, this);
        }

        public void FileNew() {
            string name = DocumentManager.GetUntitledDocumentName();
            Document doc = new Document(name, new DataStructures.MapObjects.Map());
            DocumentManager.AddAndSwitch(doc);
        }

        public void FileOpen() {
            new OpenMap("");
        }

        public void Options() {
            new SettingsPopup();
        }

        public void MapInformation() {
            new MapInformationPopup();
        }

        public void About() {
            new AboutPopup();
        }
    }
}