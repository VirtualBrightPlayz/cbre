using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace CBRE.Editor
{
    static class Program {
        private static GameMain game;
        
        public static void Main() {
            try {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += CrashHandler;
            } catch (Exception e) {
                Console.WriteLine($"Could not set up exception handler: {e.Message} ({e.GetType().Name})\n{e.StackTrace}");
            }

            using (game = new GameMain()) {
                game.Run();
            }
        }

        private static void CrashHandler(object sender, UnhandledExceptionEventArgs e) {
            Exception exception = e.ExceptionObject as Exception;

            Console.WriteLine($"Unhandled exception: {exception.Message} ({exception.GetType().Name})\n{exception.StackTrace}");

            void swallowException(Action action) {
                try {
                    action();
                } catch {
                    //nobody cares!
                }
            }
            swallowException(() => game.Exit());
            string stats;
            try {
                stats = string.Join("\n", DebugStats.Get());
            } catch {
                stats = "Failed to retrieve DebugStats";
            }

            string crashReportPath = "crashreport.txt";
            if (File.Exists(crashReportPath)) { File.Delete(crashReportPath); }

            File.AppendAllText(crashReportPath, stats);
            File.AppendAllText(crashReportPath, $"!!!CRASH!!!\n===Exception: {exception.Message} ({exception.GetType().Name})\n{exception.StackTrace}\n\n");
            swallowException(() => MessageBox.Show(MessageBox.Flags.Error, "CBRE has crashed", $"CBRE has crashed:\n\n{stats}\n\n{exception.Message}\n{exception.StackTrace}"));
#if !DEBUG
            Environment.Exit(0);
#endif
        }
    }
}
