﻿using SharpDX.Direct2D1;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents a Brush used to draw with a Graphics surface.
    /// </summary>
    public interface IBrush
    {
        /// <summary>
        ///     Gets or sets the Brush
        /// </summary>
        Brush Brush { get; set; }
    }
}