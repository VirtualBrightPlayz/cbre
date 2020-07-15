using CBRE.Settings;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CBRE.Editor {
    public class SingleInstance : WindowsFormsApplicationBase {
        private readonly Type _formType;
        private static SingleInstance _instance;

        public static void Start(Type formType) {
            SettingsManager.Read();
            _instance = new SingleInstance(formType);
            _instance.IsSingleInstance = CBRE.Settings.View.SingleInstance;
            _instance.Run(System.Environment.GetCommandLineArgs());
        }

        protected SingleInstance(Type formType) {
            _formType = formType;
            IsSingleInstance = true;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e) {
            e.BringToForeground = true;
            base.OnStartupNextInstance(e);
            Editor.ProcessArguments(e.CommandLine.ToArray());
        }

        protected override void OnCreateMainForm() {
            MainForm = (Form)Activator.CreateInstance(_formType);
        }
    }
}
