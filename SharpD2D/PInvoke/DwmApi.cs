using System;

namespace SharpD2D.PInvoke
{
    internal static class DwmApi
    {
        public delegate void DwmExtendFrameIntoClientAreaDelegate(IntPtr hWnd, ref NativeMargin pMargins);

        public static readonly DwmExtendFrameIntoClientAreaDelegate DwmExtendFrameIntoClientArea;

        static DwmApi()
        {
            var library = DynamicImport.ImportLibrary("dwmapi.dll");

            DwmExtendFrameIntoClientArea =
                DynamicImport.Import<DwmExtendFrameIntoClientAreaDelegate>(library, "DwmExtendFrameIntoClientArea");
        }
    }
}