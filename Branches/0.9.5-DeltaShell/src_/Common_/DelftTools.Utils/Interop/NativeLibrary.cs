using System;
using System.Runtime.InteropServices;

namespace DelftTools.Utils.Interop
{
    public abstract class NativeLibrary : IDisposable
    {
        [DllImport("kernel32.dll")]
        private extern static IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll")]
        private extern static bool FreeLibrary(IntPtr lib);

        private IntPtr lib = IntPtr.Zero;

        protected NativeLibrary(string fileName)
        {
            lib = LoadLibrary(fileName);
        }

        ~NativeLibrary()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (lib == IntPtr.Zero)
            {
                return;
            }

            FreeLibrary(lib);
            
            lib = IntPtr.Zero;
        }

        protected IntPtr Library
        {
            get
            {
                if (lib == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Plug-in library is not loaded");
                }

                return lib;
            }
        }
    }
}