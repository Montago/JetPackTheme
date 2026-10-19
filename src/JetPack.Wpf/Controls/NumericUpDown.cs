using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace JetPack.Wpf.Controls;

/// <summary>Number input with up/down spinner buttons, arrow keys and mouse wheel.</summary>
[TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartUp, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartDown, Type = typeof(ButtonBase))]
public class NumericUpDown : Control
{
    private const string PartTextBox = "PART_TextBox";
    private const string PartUp = "PART_Up";
    private const string PartDown = "PART_Down";

    private TextBox? textBox;

    static NumericUpDown()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
    }

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double?>), typeof(NumericUpDown));

    public event RoutedPropertyChangedEventHandler<double?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MinValue, OnRangeChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue, OnRangeChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
        nameof(Interval), typeof(double), typeof(NumericUpDown), new PropertyMetadata(1d));

    public double Interval
    {
        get => (double)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
        nameof(StringFormat), typeof(string), typeof(NumericUpDown), new PropertyMetadata("G", (d, _) => ((NumericUpDown)d).UpdateText()));

    /// <summary>Format used to show the value, e.g. "N1" or "0.00 USD".</summary>
    public string StringFormat
    {
        get => (string)GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public static readonly DependencyProperty HideUpDownButtonsProperty = DependencyProperty.Register(
        nameof(HideUpDownButtons), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

    public bool HideUpDownButtons
    {
        get => (bool)GetValue(HideUpDownButtonsProperty);
        set => SetValue(HideUpDownButtonsProperty, value);
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var n = (NumericUpDown)d;
        if (baseValue is not double v)
        {
            return baseValue;
        }
        return Math.Max(n.Minimum, Math.Min(n.Maximum, v));
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => d.CoerceValue(ValueProperty);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var n = (NumericUpDown)d;
        n.UpdateText();
        n.RaiseEvent(new RoutedPropertyChangedEventArgs<double?>((double?)e.OldValue, (double?)e.NewValue, ValueChangedEvent));
    }

    public override void OnApplyTemplate()
    {
        if (textBox != null)
        {
            textBox.LostKeyboardFocus -= OnTextBoxLostFocus;
            textBox.PreviewKeyDown -= OnTextBoxKeyDown;
        }

        base.OnApplyTemplate();

        textBox = GetTemplateChild(PartTextBox) as TextBox;
        if (textBox != null)
        {
            textBox.LostKeyboardFocus += OnTextBoxLostFocus;
            textBox.PreviewKeyDown += OnTextBoxKeyDown;
        }
        if (GetTemplateChild(PartUp) is ButtonBase up)
        {
            up.Click += (_, _) => Step(1);
        }
        if (GetTemplateChild(PartDown) is ButtonBase down)
        {
            down.Click += (_, _) => Step(-1);
        }
        UpdateText();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (ReferenceEquals(e.NewFocus, this) && textBox != null)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (IsKeyboardFocusWithin && !IsReadOnly)
        {
            Step(e.Delta > 0 ? 1 : -1);
            e.Handled = true;
        }
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                Step(1);
                e.Handled = true;
                break;
            case Key.Down:
                Step(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                CommitText();
                e.Handled = true;
                break;
        }
    }

    private void OnTextBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitText();

    private void Step(int direction)
    {
        if (IsReadOnly)
        {
            return;
        }
        CommitText();
        var start = Value ?? Math.Max(Minimum, Math.Min(Maximum, 0));
        Value = Math.Round(start + direction * Interval, 10);
        textBox?.SelectAll();
    }

    private void CommitText()
    {
        if (textBox == null)
        {
            return;
        }

        var text = textBox.Text.Trim();
        if (text.Length == 0)
        {
            Value = null;
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var digits = new string(text.Where(c => char.IsDigit(c) || c == '-' || culture.NumberFormat.NumberDecimalSeparator.Contains(c)).ToArray());
        if (double.TryParse(digits, NumberStyles.Number, culture, out var parsed))
        {
            Value = parsed;
        }
        UpdateText();
    }

    private void UpdateText()
    {
        if (textBox == null)
        {
            return;
        }
        var culture = CultureInfo.CurrentCulture;
        textBox.Text = Value?.ToString(StringFormat, culture) ?? string.Empty;
    }
}
