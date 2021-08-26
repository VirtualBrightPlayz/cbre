using System.Collections.Generic;
using System.IO;
using CBRE.DataStructures.Geometric;
using CBRE.Editor.Brushes;
using CBRE.Editor.Scripting.LuaAPI;
using CBRE.Settings;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib;

namespace CBRE.Editor.Scripting {
    public class LuaLoader {
        public static readonly List<Script> scripts = new List<Script>();

        public static void Init() {
            List<string> paths = SettingsManager.GetAdditionalData<List<string>>("LuaScripts");
            if (paths == null) {
                SettingsManager.SetAdditionalData("LuaScripts", new List<string>());
                return;
            }
            UserData.RegisterType<JsonModule>();
            UserData.RegisterType<TableModule>();
            UserData.RegisterType<LuaVector3>();
            paths.Add(Path.Combine(typeof(Program).Assembly.Location, "..", "Resources", "test.lua"));
            foreach (var scrPath in paths) {
                if (!File.Exists(scrPath)) continue;
                Script scr = new Script(CoreModules.Preset_HardSandbox);
                scr.Globals["json"] = typeof(JsonModule);
                scr.Globals["table"] = typeof(TableModule);
                scr.Globals["Vector3"] = typeof(LuaVector3);
                scr.DoFile(scrPath);
                scripts.Add(scr);
                LuaBrush b = new LuaBrush(scr);
                BrushManager.Register(b);
            }
        }
    }
}