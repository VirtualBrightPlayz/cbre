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
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CrashHandler;

            using (game = new GameMain()) {
                game.Run();
            }
        }

        private static void CrashHandler(object sender, UnhandledExceptionEventArgs e) {
            game.Exit();
            Exception exception = e.ExceptionObject as Exception;
            string stats;
            try {
                stats = string.Join("\n", DebugStats.Get());
            } catch {
                stats = "Failed to retrieve DebugStats";
            }
            var writeTask = Task.Run(async() => {
                await Task.Yield();
                await File.AppendAllTextAsync(Logging.Logger.LogFile, stats);
                await File.AppendAllTextAsync(Logging.Logger.LogFile, $"!!!CRASH!!!\n===Stack Trace: {e.ToString()}\n\n");
            });
            MessageBox.Show(MessageBox.Flags.Error, "CBRE has crashed", $"CBRE has crashed:\n\n{stats}\n\n{exception.Message}\n{exception.StackTrace}");
            writeTask.Wait(10000);
#if !DEBUG
            Environment.Exit(0);
#endif
        }
    }
}
