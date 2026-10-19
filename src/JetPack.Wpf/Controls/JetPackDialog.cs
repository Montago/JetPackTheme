using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JetPack.Wpf.Controls;

public enum MessageDialogResult
{
    Canceled,
    Affirmative,
    Negative,
}

/// <summary>
/// In-window dialog in the ChildWindow style: light title strip, grey body,
/// buttons bottom right. Show it with <see cref="JetPackWindow.ShowDialogAsync"/>.
/// </summary>
[TemplatePart(Name = "PART_Affirmative", Type = typeof(Button))]
[TemplatePart(Name = "PART_Negative", Type = typeof(Button))]
[TemplatePart(Name = "PART_Close", Type = typeof(Button))]
public class JetPackDialog : ContentControl
{
    private TaskCompletionSource<MessageDialogResult>? completion;

    static JetPackDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(JetPackDialog), new FrameworkPropertyMetadata(typeof(JetPackDialog)));
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(JetPackDialog), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty AffirmativeTextProperty = DependencyProperty.Register(
        nameof(AffirmativeText), typeof(string), typeof(JetPackDialog), new PropertyMetadata("OK"));

    public string AffirmativeText
    {
        get => (string)GetValue(AffirmativeTextProperty);
        set => SetValue(AffirmativeTextProperty, value);
    }

    public static readonly DependencyProperty NegativeTextProperty = DependencyProperty.Register(
        nameof(NegativeText), typeof(string), typeof(JetPackDialog), new PropertyMetadata(null));

    /// <summary>Text of the second button; the button is hidden when null.</summary>
    public string? NegativeText
    {
        get => (string?)GetValue(NegativeTextProperty);
        set => SetValue(NegativeTextProperty, value);
    }

    public static readonly DependencyProperty AffirmativeButtonStyleProperty = DependencyProperty.Register(
        nameof(AffirmativeButtonStyle), typeof(Style), typeof(JetPackDialog), new PropertyMetadata(null));

    /// <summary>Style of the main button, e.g. <c>JetPack.Styles.Button.Orange</c> for a destructive action.</summary>
    public Style? AffirmativeButtonStyle
    {
        get => (Style?)GetValue(AffirmativeButtonStyleProperty);
        set => SetValue(AffirmativeButtonStyleProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Affirmative") is Button ok)
        {
            ok.Click += (_, _) => Close(MessageDialogResult.Affirmative);
            Dispatcher.BeginInvoke(() => ok.Focus(), System.Windows.Threading.DispatcherPriority.Input);
        }
        if (GetTemplateChild("PART_Negative") is Button no)
        {
            no.Click += (_, _) => Close(MessageDialogResult.Negative);
        }
        if (GetTemplateChild("PART_Close") is Button close)
        {
            close.Click += (_, _) => Close(MessageDialogResult.Canceled);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close(MessageDialogResult.Canceled);
            e.Handled = true;
        }
    }

    internal Task<MessageDialogResult> WaitForResultAsync()
    {
        completion = new TaskCompletionSource<MessageDialogResult>();
        return completion.Task;
    }

    public void Close(MessageDialogResult result) => completion?.TrySetResult(result);
}
