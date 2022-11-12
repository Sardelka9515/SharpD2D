using System;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Provides data for the RecreateResources event.
    /// </summary>
    public class RecreateResourcesEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new RecreateResourcesEventArgs using the given graphics object.
        /// </summary>
        /// <param name="graphics"></param>
        public RecreateResourcesEventArgs(Graphics graphics)
        {
            Graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        }

        /// <summary>
        ///     Gets the Graphics object associated with this event.
        /// </summary>
        public Graphics Graphics { get; }
    }
}