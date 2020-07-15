using CBRE.Common.Mediator;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CBRE.Editor.UI
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();

            VersionLabel.Text = FileVersionInfo.GetVersionInfo(typeof(Editor).Assembly.Location).FileVersion;

            LTLink.Click += (s, e) => Mediator.Publish(EditorMediator.OpenWebsite, "http://logic-and-trick.com");
            GithubLink.Click += (s, e) => Mediator.Publish(EditorMediator.OpenWebsite, "https://github.com/juanjp600/sledge");
            GPLLink.Click += (s, e) => Mediator.Publish(EditorMediator.OpenWebsite, "http://www.gnu.org/licenses/gpl-2.0.html");
        }
    }
}
