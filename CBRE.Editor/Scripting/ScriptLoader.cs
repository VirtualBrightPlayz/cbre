using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CBRE.Editor.Scripting {
    public class ScriptLoader {
        public void Run(string filename) {
            var comp = CSharpCompilation.Create(Path.GetRandomFileName());
            foreach (var item in typeof(Program).Assembly.GetReferencedAssemblies()) {
                comp.AddReferences(MetadataReference.CreateFromFile(item.CodeBase));
            }
            comp.AddReferences(MetadataReference.CreateFromFile(typeof(Program).Assembly.Location));
            using (MemoryStream ms = new MemoryStream()) {
                EmitResult res = comp.Emit(ms);
                if (res.Success) {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly asm = Assembly.Load(ms.ToArray());
                    asm.EntryPoint.Invoke(null, null);
                }
            }
        }
    }
}