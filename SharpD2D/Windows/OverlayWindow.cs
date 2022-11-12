using System;
using System.Runtime.InteropServices;
using System.Threading;
using PInvoke;
using SharpD2D.Drawing;
using static SharpD2D.PInvoke.User32Ex;
using static PInvoke.User32;

namespace SharpD2D.Windows
{
    /// <summary>
    ///     Represents a transparent overlay window.
    /// </summary>
    public class OverlayWindow : Canvas, IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        public delegate void WmDelegate(WindowMessage msg, IntPtr wParam, IntPtr lParam);

        private readonly Thread _windowThread;
        private readonly WndProc _wndProc;

        /// <summary>
        ///     Class name of this window
        /// </summary>
        public readonly string ClassName;

        /// <summary>
        ///     Gets the windows menu name.
        /// </summary>
        public readonly string MenuName;

        /// <summary>
        ///     Initializes a new OverlayWindow.
        /// </summary>
        public OverlayWindow(Graphics gfx = null) : this(Rectangle.Create(0, 0, 800, 600), gfx)
        {
        }

        /// <summary>
        ///     Create a new overlay window with given position and size
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="className"></param>
        /// <param name="title"></param>
        /// <param name="gfx"></param>
        /// <exception cref="Exception"></exception>
        public OverlayWindow(Rectangle rect, Graphics gfx = null, string className = null, string title = null) : base(
            default, gfx)
        {
            ClassName = className;
            MenuName = WindowHelper.GenerateRandomTitle();
            if (string.IsNullOrEmpty(ClassName)) ClassName = WindowHelper.GenerateRandomClass();
            unsafe
            {
                // Need to assign it somewhere to ensure it won't be garbage-collected
                _wndProc = WindowProcedure;

                fixed (char* lpMenu = MenuName, lpCLass = ClassName)
                {
                    var wndClassEx = new WNDCLASSEX
                    {
                        cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                        style = 0,
                        lpfnWndProc = _wndProc,
                        lpszMenuName = lpMenu,
                        lpszClassName = lpCLass
                    };
                    if (RegisterClassEx(ref wndClassEx) == 0) throw new Exception("Failed to register window class");
                }
            }

            InstantiateNewWindow(rect, title);
            IsVisible = true;
            WindowHelper.ExtendFrameIntoClientArea(Handle);
            _windowThread = Thread.CurrentThread;
        }

        /// <summary>
        ///     Gets or sets the window style
        /// </summary>
        public WindowStyles Style
        {
            get => (WindowStyles)GetWindowLong(Handle, WindowLongIndexFlags.GWL_STYLE);
            set => SetWindowLong(Handle, WindowLongIndexFlags.GWL_STYLE, (SetWindowLongFlags)value);
        }

        /// <summary>
        ///     Gets or sets the extended window style
        /// </summary>
        public WindowStylesEx StyleEx
        {
            get => (WindowStylesEx)GetWindowLong(Handle, WindowLongIndexFlags.GWL_EXSTYLE);
            set => SetWindowLong(Handle, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags)value);
        }

        /// <summary>
        ///     Retrieves the window info
        /// </summary>
        public WINDOWINFO Info
        {
            get
            {
                var info = new WINDOWINFO();
                if (!GetWindowInfo(Handle, ref info)) throw new Exception("Failed to get window info");
                return info;
            }
        }

        /// <summary>
        ///     Gets or sets a Boolean indicating whether this window is topmost.
        /// </summary>
        public bool IsTopmost
        {
            get => ((SetWindowLongFlags)StyleEx).HasFlag(SetWindowLongFlags.WS_EX_TOPMOST);
            set
            {
                if (value)
                    WindowHelper.MakeTopmost(Handle);
                else
                    WindowHelper.RemoveTopmost(Handle);
            }
        }

        /// <summary>
        ///     Gets or sets a Boolean indicating whether this window is visible.
        /// </summary>
        public bool IsVisible
        {
            get => IsWindowVisible(Handle);
            set => ShowWindow(Handle, value ? WindowShowStyle.SW_SHOW : WindowShowStyle.SW_HIDE);
        }

        /// <summary>
        ///     Gets or sets the windows title.
        /// </summary>
        public string Title
        {
            get => GetWindowText(Handle);
            set => SetWindowText(Handle, value);
        }

        /// <summary>
        ///     Invoked when a window message is received, only works if you run <see cref="MessageLoop" />.
        /// </summary>
        public event WmDelegate WindowMessageReceived;

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~OverlayWindow()
        {
            Dispose(false);
        }

        private void DestroyWindow()
        {
            lock (this)
            {
                if (!IsWindow(Handle))
                    throw new InvalidOperationException("Window not found, probably already destroyed");
                PostMessage(Handle, WindowMessage.WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
                Handle = default;
            }
        }

        private void InstantiateNewWindow(Rectangle rect, string title)
        {
            var styleEx = WindowStylesEx.WS_EX_TRANSPARENT | WindowStylesEx.WS_EX_TOPMOST |
                          WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_NOACTIVATE;
            var style = WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE;
            Handle = CreateWindowEx(
                styleEx,
                ClassName,
                title,
                style,
                rect.Left, rect.Top,
                rect.Width, rect.Height,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            SetLayeredWindowAttributes(Handle, 0, 255, 0x2); //alpha
            UpdateWindow(Handle);
        }

        private unsafe IntPtr WindowProcedure(IntPtr hWnd, WindowMessage msg, void* wParam, void* lParam)
        {
            WindowMessageReceived?.Invoke(msg, (IntPtr)wParam, (IntPtr)lParam);
            switch (msg)
            {
                case WindowMessage.WM_ERASEBKGND:
                    SendMessage(hWnd, WindowMessage.WM_PAINT, (IntPtr)0, (IntPtr)0);
                    break;

                case WindowMessage.WM_IME_KEYUP:
                case WindowMessage.WM_IME_KEYDOWN:
                case WindowMessage.WM_SYSCOMMAND:
                case WindowMessage.WM_SYSKEYDOWN:
                case WindowMessage.WM_SYSKEYUP:
                case WindowMessage.WM_DPICHANGED:
                case WindowMessage.WM_NCPAINT:
                case WindowMessage.WM_PAINT:
                    return (IntPtr)0;

                case WindowMessage.WM_DWMCOMPOSITIONCHANGED:
                    WindowHelper.ExtendFrameIntoClientArea(hWnd);
                    return (IntPtr)0;

                case WindowMessage.WM_DESTROY:
                case WindowMessage.WM_NCDESTROY:
                    PostQuitMessage(0);
                    break;
            }

            return DefWindowProc(hWnd, msg, (IntPtr)wParam, (IntPtr)lParam);
        }

        /// <summary>
        ///     Run the message loop to receive WM messages, must be called from the thread that this window is created
        /// </summary>
        public unsafe void MessageLoop()
        {
            if (!IsWindow(Handle))
                throw new InvalidOperationException(
                    "Invalid handle, this may indicate this object is already disposed");

            if (Thread.CurrentThread != _windowThread)
                throw new InvalidOperationException(
                    "The message loop must be run in the same thread this window is created");

            MSG message = default;
            while (GetMessage(&message, Handle, default, default) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
                WaitMessage();
            }

            Dispose();
        }

        /// <summary>
        ///     Returns a value indicating whether this instance and a specified <see cref="T:System.Object" /> represent the same
        ///     type and value.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="obj" /> is a OverlayWindow and equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj as Canvas)?.Handle == Handle && (Handle != default || ReferenceEquals(this, obj));
        }

        /// <summary>
        ///     Adapts to another window in the position and size.
        /// </summary>
        /// <param name="windowHandle">The target window handle.</param>
        /// <param name="attachToClientArea">A Boolean determining whether to fit to the client area of the target window.</param>
        public void FitTo(IntPtr windowHandle, bool attachToClientArea = false)
        {
            if (attachToClientArea)
            {
                POINT clientPoint = default;
                if (GetClientRect(windowHandle, out var client) && ClientToScreen(windowHandle, ref clientPoint))
                {
                    Rectangle clientRect = client;
                    clientRect.Location = clientPoint;
                    Rect = clientRect;
                }
            }
            else if (GetWindowRect(windowHandle, out var rect))
            {
                Rect = rect;
            }
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        /// <summary>
        ///     Places the OverlayWindow above the target window according to the windows z-order.
        /// </summary>
        /// <param name="windowHandle">The target window handle.</param>
        public void PlaceAbove(IntPtr windowHandle)
        {
            var windowAboveParentWindow = GetWindow(windowHandle, GetWindowCommands.GW_HWNDPREV);

            if (windowAboveParentWindow != Handle)
                SetWindowPos(
                    Handle,
                    windowAboveParentWindow,
                    0, 0, 0, 0,
                    SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE |
                    SetWindowPosFlags.SWP_ASYNCWINDOWPOS);
        }

        /// <summary>
        ///     Converts this OverlayWindow structure to a human-readable string.
        /// </summary>
        /// <returns>A string representation of this OverlayWindow.</returns>
        public override string ToString()
        {
            return OverrideHelper.ToString(
                "Handle", Handle.ToString("X"),
                "IsVisible", IsVisible.ToString(),
                "IsTopmost", IsTopmost.ToString(),
                "X", X.ToString(),
                "Y", Y.ToString(),
                "Width", Width.ToString(),
                "Height", Height.ToString());
        }

        #region IDisposable Support

        private bool disposedValue;

        /// <summary>
        ///     Releases all resources used by this OverlayWindow.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (Handle != default) DestroyWindow();
                disposedValue = true;
            }
        }

        /// <summary>
        ///     Releases all resources used by this OverlayWindow.
        /// </summary>
        public void Dispose()
        {
            base.Dispose(true);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}