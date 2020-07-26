using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CBRE.Editor.Settings
{
    public static class FileTypeRegistration
    {
        public const string ProgramId = "CBREEditor";
        public const string ProgramIdVer = "1";

        public static FileType[] GetSupportedExtensions()
        {
            return new[]
            {
                new FileType(".vmf", "Valve Map File", true, true),
                new FileType(".rmf", "Worldcraft RMF", true, true),
                new FileType(".map", "Quake MAP Format", true, true),
                new FileType(".3dw", "Leadwerks 3D World Studio File", false, true),

                new FileType(".rmx", "Worldcraft RMF (Hammer Backup)", false, true),
                new FileType(".max", "Quake MAP Format (Hammer Backup)", false, true),
                new FileType(".vmx", "Valve Map File (Hammer Backup)", false, true)
            };
        }

        private static string ExecutableLocation()
        {
            return Assembly.GetEntryAssembly().Location;
        }
    }
}
