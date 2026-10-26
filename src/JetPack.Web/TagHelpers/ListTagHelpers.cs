using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>An item of a ListBox: <c>&lt;jp-list-box-item content="Charles Hoyle" /&gt;</c>.</summary>
[HtmlTargetElement("jp-list-box-item", ParentTag = "jp-list-box")]
public class ListBoxItemTagHelper : TagHelper
{
    public string? Content { get; set; }

    public string? Value { get; set; }

    public bool IsEnabled { get; set; } = true;

    internal string Html { get; private set; } = "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        Html = Content is null ? (await output.GetChildContentAsync()).GetContent() : TagHelpers.Html.Text(Content);
        ChildList<ListBoxItemTagHelper>.Of(context)?.Items.Add(this);
        output.SuppressOutput();
    }
}

/// <summary>
/// <c>&lt;jp-list-box selected-index="1"&gt;&lt;jp-list-box-item content="…" /&gt;…&lt;/jp-list-box&gt;</c>: single
/// selection with the mouse and the arrow keys; name posts the selected value.
/// </summary>
[HtmlTargetElement("jp-list-box")]
public class ListBoxTagHelper : JetPackTagHelper
{
    public int SelectedIndex { get; set; } = -1;

    public string? Name { get; set; }

    /// <summary>The dark variant for the flyouts and panels.</summary>
    public bool Dark { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var children = ChildList<ListBoxItemTagHelper>.Start(context);
        await output.GetChildContentAsync();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-list-box", Dark ? "jp-list-box--dark" : null);
        output.Attributes.SetAttribute("role", "listbox");
        output.Attributes.SetAttribute("tabindex", "0");
        if (Name is not null) output.Attributes.SetAttribute("data-jp-name", Name);
        if (!IsEnabled) output.Attributes.SetAttribute("aria-disabled", "true");
        ApplyCommon(output);
        var html = string.Concat(children.Items.Select((item, i) =>
        {
            var value = item.Value is null ? "" : $" data-value=\"{Html.Attr(item.Value)}\"";
            var off = item.IsEnabled ? "" : " aria-disabled=\"true\"";
            return $"<div class=\"jp-list-box-item\" role=\"option\" aria-selected=\"{(i == SelectedIndex ? "true" : "false")}\"{value}{off}>{item.Html}</div>";
        }));
        if (Name is not null && SelectedIndex >= 0 && SelectedIndex < children.Items.Count)
        {
            html += $"<input type=\"hidden\" name=\"{Html.Attr(Name)}\" value=\"{Html.Attr(children.Items[SelectedIndex].Value ?? children.Items[SelectedIndex].Content)}\">";
        }
        output.Content.SetHtmlContent(html);
    }
}

/// <summary>ScrollBarVisibility of a ScrollViewer.</summary>
public enum ScrollBarVisibility
{
    Auto,
    Visible,
    Hidden,
    Disabled,
}

/// <summary>
/// <c>&lt;jp-scroll-viewer thin="true"&gt;…&lt;/jp-scroll-viewer&gt;</c>: a scrolling box with the JetPack bars;
/// thin is JetPack.Styles.ScrollViewer.Thin (the accent thumb that hides after 1.5 s).
/// </summary>
[HtmlTargetElement("jp-scroll-viewer")]
public class ScrollViewerTagHelper : JetPackTagHelper
{
    public bool Thin { get; set; }

    public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; } = ScrollBarVisibility.Disabled;

    public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Auto;

    private static string Css(ScrollBarVisibility v) => v switch
    {
        ScrollBarVisibility.Visible => "scroll",
        ScrollBarVisibility.Hidden or ScrollBarVisibility.Disabled => "hidden",
        _ => "auto",
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        AddClasses(output, "jp-scroll-viewer", Thin ? "jp-scroll-viewer--thin" : null);
        output.Attributes.SetAttribute("style", $"overflow-x:{Css(HorizontalScrollBarVisibility)};overflow-y:{Css(VerticalScrollBarVisibility)};" + (output.Attributes["style"]?.Value ?? ""));
        output.Attributes.SetAttribute("tabindex", "-1");
        ApplyCommon(output);
    }
}

/// <summary>
/// <c>&lt;jp-tree-view-item header="Contoso" is-expanded="true"&gt; child items &lt;/jp-tree-view-item&gt;</c>.
/// An item with children gets the [+]/[−] box.
/// </summary>
[HtmlTargetElement("jp-tree-view-item")]
public class TreeViewItemTagHelper : TagHelper
{
    public string Header { get; set; } = "";

    public bool IsExpanded { get; set; }

    public bool IsSelected { get; set; }

    public string? Value { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var children = (await output.GetChildContentAsync()).GetContent().Trim();
        var value = Value is null ? "" : $" data-value=\"{Html.Attr(Value)}\"";
        var item = $"<span class=\"jp-tree-view__item\" role=\"treeitem\" aria-selected=\"{(IsSelected ? "true" : "false")}\"{value}>{Html.Text(Header)}</span>";
        output.TagName = "li";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(children.Length == 0
            ? item
            : $"<details{(IsExpanded ? " open" : "")}><summary>{item}</summary><ul role=\"group\">{children}</ul></details>");
    }
}

/// <summary><c>&lt;jp-tree-view&gt;&lt;jp-tree-view-item …&gt;…&lt;/jp-tree-view&gt;</c>: the dark tree with bold items.</summary>
[HtmlTargetElement("jp-tree-view")]
public class TreeViewTagHelper : JetPackTagHelper
{
    public string? Name { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "ul";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-tree-view");
        output.Attributes.SetAttribute("role", "tree");
        if (Name is not null) output.Attributes.SetAttribute("data-jp-name", Name);
        ApplyCommon(output);
    }
}

/// <summary>A column of a DataGrid or ListView, collected by the parent.</summary>
public abstract class GridColumnTagHelper : TagHelper
{
    public string Header { get; set; } = "";

    /// <summary>Binding: the property of each item to show.</summary>
    public string? Binding { get; set; }

    /// <summary>A pixel width, or * for the rest.</summary>
    public string? Width { get; set; }

    /// <summary>StringFormat: a .NET format such as N1 or d.</summary>
    public string? StringFormat { get; set; }

    /// <summary>Right for numbers.</summary>
    public string? HorizontalAlignment { get; set; }

    /// <summary>Ascending or Descending: the sort arrow in the header.</summary>
    public string? SortDirection { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ChildList<GridColumnTagHelper>.Of(context)?.Items.Add(this);
        output.SuppressOutput();
    }

    internal string Cell(object? item)
    {
        if (item is null || Binding is null) return "";
        var value = item.GetType().GetProperty(Binding)?.GetValue(item);
        return value is IFormattable f && StringFormat is not null
            ? f.ToString(StringFormat, CultureInfo.CurrentCulture)
            : Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
    }

    internal string WidthStyle => Width is null || Width == "*" ? "" : $" style=\"width:{Html.Attr(Width)}px\"";

    internal string Align => string.Equals(HorizontalAlignment, "Right", StringComparison.OrdinalIgnoreCase) ? " class=\"jp-data-grid__number\"" : "";
}

/// <summary><c>&lt;jp-data-grid-text-column header="Hours" binding="Hours" string-format="N1" width="60" horizontal-alignment="Right" /&gt;</c></summary>
[HtmlTargetElement("jp-data-grid-text-column", ParentTag = "jp-data-grid", TagStructure = TagStructure.WithoutEndTag)]
public class DataGridTextColumnTagHelper : GridColumnTagHelper
{
}

/// <summary><c>&lt;jp-grid-view-column header="Employee" display-member-binding="Employee" width="150" /&gt;</c></summary>
[HtmlTargetElement("jp-grid-view-column", ParentTag = "jp-list-view", TagStructure = TagStructure.WithoutEndTag)]
public class GridViewColumnTagHelper : GridColumnTagHelper
{
    public string? DisplayMemberBinding { get => Binding; set => Binding = value; }
}

/// <summary>
/// <c>&lt;jp-data-grid items="@rows" selected-index="1" height="190"&gt;&lt;jp-data-grid-text-column … /&gt;&lt;/jp-data-grid&gt;</c>:
/// a read-only grid with a sticky header, filled from the items like ItemsSource.
/// </summary>
[HtmlTargetElement("jp-data-grid")]
public class DataGridTagHelper : JetPackTagHelper
{
    public IEnumerable Items { get; set; } = Array.Empty<object>();

    public int SelectedIndex { get; set; } = -1;

    /// <summary>Height in pixels; the rows scroll under the header.</summary>
    public double? Height { get; set; }

    /// <summary>Single (default) or None.</summary>
    public string SelectionMode { get; set; } = "Single";

    protected virtual string HostClass => "jp-data-grid-host";

    protected virtual string TableClass => "jp-data-grid";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var columns = ChildList<GridColumnTagHelper>.Start(context);
        await output.GetChildContentAsync();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, HostClass);
        if (Height is { } h)
        {
            output.Attributes.SetAttribute("style", $"height:{Html.Num(h)}px;" + (output.Attributes["style"]?.Value ?? ""));
        }
        ApplyCommon(output);
        var selection = SelectionMode == "None" ? "" : " data-jp-selection=\"single\"";
        var head = string.Concat(columns.Items.Select(c =>
        {
            // Headers stay left-aligned over right-aligned numbers, as in a WPF DataGrid
            var sort = c.SortDirection is null ? "" : $" aria-sort=\"{c.SortDirection.ToLowerInvariant()}\"";
            return $"<th{c.WidthStyle}{sort}>{Html.Text(c.Header)}</th>";
        }));
        var rows = string.Concat(Items.Cast<object?>().Select((item, i) =>
            $"<tr aria-selected=\"{(i == SelectedIndex ? "true" : "false")}\">" +
            string.Concat(columns.Items.Select(c => $"<td{c.Align}>{Html.Text(c.Cell(item))}</td>")) + "</tr>"));
        output.Content.SetHtmlContent($"<table class=\"{TableClass}\"{selection}><thead><tr>{head}</tr></thead><tbody>{rows}</tbody></table>");
    }
}

/// <summary><c>&lt;jp-list-view items="@rows" selected-index="2"&gt;&lt;jp-grid-view-column … /&gt;&lt;/jp-list-view&gt;</c>: ListView with a GridView.</summary>
[HtmlTargetElement("jp-list-view")]
public class ListViewTagHelper : DataGridTagHelper
{
    protected override string HostClass => "jp-list-view";

    protected override string TableClass => "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);
        output.Attributes.SetAttribute("tabindex", "0");
    }
}

/// <summary><c>&lt;jp-calendar selected-date="@DateTime.Today" /&gt;</c>: the month view with the year and decade views behind the header.</summary>
[HtmlTargetElement("jp-calendar", TagStructure = TagStructure.WithoutEndTag)]
public class CalendarTagHelper : JetPackTagHelper
{
    public DateTime? SelectedDate { get; set; }

    public DateTime? DisplayDate { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-calendar");
        if (SelectedDate is { } s) output.Attributes.SetAttribute("data-selected-date", s.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (DisplayDate is { } d) output.Attributes.SetAttribute("data-display-date", d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        ApplyCommon(output);
    }
}
