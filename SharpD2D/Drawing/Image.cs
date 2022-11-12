using System;
using System.Globalization;
using System.IO;
using SharpD2D.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents an Image which can be drawn using a Graphics surface.
    /// </summary>
    public class Image : IDisposable
    {
        internal static readonly ImagingFactory ImageFactory = new ImagingFactory();

        private RenderTarget _device;

        /// <summary>
        ///     The SharpDX Bitmap
        /// </summary>
        public Bitmap Bitmap;

        /// <summary>
        ///     Initialize a empty placeholder bitmap with specified size and format
        /// </summary>
        /// <param name="device"></param>
        /// <param name="pixelSize"></param>
        /// <param name="format"></param>
        public Image(RenderTarget device, Size2 pixelSize, PixelFormat format)
        {
            Bitmap = new Bitmap(device, pixelSize, new BitmapProperties(format));
            _device = device;
        }

        /// <summary>
        ///     Initializes a new Image for the given device by using a byte[].
        /// </summary>
        /// <param name="device">The Graphics device.</param>
        /// <param name="bytes">A byte[] containing image data.</param>
        public Image(RenderTarget device, byte[] bytes)
        {
            Bitmap = LoadBitmapFromMemory(device, bytes);
        }

        /// <summary>
        ///     Initializes a new Image for the given device by using a file on disk.
        /// </summary>
        /// <param name="device">The Graphics device.</param>
        /// <param name="path">The path to an image file on disk.</param>
        public Image(RenderTarget device, string path)
        {
            Bitmap = LoadBitmapFromFile(device, path);
        }

        /// <summary>
        ///     Initializes a new Image for the given device by using a byte[].
        /// </summary>
        /// <param name="device">The Graphics device.</param>
        /// <param name="bytes">A byte[] containing image data.</param>
        public Image(Graphics device, byte[] bytes) : this(device.GetRenderTarget(), bytes)
        {
        }

        /// <summary>
        ///     Initializes a new Image for the given device by using a file on disk.
        /// </summary>
        /// <param name="device">The Graphics device.</param>
        /// <param name="path">The path to an image file on disk.</param>
        public Image(Graphics device, string path) : this(device.GetRenderTarget(), path)
        {
        }

        /// <summary>
        ///     Gets the width of this Image
        /// </summary>
        public float Width => Bitmap.PixelSize.Width;

        /// <summary>
        ///     Gets the height of this Image
        /// </summary>
        public float Height => Bitmap.PixelSize.Height;

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~Image()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Returns a value indicating whether this instance and a specified <see cref="T:System.Object" /> represent the same
        ///     type and value.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="obj" /> is a Image and equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Image image)
                return image.Bitmap.NativePointer == Bitmap.NativePointer;
            return false;
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of Image represent the same value.
        /// </summary>
        /// <param name="value">An object to compare to this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="value" /> is equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool Equals(Image value)
        {
            return value != null
                   && value.Bitmap.NativePointer == Bitmap.NativePointer;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return OverrideHelper.HashCodes(
                Bitmap.NativePointer.GetHashCode());
        }

        /// <summary>
        ///     Converts this Image instance to a human-readable string.
        /// </summary>
        /// <returns>A string representation of this Image.</returns>
        public override string ToString()
        {
            return OverrideHelper.ToString(
                "Image", "Bitmap",
                "Width", Width.ToString(CultureInfo.InvariantCulture),
                "Height", Height.ToString(CultureInfo.InvariantCulture),
                "PixelFormat", Bitmap.PixelFormat.Format.ToString());
        }

        /// <summary>
        ///     Updates the underlying bitmap by copying data from the specified memory region
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="pitch">The size of bytes each scan line, aka stride in GDI+</param>
        /// <param name="scan0">Pointer to the start of the first pixel in the bitmap data</param>
        /// <param name="format">Format of the bitmap data</param>
        /// <param name="destination">The destination to copy the data to, only effective if ths size and format are same</param>
        /// <remarks>If the size or format doesn't match the existing one, a new one is created and the old one will be destroyed</remarks>
        public void Update(int width, int height, int pitch, IntPtr scan0, PixelFormat format, Rectangle destination)
        {
            if (Bitmap?.Size != new Size2F(width, height) || format.Format != Bitmap.PixelFormat.Format)
            {
                Bitmap?.Dispose();
                Bitmap = new Bitmap(_device, new Size2(width, height),
                    new DataPointer { Pointer = scan0, Size = pitch * height },
                    pitch, new BitmapProperties(format));
            }
            else
            {
                Bitmap.CopyFromMemory(scan0, pitch, destination);
            }
        }

        /// <summary>
        ///     Updates the underlying bitmap by copying data from the specified memory region
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="pitch">The size of bytes each scan line, aka stride in GDI+</param>
        /// <param name="scan0">Pointer to the start of the first pixel in the bitmap data</param>
        /// <param name="format">Format of the bitmap data</param>
        /// <remarks>If the size or format doesn't match the existing one, a new one is created and the old one will be destroyed</remarks>
        public void Update(int width, int height, int pitch, IntPtr scan0, PixelFormat format) =>
            Update(width, height, pitch, scan0, format, Rectangle.Create(0, 0, Bitmap.PixelSize.Width, Bitmap.PixelSize.Height));

        /// <summary>
        ///     Converts an Image to a SharpDX Bitmap.
        /// </summary>
        /// <param name="image">The Image object.</param>
        public static implicit operator Bitmap(Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            return image.Bitmap;
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of Image represent the same value.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public static bool Equals(Image left, Image right)
        {
            return left?.Equals(right) == true;
        }

        private Bitmap LoadBitmapFromMemory(RenderTarget device, byte[] bytes)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentOutOfRangeException(nameof(bytes));
            _device = device;
            Bitmap bmp = null;
            MemoryStream stream = null;
            BitmapDecoder decoder = null;
            // BitmapFrameDecode frame = null;
            // FormatConverter converter = null;

            try
            {
                stream = new MemoryStream(bytes);
                decoder = new BitmapDecoder(ImageFactory, stream, DecodeOptions.CacheOnDemand);

                bmp = ImageDecoder.Decode(device, decoder);

                decoder.Dispose();
                stream.Dispose();

                return bmp;
            }
            catch
            {
                // if (converter?.IsDisposed == false) converter.Dispose();
                // if (frame?.IsDisposed == false) frame.Dispose();
                if (decoder?.IsDisposed == false) decoder.Dispose();
                if (stream != null) TryCatch(() => stream.Dispose());
                if (bmp?.IsDisposed == false) bmp.Dispose();

                throw;
            }
        }

        private Bitmap LoadBitmapFromFile(RenderTarget device, string path)
        {
            return LoadBitmapFromMemory(device, File.ReadAllBytes(path));
        }

        private static void TryCatch(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                action();
            }
            catch
            {
            }
        }

        #region IDisposable Support

        private bool disposedValue;

        /// <summary>
        ///     Releases all resources used by this Image.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Bitmap?.Dispose();

                disposedValue = true;
            }
        }

        /// <summary>
        ///     Releases all resources used by this Image.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}