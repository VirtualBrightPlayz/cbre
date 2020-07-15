using CBRE.Editor.Documents;
using CBRE.Providers.Model;
using System.Windows.Forms;

namespace CBRE.Editor.Compiling {
    class GenericExport {
        public static void SaveToFile(string filename, Document document, ExportForm form, string format) {
            form.ProgressBar.Invoke((MethodInvoker)(() => form.ProgressBar.Maximum = 10000));
            AssimpProvider.SaveToFile(filename, document.Map, format);
            form.ProgressLog.Invoke((MethodInvoker)(() => form.ProgressLog.AppendText("\nDone!")));
            form.ProgressBar.Invoke((MethodInvoker)(() => form.ProgressBar.Value = 10000));
        }
    }
}
