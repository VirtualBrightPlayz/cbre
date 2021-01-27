using System;
using System.Collections.Generic;
using System.Text;
using CBRE.Graphics;

namespace CBRE.Editor
{
    static class Program
    {
        public static void Main() {
            using (var game = new GameMain()) { game.Run(); }
        }
    }
}
