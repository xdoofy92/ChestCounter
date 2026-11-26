using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>Ayuda oscura con ancho limitado y el alto que necesita el texto, también con escalado DPI.</summary>
[SupportedOSPlatform("windows")]
internal sealed class ThemedToolTip : ToolTip
{
    private const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    private Font _font = Theme.Text();
    private int _padding = 12;
    private readonly Dictionary<Control, string> _anchored = new();
    private readonly System.Windows.Forms.Timer _hover = new();
    private Control? _hovered;

    public ThemedToolTip()
    {
        OwnerDraw = true;
        BackColor = Theme.Card;
        ForeColor = Theme.Ink;
        InitialDelay = 450;
        ReshowDelay = 100;
        AutoPopDelay = 20000;
        _hover.Interval = InitialDelay;
        _hover.Tick += (_, _) =>
        {
            _hover.Stop();
            if (_hovered is not { IsDisposed: false, Visible: true } control
                || !_anchored.TryGetValue(control, out var text) || string.IsNullOrEmpty(text)
                || !control.ClientRectangle.Contains(control.PointToClient(Cursor.Position))) return;
            var bounds = Screen.FromControl(control).WorkingArea;
            var size = Prepare(control, text);
            var location = Place(Cursor.Position, size, bounds, (int)Math.Round(16 * control.DeviceDpi / 96f));
            Show(text, control, control.PointToClient(location), AutoPopDelay);
        };
        Popup += (_, e) =>
        {
            e.ToolTipSize = Prepare(e.AssociatedControl,
                _anchored.GetValueOrDefault(e.AssociatedControl) ?? GetToolTip(e.AssociatedControl) ?? "");
        };
        Draw += (_, e) => PaintTip(e.Graphics, e.Bounds, e.ToolTipText, _font, _padding);
    }

    // Programmatic Show anchors the tip to the control instead of letting native automatic
    // placement choose a screen corner from its original, unwrapped text dimensions.
    internal void SetAnchoredToolTip(Control control, string text)
    {
        if (!_anchored.ContainsKey(control))
        {
            control.MouseEnter += AnchorEnter;
            control.MouseLeave += AnchorLeave;
            control.MouseDown += AnchorLeave;
        }
        if (_anchored.GetValueOrDefault(control) == text) return;
        _anchored[control] = text;
        if (_hovered == control) AnchorLeave(control, EventArgs.Empty);
    }

    private void AnchorEnter(object? sender, EventArgs e)
    {
        _hovered = (Control?)sender;
        _hover.Start();
    }

    private void AnchorLeave(object? sender, EventArgs e)
    {
        _hover.Stop();
        if (sender is Control control) Hide(control);
        _hovered = null;
    }

    private Size Prepare(Control control, string text)
    {
        var scale = control.DeviceDpi / 96f;
        _padding = (int)Math.Round(12 * scale);
        _font.Dispose();
        _font = Theme.Text(9f * scale);
        var width = Math.Min((int)Math.Round(360 * scale), Screen.FromControl(control).WorkingArea.Width - 2 * _padding);
        return Measure(text, _font, _padding, width);
    }

    internal static Point Place(Point cursor, Size size, Rectangle workArea, int gap)
    {
        var x = Math.Clamp(cursor.X + gap, workArea.Left, Math.Max(workArea.Left, workArea.Right - size.Width));
        var y = cursor.Y + gap;
        if (y + size.Height > workArea.Bottom) y = cursor.Y - size.Height - gap;
        y = Math.Clamp(y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - size.Height));
        return new Point(x, y);
    }

    internal static Size Measure(string text, Font font, int padding, int maxWidth)
    {
        var measured = TextRenderer.MeasureText(text, font, new Size(Math.Max(1, maxWidth - padding * 2), int.MaxValue), Flags);
        return new Size(Math.Min(maxWidth, measured.Width + padding * 2), measured.Height + padding * 2);
    }

    internal static void PaintTip(Graphics graphics, Rectangle bounds, string text, Font font, int padding)
    {
        using var fill = new SolidBrush(Theme.Card);
        using var edge = new Pen(Theme.Line);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(edge, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        var content = Rectangle.Inflate(bounds, -padding, -padding);
        TextRenderer.DrawText(graphics, text, font, content, Theme.Ink, Flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hover.Dispose();
            foreach (var control in _anchored.Keys)
            {
                control.MouseEnter -= AnchorEnter;
                control.MouseLeave -= AnchorLeave;
                control.MouseDown -= AnchorLeave;
            }
            _anchored.Clear();
            _font.Dispose();
        }
        base.Dispose(disposing);
    }
}
