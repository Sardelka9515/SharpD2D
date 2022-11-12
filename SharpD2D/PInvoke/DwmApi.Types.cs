﻿using System.Runtime.InteropServices;

namespace SharpD2D.PInvoke
{
    // MARGIN struct used with DwmApi
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeMargin
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyBottomHeight;
        public int cyTopHeight;
    }
}