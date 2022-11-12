using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SharpD2D.Drawing;
using SharpD2D.PInvoke;
using static PInvoke.User32;

namespace SharpD2D.Windows
{
    /// <summary>
    ///     Provides methods to interact with windows.
    /// </summary>
    public static class WindowHelper
    {
        private const int MaxRandomStringLen = 16;
        private const int MinRandomStringLen = 8;

        private static readonly Lazy<IntPtr> _accentPolicyBuffer = new Lazy<IntPtr>(() =>
        {
            var buffer = Marshal.AllocHGlobal((int)AccentPolicy.MemorySize);

            var policy = new AccentPolicy
            {
                AccentFlags = 2,
                AccentState = AccentState.EnableBlurBehind,
                AnimationId = 0,
                GradientColor = 0
            };

            Marshal.StructureToPtr(policy, buffer, true);

            return buffer;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly object _blacklistLock = new object();
        private static readonly Random _random = new Random();
        private static readonly List<string> _windowClassesBlacklist = new List<string>();

        private static string GenerateRandomAsciiString(int minLength, int maxLength)
        {
            var length = _random.Next(minLength, maxLength);

            var chars = new char[length];

            for (var i = 0; i < chars.Length; i++)
                chars[i] = (char)_random.Next(97, 123); // ascii range for small letters

            return new string(chars);
        }

        /// <summary>
        ///     Disable scaling for all windows in this process by calling SetProcessDpiAware win32 function
        /// </summary>
        public static void DisableScalingGlobal()
        {
            SetProcessDPIAware();
        }

        private static bool GetWindowClientInternal(IntPtr hwnd, out Rectangle rect)
        {
            rect = new Rectangle();
            if (!GetWindowRect(hwnd, out var wRect)) return false;
            if (!GetClientRect(hwnd, out var cRect)) return true;
            rect = wRect;
            Rectangle client = cRect;
            if (rect.Width == client.Width && rect.Height == client.Height) return true;

            if (client.Width != rect.Width)
            {
                var difX = client.Width > rect.Width ? client.Width - rect.Width : rect.Width - client.Width;
                difX /= 2;

                rect.Right -= difX;
                rect.Left += difX;

                if (client.Height != rect.Height)
                {
                    var difY = client.Height > rect.Height ? client.Height - rect.Height : rect.Height - client.Height;

                    rect.Top += difY - difX;
                    rect.Bottom -= difX;
                }
            }
            else if (client.Height != rect.Height)
            {
                var difY = client.Height > rect.Height ? client.Height - rect.Height : rect.Height - client.Height;
                difY /= 2;

                rect.Bottom -= difY;
                rect.Top += difY;
            }

            return true;
        }

        /// <summary>
        ///     Enables the blur effect for a window and makes it translucent.
        /// </summary>
        /// <param name="hwnd">A valid handle to a window. The desktop window is not supported.</param>
        public static void EnableBlurBehind(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            var data = new WindowCompositionAttributeData
            {
                Attribute = (uint)WindowCompositionAttribute.AccentPolicy,
                Data = _accentPolicyBuffer.Value,
                DataSize = AccentPolicy.MemorySize
            };

            Undocumented.SetWindowCompositionAttribute(hwnd, ref data);
        }

        /// <summary>
        ///     Extends a windows frame into the client area of the window.
        /// </summary>
        /// <param name="hwnd">A IntPtr representing the handle of a window.</param>
        public static void ExtendFrameIntoClientArea(IntPtr hwnd)
        {
            var margin = new NativeMargin
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyBottomHeight = -1,
                cyTopHeight = -1
            };

            DwmApi.DwmExtendFrameIntoClientArea(hwnd, ref margin);
        }

        /// <summary>
        ///     Searches for the first child window matching the search criterias.
        /// </summary>
        /// <param name="parentWindow">A window handle.</param>
        /// <param name="childWindowName">The window title of the child window. Can be null.</param>
        /// <param name="childClassName">The window class of the child window. Can be null.</param>
        /// <param name="childAfter">
        ///     A handle to a child window. The search begins with the next child window in the Z order. The child window must be a
        ///     direct child window of
        ///     hwndParent, not just a descendant window.
        /// </param>
        /// <returns>Returns the matching window handle or IntPtr.Zero if none matches.</returns>
        public static IntPtr FindChildWindow(IntPtr parentWindow, string childWindowName = null,
            string childClassName = null, IntPtr childAfter = default)
        {
            if (string.IsNullOrEmpty(childWindowName)) childWindowName = null;
            if (string.IsNullOrEmpty(childClassName)) childClassName = null;

            return FindWindowEx(parentWindow, childAfter, childClassName, childWindowName);
        }

        /// <summary>
        ///     Searches for the first window matching the given parameters.
        /// </summary>
        /// <param name="title">The window name. Can be null.</param>
        /// <param name="className">The windows class name. Can be null.</param>
        /// <returns>Returns the matching window handle or IntPtr.Zero if none matches.</returns>
        public static IntPtr FindWindow(string title, string className = null)
        {
            return string.IsNullOrEmpty(className)
                ? FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, title)
                : FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, title);
        }

        /// <summary>
        ///     Generates a random window class name.
        /// </summary>
        /// <returns>The string this method creates.</returns>
        public static string GenerateRandomClass()
        {
            lock (_blacklistLock)
            {
                while (true)
                {
                    var name = GenerateRandomAsciiString(MinRandomStringLen, MaxRandomStringLen);

                    if (!_windowClassesBlacklist.Contains(name))
                    {
                        _windowClassesBlacklist.Add(name);

                        return name;
                    }
                }
            }
        }

        /// <summary>
        ///     Generates a random window title.
        /// </summary>
        /// <returns>The string this method creates.</returns>
        public static string GenerateRandomTitle()
        {
            return GenerateRandomAsciiString(MinRandomStringLen, MaxRandomStringLen);
        }

        /// <summary>
        ///     Adds the topmost flag to a window.
        /// </summary>
        /// <param name="hwnd">A IntPtr representing the handle of a window.</param>
        public static void MakeTopmost(IntPtr hwnd)
        {
            SetWindowPos(hwnd, (IntPtr)(-1), 0, 0, 0, 0,
                SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE |
                SetWindowPosFlags.SWP_NOSIZE);
        }

        /// <summary>
        ///     Removes the topmost flag from a window.
        /// </summary>
        /// <param name="hwnd">A IntPtr representing the handle of a window.</param>
        public static void RemoveTopmost(IntPtr hwnd)
        {
            SetWindowPos(hwnd, (IntPtr)(-2), 0, 0, 0, 0,
                SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
        }
    }
}