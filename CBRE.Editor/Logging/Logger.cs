using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CBRE.Editor.Popup;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Logging {
    public static class Logger {
        public static string LogFile { get; private set; }

        static Logger()
        {
            LogFile = "cbre-log-" + DateTime.Now.ToString("yyyy_MM_dd_T_HH_mm_ss") + ".txt";
            LogFile = Path.GetFullPath(Path.Combine(typeof(Logger).Assembly.Location, "..", LogFile));
        }

        public static void Log(ExceptionInfo info) {
            File.AppendAllText(LogFile, $"===Date: {info.Date.ToString()}\n===Application Version: {info.ApplicationVersion}\n===Runtime Version: {info.RuntimeVersion}\n===Message: {info.Message}\n===Source: {info.Source}\n===OS: {info.FriendlyOSName()}\n===Stack Trace: {info.FullStackTrace}\n\n");
        }

        public static void ShowException(Exception ex, string message = "") {
            var info = new ExceptionInfo(ex, message);
            Log(info);
            GameMain.Instance.Popups.Add(
                new CopyMessagePopup($"Exception", $"{info.Message}\nThis error has been logged to the file \"{LogFile}\"",
                        new ImColor() { Value = new Num.Vector4(0.75f, 0f, 0f, 1f) }));
            /*var window = new ExceptionWindow(info);
            if (Editor.Instance == null || Editor.Instance.IsDisposed) window.Show();
            else window.Show(Editor.Instance);*/
        }
    }

    public class ExceptionInfo {
        public Exception Exception { get; set; }
        public string RuntimeVersion { get; set; }
        public string OperatingSystem { get; set; }
        public string ApplicationVersion { get; set; }
        public DateTime Date { get; set; }
        public string InformationMessage { get; set; }
        public string UserEnteredInformation { get; set; }

        public string Source {
            get { return Exception.Source; }
        }

        public string Message {
            get {
                var msg = String.IsNullOrWhiteSpace(InformationMessage) ? Exception.Message : InformationMessage;
                return msg.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => !String.IsNullOrWhiteSpace(x));
            }
        }

        public string StackTrace {
            get { return Exception.StackTrace; }
        }

        public string FullStackTrace { get; set; }

        public string FriendlyOSName() {
            Version version = System.Environment.OSVersion.Version;
            string os;
            switch (version.Major) {
                case 6:
                    switch (version.Minor) {
                        case 1: os = $"Windows 7 (NT {version.Major}.{version.Minor}, Build {version.Build})"; break;
                        case 2: os = $"Windows 8 (NT {version.Major}.{version.Minor}, Build {version.Build})"; break;
                        case 3: os = $"Windows 8.1 (NT {version.Major}.{version.Minor}, Build {version.Build})"; break;
                        default: os = "Unknown"; break;
                    }
                    break;
                case 10:
                    switch (version.Minor) {
                        case 0: os = $"Windows 10 (NT {version.Major}.{version.Minor}, Build {version.Build})"; break;
                        default: os = "Unknown"; break;
                    }
                    break;
                default:
                    os = "Unknown";
                    break;
            }
            return os;
        }

        public ExceptionInfo(Exception exception, string info) {
            Exception = exception;
            RuntimeVersion = System.Environment.Version.ToString();
            Date = DateTime.Now;
            InformationMessage = info;
            ApplicationVersion = FileVersionInfo.GetVersionInfo(typeof(Logger).Assembly.Location).FileVersion;
            OperatingSystem = FriendlyOSName();

            var list = new List<Exception>();
            do {
                list.Add(exception);
                exception = exception.InnerException;
            } while (exception != null);

            FullStackTrace = (info + "\r\n").Trim();
            foreach (var ex in Enumerable.Reverse(list)) {
                FullStackTrace += "\r\n" + ex.Message + " (" + ex.GetType().FullName + ")\r\n" + ex.StackTrace;
            }
            FullStackTrace = FullStackTrace.Trim();
        }
    }
}
