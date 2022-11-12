using System;
using System.Runtime.InteropServices;

namespace SharpD2D.PInvoke
{
    /// <summary>
    ///     Imports of some API that's not included in the PInvoke package
    /// </summary>
    internal static class User32Ex
    {
        [DllImport("user32.dll")]
        public static extern bool WaitMessage();

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    }
}