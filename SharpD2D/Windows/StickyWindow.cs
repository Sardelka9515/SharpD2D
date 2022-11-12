using System;
using SharpD2D.Drawing;
using static PInvoke.User32;

namespace SharpD2D.Windows
{
    /// <summary>
    ///     Represents a StickyWindow which uses a Canvas sticks to a parent window.
    /// </summary>
    public class StickyWindow : OverlayWindow
    {
        private long _lastStick;

        /// <summary>
        ///     Initializes a new StickyWindow with the given window position and size and the window handle of the parent window.
        /// </summary>
        /// <param name="parentWindow">An IntPtr representing the parent windows handle.</param>
        /// <param name="device">Optionally specify a Graphics device to use.</param>
        public StickyWindow(IntPtr parentWindow, Graphics device = null) : base(device)
        {
            if (!IsWindow(parentWindow)) throw new ArgumentException("Not a window", nameof(parentWindow));

            ParentWindowHandle = parentWindow;
        }

        /// <summary>
        ///     Gets or sets a Boolean which indicates whether to stick to the parents client area.
        /// </summary>
        public bool AttachToClientArea { get; set; }

        /// <summary>
        ///     Gets or sets a Boolean which indicates whether to bypass the need of the windows Topmost flag.
        /// </summary>
        public bool BypassTopmost { get; set; }

        /// <summary>
        ///     Gets or Sets an IntPtr which is used to identify the parent window.
        /// </summary>
        public IntPtr ParentWindowHandle { get; set; }

        /// <inheritdoc />
        protected override void OnDrawGraphics(int frameCount, long frameTime, long deltaTime)
        {
            if (frameTime - _lastStick > 34)
            {
                if (BypassTopmost) PlaceAbove(ParentWindowHandle);
                FitTo(ParentWindowHandle, AttachToClientArea);
                _lastStick = frameTime;
            }

            base.OnDrawGraphics(frameCount, frameTime, deltaTime);
        }
    }
}