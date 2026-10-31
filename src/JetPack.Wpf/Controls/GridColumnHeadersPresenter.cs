using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace JetPack.Wpf.Controls;

/// <summary>
/// The header row of the DataGrid and <see cref="JetPackGrid"/> templates: fills
/// <see cref="JetPackGrid.HeaderColumnProperty"/> on each header as it gets or loses its column.
/// </summary>
/// <remarks>
/// <c>DataGridColumnHeader.Column</c> does not notify, and no property of the header changes reliably when a
/// recycled header gets its column back: with column virtualization a header that leaves the visual tree and
/// returns for the same column keeps its DisplayIndex. Preparing the container is the one moment it always
/// passes through.
/// </remarks>
public sealed class GridColumnHeadersPresenter : DataGridColumnHeadersPresenter
{
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is DataGridColumnHeader header)
        {
            JetPackGrid.SetHeaderColumn(header, header.Column);
        }
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        base.ClearContainerForItemOverride(element, item);
        if (element is DataGridColumnHeader header)
        {
            JetPackGrid.SetHeaderColumn(header, null);
        }
    }
}
