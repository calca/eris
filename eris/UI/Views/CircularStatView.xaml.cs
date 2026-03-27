namespace eris.UI.Views;

public partial class CircularStatView : ContentView
{
    private readonly RingDrawable _drawable = new();

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(CircularStatView), "0");

    public static readonly BindableProperty StatLabelProperty =
        BindableProperty.Create(nameof(StatLabel), typeof(string), typeof(CircularStatView), "");

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CircularStatView), "");

    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(CircularStatView), null);

    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(float), typeof(CircularStatView), 0.85f,
            propertyChanged: (b, _, _2) => ((CircularStatView)b).Refresh());

    public string Value     { get => (string)GetValue(ValueProperty);     set => SetValue(ValueProperty, value); }
    public string StatLabel { get => (string)GetValue(StatLabelProperty); set => SetValue(StatLabelProperty, value); }
    public string Icon      { get => (string)GetValue(IconProperty);      set => SetValue(IconProperty, value); }
    public ImageSource IconSource { get => (ImageSource)GetValue(IconSourceProperty); set => SetValue(IconSourceProperty, value); }
    public float  Progress  { get => (float)GetValue(ProgressProperty);   set => SetValue(ProgressProperty, value); }

    public CircularStatView()
    {
        InitializeComponent();
        RingView.Drawable = _drawable;
        UpdateTrackColor();
        Application.Current!.RequestedThemeChanged += (_, _) => UpdateTrackColor();
    }

    private void UpdateTrackColor()
    {
        var isDark = Application.Current!.RequestedTheme == AppTheme.Dark;
        _drawable.TrackColor = isDark
            ? Color.FromArgb("#1e293b")
            : Color.FromArgb("#e2e8f0");
        RingView.Invalidate();
    }

    private void Refresh()
    {
        _drawable.Progress = Progress;
        RingView.Invalidate();
    }
}

internal sealed class RingDrawable : IDrawable
{
    public float Progress { get; set; } = 0.85f;
    public Color TrackColor { get; set; } = Color.FromArgb("#1e293b");

    private static readonly Color ArcColor   = Color.FromArgb("#0ea5e9");

    public void Draw(ICanvas canvas, RectF rect)
    {
        const float sw = 10f;
        float cx     = rect.Width  / 2f;
        float cy     = rect.Height / 2f;
        float radius = Math.Min(cx, cy) - sw;

        canvas.Antialias = true;

        // Background track
        canvas.StrokeColor = TrackColor;
        canvas.StrokeSize  = sw;
        canvas.DrawCircle(cx, cy, radius);

        float p = Math.Clamp(Progress, 0f, 1f);
        if (p <= 0f) return;

        canvas.StrokeColor   = ArcColor;
        canvas.StrokeSize    = sw;
        canvas.StrokeLineCap = LineCap.Round;

        // When full, AddArc start==end (both map to 12 o'clock) → draws nothing.
        // Use DrawCircle directly instead.
        if (p >= 1f)
        {
            canvas.DrawCircle(cx, cy, radius);
            return;
        }

        // Foreground arc — starts at 12 o'clock, goes clockwise
        float startAngle = -90f;
        float endAngle   = startAngle + 360f * p;

        var path = new PathF();
        path.AddArc(cx - radius, cy - radius, cx + radius, cy + radius,
                    startAngle, endAngle, clockwise: false);
        canvas.DrawPath(path);
    }
}
