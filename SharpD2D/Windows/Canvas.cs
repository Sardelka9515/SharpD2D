using System;
using System.Diagnostics;
using System.Threading;
using SharpD2D.Drawing;
using static PInvoke.User32;

namespace SharpD2D.Windows
{
    /// <summary>
    ///     Represents a window based rendering surface with fps and multi-threaded drawing support
    /// </summary>
    public class Canvas
    {
        private readonly DrawGraphicsEventArgs _drawArgs = new DrawGraphicsEventArgs();
        private readonly Stopwatch _watch;
        private volatile uint _fps;
        private int _frameCount;
        private long _lastDraw;
        private volatile uint _lastFps;
        private Thread _renderLoopThread;

        /// <summary>
        ///     The synchronization context that gets locked during the drawing operation
        /// </summary>
        public object DrawLock = new object();

        /// <summary>
        ///     Initializes a new Canvas.
        /// </summary>
        /// <param name="handle">Window handle of the rendering target, can also be a handle of a winforms control</param>
        /// <param name="device">Optionally specify a Graphics device to use.</param>
        public Canvas(IntPtr handle, Graphics device = null)
        {
            Handle = handle;
            Graphics = device ?? new Graphics();
            _watch = Stopwatch.StartNew();
        }

        public int Width => Rect.Width;
        public int Height => Rect.Height;

        public int X => Rect.Left;

        public int Y => Rect.Top;

        public Rectangle Rect
        {
            get
            {
                GetWindowRect(Handle, out var rect);
                return rect;
            }
            set
            {
                MoveWindow(Handle, value.Left, value.Top, value.Right - value.Left, value.Bottom - value.Top, true);
                WindowHelper.ExtendFrameIntoClientArea(Handle);
            }
        }

        /// <summary>
        ///     Window handle of the drawing target
        /// </summary>
        public IntPtr Handle { get; protected set; }

        /// <summary>
        ///     Gets or sets the target framerate of this <see cref="Canvas" />.
        /// </summary>
        /// <remarks>
        ///     If set to a value larger than zero, a background thread will be spawned and fire the
        ///     <see cref="DrawGraphics" /> event periodically. By default the actual FPS would be unsatable and not matching the
        ///     given value, call <see cref="TimerService.EnableHighPrecisionTimers()" /> to resolve this probelm globally
        /// </remarks>
        public uint FPS
        {
            get => _fps;
            set
            {
                lock (this)
                {
                    (_fps, _lastFps) = (value, _fps);
                    if (value != 0 && _renderLoopThread?.IsAlive != true)
                    {
                        _renderLoopThread = new Thread(RenderLoop) { IsBackground = true };
                        _renderLoopThread.Start();
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets the used Graphics surface.
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        ///     Gets or sets whether the rendering loop is active. Equivalent of setting the <see cref="FPS" /> to zero(false) or
        ///     last fps or 10(true)
        /// </summary>
        public bool IsRunning
        {
            get => _fps != 0;
            set
            {
                lock (this)
                {
                    if (value)
                        FPS = _lastFps != 0 ? _lastFps : 60;
                    else
                        FPS = 0;
                }
            }
        }

        /// <summary>
        ///     Fires when you should free any device-depedent resources.
        /// </summary>
        public event EventHandler<DestroyGraphicsEventArgs> DestroyGraphics;

        /// <summary>
        ///     Fires when a new Scene / frame needs to be rendered.
        /// </summary>
        public event EventHandler<DrawGraphicsEventArgs> DrawGraphics;

        /// <summary>
        ///     Fires when you should allocate any resources you use to draw using this instance.
        /// </summary>
        public event EventHandler<SetupGraphicsEventArgs> SetupGraphics;

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~Canvas()
        {
            Dispose(false);
        }

        private void RenderLoop()
        {
            while (IsRunning)
            {
                var startTime = _watch.ElapsedMilliseconds;
                lock (this)
                {
                    Initialize();
                    SafeDraw();
                }
                var fps = _fps;
                if (fps == 0) break;
                var remainingTime = (int)(1000 / fps - (_watch.ElapsedMilliseconds - startTime));
                if (remainingTime > 0) TimerService.Methods.DelayExecution(remainingTime);
            }
        }


        /// <summary>
        ///     Locks the <see cref="DrawLock" /> object, calculate frame info and call
        ///     <see cref="OnDrawGraphics(int, long, long)" />, which invokes <see cref="DrawGraphics" /> event.
        /// </summary>
        public void SafeDraw()
        {
            lock (DrawLock)
            {
                if (Graphics.Width != Width || Graphics.Height != Height)
                    Graphics.Resize(Width, Height);
                _frameCount++;
                var curTime = _watch.ElapsedMilliseconds;
                var deltaTime = curTime - _lastDraw;
                _lastDraw = curTime;
                OnDrawGraphics(_frameCount, curTime, deltaTime);
            }
        }

        /// <summary>
        ///     Releases all resources used by this Canvas.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            IsRunning = false;
            Join();
            lock (this)
            {
                OnDestroyGraphics(Graphics);
                Graphics?.Dispose();
            }
        }

        /// <summary>
        ///     Gets called when the graphics surface is about to be destroyed.
        /// </summary>
        /// <param name="graphics">A Graphics surface.</param>
        protected virtual void OnDestroyGraphics(Graphics graphics)
        {
            DestroyGraphics?.Invoke(this, new DestroyGraphicsEventArgs(graphics));
        }

        /// <summary>
        ///     Gets called when the graphics thread needs to render a new Scene / frame or <see cref="SafeDraw" /> is invokded.
        /// </summary>
        /// <param name="frameCount">The number of the currently rendered frame. Starting at 1.</param>
        /// <param name="frameTime">The current time in milliseconds.</param>
        /// <param name="deltaTime">The elapsed time in milliseconds since the last frame.</param>
        /// <remarks>You must call the base method when overriding since the <see cref="DrawGraphics" /> event is invoked here</remarks>
        protected virtual void OnDrawGraphics(int frameCount, long frameTime, long deltaTime)
        {
            if (DrawGraphics == null) return;
            _drawArgs.FrameCount = frameCount;
            _drawArgs.Graphics = Graphics;
            _drawArgs.DeltaTime = deltaTime;
            _drawArgs.FrameTime = frameTime;

            DrawGraphics.Invoke(this, _drawArgs);
        }

        /// <summary>
        ///     Gets called when the graphics thread setups the Graphics surface.
        /// </summary>
        /// <param name="graphics">A Graphics surface.</param>
        protected virtual void OnSetupGraphics(Graphics graphics)
        {
            SetupGraphics?.Invoke(this, new SetupGraphicsEventArgs(graphics));
        }

        /// <summary>
        ///     Update render target window and reinitialize graphics
        /// </summary>
        public void Recreate(IntPtr hwnd = default)
        {
            lock (this)
            {
                Handle = hwnd != default ? hwnd : Handle;
                OnDestroyGraphics(Graphics);
                Graphics.Recreate(Handle);
                OnSetupGraphics(Graphics);
            }
        }

        /// <summary>
        ///     Try to initialize graphic if not already initialized
        /// </summary>
        /// <returns>False if already initialized, otherwise, true</returns>
        public bool Initialize()
        {
            if (!Graphics.IsInitialized)
            {
                Graphics.Width = Width;
                Graphics.Height = Height;
                Graphics.WindowHandle = Handle;
                Graphics.Setup();
                OnSetupGraphics(Graphics);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Block current thread until the execution of all associated threads are finished
        /// </summary>
        public virtual void Join()
        {
            if (Thread.CurrentThread == _renderLoopThread)
                throw new InvalidOperationException("Cannot join a thread inside itself");
            lock (this)
            {
                if (_renderLoopThread?.IsAlive == true) _renderLoopThread.Join();
            }
        }
    }
}