using System.Diagnostics.CodeAnalysis;
using CBRE.DataStructures.Models;
using CBRE.RMesh;

namespace RMeshDecomp;

static class Program {
    static void Log(string msg, ConsoleColor color = ConsoleColor.White) {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
    }
    
    public static void Main(string[] args) {
        if (!args.Any()) {
            Log("No input files defined! Just drag and drop files onto CBRE.SMFConverter.exe", ConsoleColor.Red);
        } else {
            foreach (string filePath in args) {
                DecompileRMesh(filePath);
            }
        }
    }

    static void DecompileRMesh(string filePath) {
        RMesh rmesh = RMesh.Loader.FromFile(filePath, null);
    }
}
