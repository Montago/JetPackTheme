using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JetPack.Wpf.Controls;

/// <summary>
/// Check box column for <see cref="JetPackGrid"/> with the JetPack check box. Cells that are not being
/// edited draw the box themselves (<see cref="GridCheckBox"/>), so they scroll as fast as text cells and
/// show in the thumb-drag preview; editing uses a CheckBox with <c>JetPack.Styles.CheckBox.OnLight</c>.
/// The header filter, the search and Ctrl+C read the values as <see cref="TrueText"/> and <see cref="FalseText"/>.
/// </summary>
public class JetPackGridCheckBoxColumn : DataGridCheckBoxColumn
{
    private const string CheckBoxStyleKey = "JetPack.Styles.CheckBox.OnLight";

    public static readonly DependencyProperty IsSearchableProperty = JetPackGrid.IsSearchableProperty.AddOwner(typeof(JetPackGridCheckBoxColumn), new PropertyMetadata(false));

    /// <summary>Whether the grid's search looks in this column (for <see cref="TrueText"/> and <see cref="FalseText"/>); off by default.</summary>
    public bool IsSearchable
    {
        get => (bool)GetValue(IsSearchableProperty);
        set => SetValue(IsSearchableProperty, value);
    }

    public static readonly DependencyProperty IsFilterableProperty = JetPackGrid.IsFilterableProperty.AddOwner(typeof(JetPackGridCheckBoxColumn));

    /// <summary>Whether the header shows the filter button.</summary>
    public bool IsFilterable
    {
        get => (bool)GetValue(IsFilterableProperty);
        set => SetValue(IsFilterableProperty, value);
    }

    public static readonly DependencyProperty FilterMemberPathProperty = JetPackGrid.FilterMemberPathProperty.AddOwner(typeof(JetPackGridCheckBoxColumn));

    /// <summary>Member the search and filter read instead of the Binding.</summary>
    public string? FilterMemberPath
    {
        get => (string?)GetValue(FilterMemberPathProperty);
        set => SetValue(FilterMemberPathProperty, value);
    }

    public static readonly DependencyProperty FilterTextProperty = JetPackGrid.FilterTextProperty.AddOwner(typeof(JetPackGridCheckBoxColumn));

    /// <summary>Values this column must equal, such as "yes" (see <see cref="JetPackGrid.GetFilterText"/>).</summary>
    public string FilterText
    {
        get => (string)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public static readonly DependencyProperty TrueTextProperty = DependencyProperty.Register(
        nameof(TrueText), typeof(string), typeof(JetPackGridCheckBoxColumn), new PropertyMetadata("Yes", JetPackGrid.OnColumnSettingChanged));

    /// <summary>The text of a ticked cell in the header filter, the search and when copying.</summary>
    public string TrueText
    {
        get => (string)GetValue(TrueTextProperty);
        set => SetValue(TrueTextProperty, value);
    }

    public static readonly DependencyProperty FalseTextProperty = DependencyProperty.Register(
        nameof(FalseText), typeof(string), typeof(JetPackGridCheckBoxColumn), new PropertyMetadata("No", JetPackGrid.OnColumnSettingChanged));

    /// <summary>The text of a cell that is not ticked in the header filter, the search and when copying.</summary>
    public string FalseText
    {
        get => (string)GetValue(FalseTextProperty);
        set => SetValue(FalseTextProperty, value);
    }

    // The cell's value; it sets what the drawn check box shows. False, like CheckBox.IsChecked, when the binding fails.
    private static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        "Value", typeof(object), typeof(JetPackGridCheckBoxColumn), new PropertyMetadata(false, (d, e) => ((GridCheckBox)d).IsChecked = e.NewValue as bool?));

    /// <summary>The text for a value: <see cref="TrueText"/>, <see cref="FalseText"/>, or empty for null.</summary>
    internal string GetText(object? value) => value switch
    {
        true => TrueText,
        false => FalseText,
        _ => string.Empty,
    };

    /// <summary>Shows another row's value in a drawn cell (the grid's thumb-drag preview); false for an editing cell.</summary>
    internal static bool ShowPreview(FrameworkElement element, object? value)
    {
        if (element is not GridCheckBox box)
        {
            return false;
        }
        box.IsChecked = value as bool?;
        return true;
    }

    /// <summary>Shows the cell's own value again, after <see cref="ShowPreview"/>.</summary>
    internal static void RestoreValue(FrameworkElement element)
    {
        if (element is GridCheckBox box)
        {
            box.IsChecked = box.GetValue(ValueProperty) as bool?;
        }
    }

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        if (ElementStyle != null && !ReferenceEquals(ElementStyle, DefaultElementStyle))
        {
            return base.GenerateElement(cell, dataItem);
        }
        // One element and one binding per cell instead of a CheckBox and its template.
        var box = new GridCheckBox();
        if (Binding != null)
        {
            BindingOperations.SetBinding(box, ValueProperty, Binding);
        }
        return box;
    }

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
    {
        var element = base.GenerateEditingElement(cell, dataItem);
        if (EditingElementStyle == null || ReferenceEquals(EditingElementStyle, DefaultEditingElementStyle))
        {
            // Where the drawn box sits, so the click that starts the edit lands on the CheckBox and ticks it.
            element.SetResourceReference(FrameworkElement.StyleProperty, CheckBoxStyleKey);
            element.HorizontalAlignment = HorizontalAlignment.Center;
            element.VerticalAlignment = VerticalAlignment.Center;
        }
        return element;
    }

    protected override void RefreshCellContent(FrameworkElement element, string propertyName)
    {
        if (element is DataGridCell { IsEditing: false } cell && propertyName == nameof(ElementStyle))
        {
            // The default style draws the box; another one gets the base CheckBox, and back.
            cell.Content = GenerateElement(cell, cell.DataContext);
            return;
        }
        base.RefreshCellContent(element, propertyName);
    }

    public override object OnCopyingCellClipboardContent(object item) => base.OnCopyingCellClipboardContent(item) switch
    {
        bool value => GetText(value),
        null => string.Empty,
        var other => other,
    };
}
