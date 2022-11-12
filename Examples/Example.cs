using System.Text;
using SharpD2D.Drawing;
using SharpD2D.Windows;

namespace Examples;

public class Example
{
    private readonly Dictionary<string, SolidBrush> _brushes;
    private readonly Canvas _canvas;
    private readonly Dictionary<string, Font> _fonts;
    private readonly Color _backColor;
    private RectangleF _gridBounds;

    private Geometry _gridGeometry;
    private long _lastRandomSet;
    private Random _random;
    private List<Action<Graphics, float, float>> _randomFigures;

    public Example(Canvas canvas) : this(canvas, new Color(0x33, 0x36, 0x3F))
    {
    }

    public Example(Canvas canvas, Color backColor)
    {
        _backColor = backColor;
        _canvas = canvas;
        _brushes = new Dictionary<string, SolidBrush>();
        _fonts = new Dictionary<string, Font>();
        _canvas.SetupGraphics += SetupGraphics;
        _canvas.DrawGraphics += DrawGraphics;
        _canvas.DestroyGraphics += DestroyGraphics;
        _canvas.Graphics.MeasureFPS = true;
    }

    private void SetupGraphics(object sender, SetupGraphicsEventArgs e)
    {
        var gfx = e.Graphics;

        if (e.RecreateResources)
            foreach (var pair in _brushes)
                pair.Value.Dispose();

        _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
        _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
        _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
        _brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
        _brushes["blue"] = gfx.CreateSolidBrush(0, 0, 255);
        _brushes["background"] = gfx.CreateSolidBrush(_backColor);
        _brushes["grid"] = gfx.CreateSolidBrush(255, 255, 255, 0.2f);
        _brushes["random"] = gfx.CreateSolidBrush(0, 0, 0);

        if (e.RecreateResources) return;

        _fonts["arial"] = gfx.CreateFont("Arial", 12);
        _fonts["consolas"] = gfx.CreateFont("Consolas", 14);

        _gridBounds = new RectangleF(20, 60, gfx.Width - 20, gfx.Height - 20);
        _gridGeometry = gfx.CreateGeometry();

        for (var x = _gridBounds.Left; x <= _gridBounds.Right; x += 20)
        {
            var line = new Line(x, _gridBounds.Top, x, _gridBounds.Bottom);
            _gridGeometry.BeginFigure(line);
            _gridGeometry.EndFigure(false);
        }

        for (var y = _gridBounds.Top; y <= _gridBounds.Bottom; y += 20)
        {
            var line = new Line(_gridBounds.Left, y, _gridBounds.Right, y);
            _gridGeometry.BeginFigure(line);
            _gridGeometry.EndFigure(false);
        }

        _gridGeometry.Close();

        _randomFigures = new List<Action<Graphics, float, float>>
        {
            (g, x, y) => g.DrawRectangle(GetRandomColor(), x + 10, y + 10, x + 110, y + 110, 2.0f),
            (g, x, y) => g.DrawCircle(GetRandomColor(), x + 60, y + 60, 48, 2.0f),
            (g, x, y) => g.DrawRoundedRectangle(GetRandomColor(), x + 10, y + 10, x + 110, y + 110, 8.0f, 2.0f),
            (g, x, y) => g.DrawTriangle(GetRandomColor(), x + 10, y + 110, x + 110, y + 110, x + 60, y + 10, 2.0f),
            (g, x, y) => g.DashedRectangle(GetRandomColor(), x + 10, y + 10, x + 110, y + 110, 2.0f),
            (g, x, y) => g.DashedCircle(GetRandomColor(), x + 60, y + 60, 48, 2.0f),
            (g, x, y) => g.DashedRoundedRectangle(GetRandomColor(), x + 10, y + 10, x + 110, y + 110, 8.0f, 2.0f),
            (g, x, y) => g.DashedTriangle(GetRandomColor(), x + 10, y + 110, x + 110, y + 110, x + 60, y + 10, 2.0f),
            (g, x, y) => g.FillRectangle(GetRandomColor(), x + 10, y + 10, x + 110, y + 110),
            (g, x, y) => g.FillCircle(GetRandomColor(), x + 60, y + 60, 48),
            (g, x, y) => g.FillRoundedRectangle(GetRandomColor(), x + 10, y + 10, x + 110, y + 110, 8.0f),
            (g, x, y) => g.FillTriangle(GetRandomColor(), x + 10, y + 110, x + 110, y + 110, x + 60, y + 10)
        };
    }

    private void DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
    {
        foreach (var pair in _brushes) pair.Value.Dispose();
        foreach (var pair in _fonts) pair.Value.Dispose();
    }

    private void DrawGraphics(object sender, DrawGraphicsEventArgs e)
    {
        var gfx = e.Graphics;
        gfx.BeginScene();
        var padding = 16;
        var infoText = new StringBuilder()
            .Append("FPS: ").Append(gfx.FPS.ToString().PadRight(padding))
            .Append("FrameTime: ").Append(e.FrameTime.ToString().PadRight(padding))
            .Append("FrameCount: ").Append(e.FrameCount.ToString().PadRight(padding))
            .Append("DeltaTime: ").Append(e.DeltaTime.ToString().PadRight(padding))
            .ToString();

        gfx.ClearScene(_brushes["background"]);

        gfx.DrawTextWithBackground(_fonts["consolas"], _brushes["green"], _brushes["black"], 58, 20, infoText);

        gfx.DrawGeometry(_gridGeometry, _brushes["grid"], 1.0f);

        if (_lastRandomSet == 0L || e.FrameTime - _lastRandomSet > 2500) _lastRandomSet = e.FrameTime;

        _random = new Random(unchecked((int)_lastRandomSet));

        for (var row = _gridBounds.Top + 12; row < _gridBounds.Bottom - 120; row += 120)
        for (var column = _gridBounds.Left + 12; column < _gridBounds.Right - 120; column += 120)
            DrawRandomFigure(gfx, column, row);
        gfx.EndScene();
    }

    private void DrawRandomFigure(Graphics gfx, float x, float y)
    {
        var action = _randomFigures[_random.Next(0, _randomFigures.Count)];

        action(gfx, x, y);
    }

    private SolidBrush GetRandomColor()
    {
        var brush = _brushes["random"];

        brush.Color = new Color(_random.Next(0, 256), _random.Next(0, 256), _random.Next(0, 256));

        return brush;
    }
}