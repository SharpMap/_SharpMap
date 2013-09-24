using System.Runtime.InteropServices;

namespace DelftTools.Utils.Interop
{
    public static class NativeWin32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool EnableWindow(HandleRef hWnd, bool enable); 
    }
}