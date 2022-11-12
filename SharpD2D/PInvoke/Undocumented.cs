using System;
using System.Runtime.InteropServices;

namespace SharpD2D.PInvoke
{
    internal static class Undocumented
    {
        public delegate uint NtDelayExecutionDelegate(byte bAlertable, ref long delayInterval);

        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool SetWindowCompositionAttributeDelegate(IntPtr hwnd,
            [In] [Out] ref WindowCompositionAttributeData data);

        public static readonly SetWindowCompositionAttributeDelegate SetWindowCompositionAttribute;

        public static readonly NtDelayExecutionDelegate NtDelayExecution;

        static Undocumented()
        {
            var user32 = DynamicImport.ImportLibrary("user32.dll");
            var ntdll = DynamicImport.ImportLibrary("ntdll.dll");

            SetWindowCompositionAttribute =
                DynamicImport.Import<SetWindowCompositionAttributeDelegate>(user32, "SetWindowCompositionAttribute");

            try
            {
                NtDelayExecution = DynamicImport.Import<NtDelayExecutionDelegate>(ntdll, "NtDelayExecution");
            }
            catch
            {
                NtDelayExecution = null;
            }
        }
    }
}