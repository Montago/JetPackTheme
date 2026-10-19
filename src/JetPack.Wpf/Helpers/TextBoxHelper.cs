using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace JetPack.Wpf.Helpers;

/// <summary>
/// Watermark and clear-button support for TextBox, PasswordBox and editable ComboBox.
/// The JetPack styles turn monitoring on, so setting <c>Watermark</c> or
/// <c>ClearTextButton</c> is all an app needs to do.
/// </summary>
public static class TextBoxHelper
{
    public static readonly RoutedUICommand ClearTextCommand = new("Clear", nameof(ClearTextCommand), typeof(TextBoxHelper));

    public static readonly DependencyProperty WatermarkProperty = DependencyProperty.RegisterAttached(
        "Watermark", typeof(string), typeof(TextBoxHelper), new FrameworkPropertyMetadata(string.Empty));

    public static string GetWatermark(DependencyObject o) => (string)o.GetValue(WatermarkProperty);
    public static void SetWatermark(DependencyObject o, string value) => o.SetValue(WatermarkProperty, value);

    public static readonly DependencyProperty ClearTextButtonProperty = DependencyProperty.RegisterAttached(
        "ClearTextButton", typeof(bool), typeof(TextBoxHelper), new FrameworkPropertyMetadata(false));

    public static bool GetClearTextButton(DependencyObject o) => (bool)o.GetValue(ClearTextButtonProperty);
    public static void SetClearTextButton(DependencyObject o, bool value) => o.SetValue(ClearTextButtonProperty, value);

    private static readonly DependencyPropertyKey HasTextPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "HasText", typeof(bool), typeof(TextBoxHelper), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty HasTextProperty = HasTextPropertyKey.DependencyProperty;

    public static bool GetHasText(DependencyObject o) => (bool)o.GetValue(HasTextProperty);

    public static readonly DependencyProperty IsMonitoringProperty = DependencyProperty.RegisterAttached(
        "IsMonitoring", typeof(bool), typeof(TextBoxHelper), new UIPropertyMetadata(false, OnIsMonitoringChanged));

    public static bool GetIsMonitoring(DependencyObject o) => (bool)o.GetValue(IsMonitoringProperty);
    public static void SetIsMonitoring(DependencyObject o, bool value) => o.SetValue(IsMonitoringProperty, value);

    private static readonly CommandBinding ClearBinding = new(ClearTextCommand, OnClearText);

    private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        var on = (bool)e.NewValue;
        switch (d)
        {
            case TextBox tb:
                if (on) tb.TextChanged += OnTextChanged; else tb.TextChanged -= OnTextChanged;
                Update(tb);
                break;
            case PasswordBox pb:
                if (on) pb.PasswordChanged += OnPasswordChanged; else pb.PasswordChanged -= OnPasswordChanged;
                Update(pb);
                break;
            case ComboBox cb:
                if (on) cb.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnComboTextChanged));
                else cb.RemoveHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnComboTextChanged));
                if (on) cb.SelectionChanged += OnComboSelectionChanged; else cb.SelectionChanged -= OnComboSelectionChanged;
                Update(cb);
                break;
        }

        if (on)
        {
            if (!element.CommandBindings.Contains(ClearBinding))
            {
                element.CommandBindings.Add(ClearBinding);
            }
        }
        else
        {
            element.CommandBindings.Remove(ClearBinding);
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e) => Update((DependencyObject)sender);
    private static void OnPasswordChanged(object sender, RoutedEventArgs e) => Update((DependencyObject)sender);
    private static void OnComboTextChanged(object sender, TextChangedEventArgs e) => Update((DependencyObject)sender);
    private static void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e) => Update((DependencyObject)sender);

    private static void Update(DependencyObject d)
    {
        var hasText = d switch
        {
            TextBox tb => tb.Text.Length > 0,
            PasswordBox pb => pb.Password.Length > 0,
            ComboBox cb => cb.IsEditable ? !string.IsNullOrEmpty(cb.Text) : cb.SelectedItem != null,
            _ => false,
        };
        d.SetValue(HasTextPropertyKey, hasText);
    }

    private static void OnClearText(object sender, ExecutedRoutedEventArgs e)
    {
        switch (sender)
        {
            case TextBox tb:
                tb.Clear();
                tb.Focus();
                break;
            case PasswordBox pb:
                pb.Clear();
                pb.Focus();
                break;
            case ComboBox cb:
                if (cb.IsEditable) cb.Text = string.Empty;
                cb.SelectedItem = null;
                break;
        }
        e.Handled = true;
    }
}
