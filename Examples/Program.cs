using System.Drawing;
using SharpD2D;
using SharpD2D.Windows;
using Color = SharpD2D.Drawing.Color;
using Rectangle = SharpD2D.Drawing.Rectangle;

namespace Examples;

public static class Program
{
    public static void Main(string[] _)
    {
        TimerService.EnableHighPrecisionTimers();
        WindowHelper.DisableScalingGlobal();
        StickWindowExample();
        WinformsControlExample();
        OverlayExample();
    }

    private static void OverlayExample()
    {
        var wind = new OverlayWindow(Rectangle.Create(0, 0, 800, 600))
        {
            FPS = 60
        };
        var ex = new Example(wind, new Color(0x33, 0x36, 0x3F, 128));
        wind.Initialize();
        wind.MessageLoop();
    }

    private static void StickWindowExample()
    {
        var testForm = new Form
        {
            Text = "Sticky window example",
            Size = new Size(500, 300)
        };
        var handle = testForm.Handle;
        OverlayWindow wind = null;
        var windowThread = new Thread(() =>
        {
            wind = new StickyWindow(handle)
            {
                IsVisible = true,

                // Draw on topmost window
                BypassTopmost = true,

                // Attach to client area, excluding window border, title bar, etc..
                AttachToClientArea = true,
                FPS = 60
            };
            var ex = new Example(wind);
            wind.Initialize();

            // Begin the message loop, calling Application.Run() will work as well.
            // Method returns when the window is destroyed (calling Dispose() in another thread or received WM_QUIT message)
            wind.MessageLoop();
        });
        windowThread.SetApartmentState(ApartmentState.STA);
        windowThread.Start();
        Application.Run(testForm);
        wind?.Dispose();
    }

    private static void WinformsControlExample()
    {
        var testForm = new Form
        {
            Text = "HWnd drawing example",
            Size = new Size(500, 300)
        };
        var con = new D2DControl
        {
            Size = testForm.Size,
            Visible = true,
            Location = default
        };
        testForm.Controls.Add(con);
        testForm.SizeChanged += (s, e) => con.Size = testForm.Size;
        Application.Run(testForm);
    }
}

/// <summary>
///     A simple control that utilizes the <see cref="Canvas" /> class to draw on HWnd based win32 controls
/// </summary>
internal class D2DControl : Control
{
    private Canvas _canvas;
    private Example _example;

    protected override void OnHandleCreated(EventArgs e)
    {
        _canvas = new Canvas(Handle);
        _example = new Example(_canvas);
        _canvas.FPS = 60;
        _canvas.Initialize();
    }
}