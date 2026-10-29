using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Helpers;

/// <summary>
/// DataGrid settings that the theme's row and cell templates read. The DataGrid style copies them onto
/// the grid, and they are inherited by its rows and cells.
/// </summary>
/// <remarks>
/// A template binding that looks for the DataGrid as an ancestor
/// (<c>RelativeSource AncestorType=DataGrid</c>) finds its source a moment after the row is built. When
/// the grid refreshes its rows in between (a new filter, sort or grouping), the row is cut loose first,
/// the binding finds no DataGrid and gives up for good, and the recycled row then shows its row header
/// wherever it is used again. An inherited value follows the row back into the grid.
/// </remarks>
public static class DataGridHelper
{
    public static readonly DependencyProperty HeadersVisibilityProperty = DependencyProperty.RegisterAttached(
        "HeadersVisibility", typeof(DataGridHeadersVisibility), typeof(DataGridHelper),
        new FrameworkPropertyMetadata(DataGridHeadersVisibility.Column, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>The grid's <c>HeadersVisibility</c>, for the row header in the row template.</summary>
    public static DataGridHeadersVisibility GetHeadersVisibility(DependencyObject o) => (DataGridHeadersVisibility)o.GetValue(HeadersVisibilityProperty);
    public static void SetHeadersVisibility(DependencyObject o, DataGridHeadersVisibility value) => o.SetValue(HeadersVisibilityProperty, value);

    public static readonly DependencyProperty AreRowDetailsFrozenProperty = DependencyProperty.RegisterAttached(
        "AreRowDetailsFrozen", typeof(bool), typeof(DataGridHelper),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>The grid's <c>AreRowDetailsFrozen</c>, for the details in the row template.</summary>
    public static bool GetAreRowDetailsFrozen(DependencyObject o) => (bool)o.GetValue(AreRowDetailsFrozenProperty);
    public static void SetAreRowDetailsFrozen(DependencyObject o, bool value) => o.SetValue(AreRowDetailsFrozenProperty, value);

    public static readonly DependencyProperty SelectionUnitProperty = DependencyProperty.RegisterAttached(
        "SelectionUnit", typeof(DataGridSelectionUnit), typeof(DataGridHelper),
        new FrameworkPropertyMetadata(DataGridSelectionUnit.FullRow, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>The grid's <c>SelectionUnit</c>, for the selected look of the cells.</summary>
    public static DataGridSelectionUnit GetSelectionUnit(DependencyObject o) => (DataGridSelectionUnit)o.GetValue(SelectionUnitProperty);
    public static void SetSelectionUnit(DependencyObject o, DataGridSelectionUnit value) => o.SetValue(SelectionUnitProperty, value);
}
