using System;
using SharpD2D.Drawing;

namespace SharpD2D.Windows
{
    /// <summary>
    ///     Provides data for the DrawGraphics event.
    /// </summary>
    public class DrawGraphicsEventArgs : EventArgs
    {
        /// <summary>
        ///     Gets the elapsed time in milliseconds since the last frame.
        /// </summary>
        public long DeltaTime;

        /// <summary>
        ///     Gets the number of frames rendered in the current loop.
        /// </summary>
        public int FrameCount;

        /// <summary>
        ///     Gets the current time in milliseconds.
        /// </summary>
        public long FrameTime;

        /// <summary>
        ///     Gets the Graphics surface.
        /// </summary>
        public Graphics Graphics;
    }

    /// <summary>
    ///     Provides data for the SetupGraphics event.
    /// </summary>
    public class SetupGraphicsEventArgs : EventArgs
    {
        private SetupGraphicsEventArgs()
        {
        }

        /// <summary>
        ///     Initializes a new SetupGraphicsEventArgs with a Graphics surface.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="recreateResources"></param>
        public SetupGraphicsEventArgs(Graphics graphics, bool recreateResources = false)
        {
            Graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
            RecreateResources = recreateResources;
        }

        /// <summary>
        ///     Gets the Graphics surface.
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        ///     Gets a boolean determining whether resources (brushes and images) have to be created again since the underlying
        ///     device has changed.
        /// </summary>
        public bool RecreateResources { get; }
    }

    /// <summary>
    ///     Provides data for the DestroyGraphics event.
    /// </summary>
    public class DestroyGraphicsEventArgs : EventArgs
    {
        private DestroyGraphicsEventArgs()
        {
        }

        /// <summary>
        ///     Initializes a new DestroyGraphicsEventArgs with a Graphics surface.
        /// </summary>
        /// <param name="graphics"></param>
        public DestroyGraphicsEventArgs(Graphics graphics)
        {
            Graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        }

        /// <summary>
        ///     Gets the Graphics surface.
        /// </summary>
        public Graphics Graphics { get; }
    }
}