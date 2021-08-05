#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NativeFileDialog {
    internal static class Dll {
        private const string DllName = "nfd.dll";
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct nfdpathset_t {
            public IntPtr buf;     //nfdchar_t*
            public IntPtr indices; //size_t*
            public IntPtr count;   //size_t
        }

        internal enum nfdresult_t {
            NFD_ERROR  = 0,       /* programmatic error */
            NFD_OKAY   = 1,       /* user pressed okay, or successful return */
            NFD_CANCEL = 2        /* user pressed cancel */
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe nfdresult_t NFD_OpenDialog(IntPtr filterList, IntPtr defaultPath, byte** outPath);
        /* nfdresult_t NFD_OpenDialog( const nfdchar_t *filterList,
                            const nfdchar_t *defaultPath,
                            nfdchar_t **outPath ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe nfdresult_t NFD_OpenDialogMultiple(IntPtr filterList, IntPtr defaultPath, nfdpathset_t* outPaths);
        /* nfdresult_t NFD_OpenDialogMultiple( const nfdchar_t *filterList,
                                    const nfdchar_t *defaultPath,
                                    nfdpathset_t *outPaths ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe nfdresult_t NFD_SaveDialog(IntPtr filterList, IntPtr defaultPath, byte** outPath);
        /* nfdresult_t NFD_SaveDialog( const nfdchar_t *filterList,
                            const nfdchar_t *defaultPath,
                            nfdchar_t **outPath ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern nfdresult_t NFD_PickFolder(IntPtr defaultPath, IntPtr outPath);
        /* nfdresult_t NFD_PickFolder( const nfdchar_t *defaultPath,
                            nfdchar_t **outPath); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr NFD_GetError();
        /* const char * NFD_GetError( void ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr NFD_PathSet_GetCount(IntPtr pathSet);
        /* size_t       NFD_PathSet_GetCount( const nfdpathset_t *pathSet ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr NFD_PathSet_GetPath(IntPtr pathSet, IntPtr index);
        /* nfdchar_t  * NFD_PathSet_GetPath( const nfdpathset_t *pathSet, size_t index ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void NFD_PathSet_Free(IntPtr pathSet);
        /* void         NFD_PathSet_Free( nfdpathset_t *pathSet ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr NFDi_Malloc(IntPtr bytes);
        /* void  * NFDi_Malloc( size_t bytes ); */
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void NFDi_Free(IntPtr ptr);
        /* void    NFDi_Free( void *ptr ); */
    }

    internal static class Helper {
        internal static IntPtr StrToHAlloc(string str) {
            byte[] bytes = Encoding.UTF8.GetBytes(str+"\0");
            IntPtr retVal = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, retVal, bytes.Length);
            return retVal;
        }

        internal static unsafe string PtrToStr(byte* cstr) {
            byte* nullPos = cstr;
            while (*nullPos != 0) { nullPos++; }
            return Encoding.UTF8.GetString(cstr, (int)(nullPos-cstr));
        }

        internal static Result Convert(this Dll.nfdresult_t internalResult)
            => internalResult switch {
                Dll.nfdresult_t.NFD_ERROR => Result.Error,
                Dll.nfdresult_t.NFD_OKAY => Result.Okay,
                Dll.nfdresult_t.NFD_CANCEL => Result.Cancel,
                _ => throw new ArgumentOutOfRangeException(nameof(internalResult), internalResult, null)
            };
    }

    public enum Result {
        Error,
        Okay,
        Cancel
    }

    internal static class GenericDialog {
        internal unsafe delegate Dll.nfdresult_t DialogFunc(IntPtr filterList, IntPtr defaultPathPtr, byte** outPath);
        
        internal static Result Open(string filterList, string defaultPath, DialogFunc dialogFunc, out string outPath) {
            outPath = "";
            unsafe {
                byte* outPathPtr = null;
                byte** outPathPtrPtr = &outPathPtr;
                IntPtr filterListPtr = Helper.StrToHAlloc(filterList);
                IntPtr defaultPathPtr = Helper.StrToHAlloc(defaultPath);

                Dll.nfdresult_t result = dialogFunc(filterListPtr, defaultPathPtr, outPathPtrPtr);

                if (result == Dll.nfdresult_t.NFD_OKAY) {
                    outPath = Helper.PtrToStr(outPathPtr);
                    Dll.NFDi_Free((IntPtr)outPathPtr);
                }
                
                Marshal.FreeHGlobal(filterListPtr);
                Marshal.FreeHGlobal(defaultPathPtr);

                return result.Convert();
            }
        }
    }
    
    public static class OpenDialog {
        public static Result Open(string filterList, string defaultPath, out string outPath) {
            unsafe
            {
                return GenericDialog.Open(filterList, defaultPath, Dll.NFD_OpenDialog, out outPath);
            }
        }
    }
    
    public static class SaveDialog {
        public static Result Open(string filterList, string defaultPath, out string outPath) {
            unsafe
            {
                return GenericDialog.Open(filterList, defaultPath, Dll.NFD_SaveDialog, out outPath);
            }
        }
    }
}
