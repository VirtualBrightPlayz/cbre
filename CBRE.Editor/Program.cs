using System;
using System.Collections.Generic;
using System.IO;
using NativeFileDialog;

namespace CBRE.Editor
{
    static class Program
    {
        public static void Main() {
            try {
                var result = OpenDialogMultiple.Open("png,jpg", Environment.CurrentDirectory + "\\sus.png", out IEnumerable<string> outPaths);
                using (var game = new GameMain()) { game.Run(); }
            }
            catch (Exception e) {
                File.AppendAllText(Logging.Logger.LogFile, $"!!!CRASH!!!\n===Stack Trace: {e.ToString()}\n\n");
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }
    }
}
