using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CBRE.Graphics;

namespace CBRE.Editor
{
    static class Program
    {
        public static void Main() {
            try {
                using (var game = new GameMain()) { game.Run(); }
            }
            catch (Exception e) {
                File.AppendAllText(Logging.Logger.LogFile, $"!!!CRASH!!!\n===Stack Trace: {e.ToString()}\n\n");
                throw;
            }
            Environment.Exit(0);
        }
    }
}
