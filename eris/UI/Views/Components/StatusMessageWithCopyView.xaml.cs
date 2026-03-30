using System.Windows.Input;

namespace eris.UI.Views.Components;

public sealed partial class StatusMessageWithCopyView : ContentView
{
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(StatusMessageWithCopyView), string.Empty);

    public static readonly BindableProperty CopyCommandProperty =
        BindableProperty.Create(nameof(CopyCommand), typeof(ICommand), typeof(StatusMessageWithCopyView));

    public static readonly BindableProperty MessageFontSizeProperty =
        BindableProperty.Create(nameof(MessageFontSize), typeof(double), typeof(StatusMessageWithCopyView), 14d);

    public static readonly BindableProperty MessageFontAttributesProperty =
        BindableProperty.Create(nameof(MessageFontAttributes), typeof(FontAttributes), typeof(StatusMessageWithCopyView), FontAttributes.Bold);

    public static readonly BindableProperty MessageHorizontalTextAlignmentProperty =
        BindableProperty.Create(nameof(MessageHorizontalTextAlignment), typeof(TextAlignment), typeof(StatusMessageWithCopyView), TextAlignment.Start);

    public StatusMessageWithCopyView()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ICommand? CopyCommand
    {
        get => (ICommand?)GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }

    public double MessageFontSize
    {
        get => (double)GetValue(MessageFontSizeProperty);
        set => SetValue(MessageFontSizeProperty, value);
    }

    public FontAttributes MessageFontAttributes
    {
        get => (FontAttributes)GetValue(MessageFontAttributesProperty);
        set => SetValue(MessageFontAttributesProperty, value);
    }

    public TextAlignment MessageHorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(MessageHorizontalTextAlignmentProperty);
        set => SetValue(MessageHorizontalTextAlignmentProperty, value);
    }
}
