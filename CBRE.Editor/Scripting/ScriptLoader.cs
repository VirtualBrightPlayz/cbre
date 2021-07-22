using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CBRE.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace CBRE.Editor.Scripting
{
    public static class ScriptLoader
    {
        public static readonly List<Assembly> loadedScripts = new List<Assembly>();

        public static void Init()
        {
            string[] paths = SettingsManager.GetAdditionalData<string[]>("Scripts");
            loadedScripts.Clear();
            foreach (var scrPath in paths)
            {
                if (!File.Exists(scrPath)) continue;
                Assembly asm = Run(scrPath);
                if (asm != null)
                {
                    loadedScripts.Add(asm);
                }
            }
        }

        public static Assembly Run(string path)
        {
            Assembly asm = Assembly.Load(Load(path));
            if (asm == null)
                return null;
            asm.EntryPoint.Invoke(null, null);
            return asm;
        }

        public static byte[] Load(string path)
        {
            var refs = new List<string>();
            var arr = typeof(ScriptLoader).Assembly.GetReferencedAssemblies();
            foreach (var item in arr)
            {
                refs.Add(item.Name);
            }
            refs.Add(typeof(ScriptLoader).Assembly.GetName().Name);
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var source = SourceText.From(File.ReadAllText(path));
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var list = new List<MetadataReference>();
            foreach (var item in refs)
            {
                if (item != null)
                {
                    list.Add(MetadataReference.CreateFromFile(Assembly.Load(item).Location));
                }
            }
            var comp = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(path) + ".csdll", new List<SyntaxTree>() { tree }, list, options: options);
            using (MemoryStream ms = new MemoryStream())
            {
                EmitResult res = comp.Emit(ms);
                if (res.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
                throw new Exception("\n" + string.Join("\n", res.Diagnostics.Select(p => p.ToString())));
            }
        }
    }
}