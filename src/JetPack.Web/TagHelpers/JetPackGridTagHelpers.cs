using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>
/// A column of a JetPackGrid, collected by the grid:
/// <c>&lt;jp-jetpack-grid-column header="Price (EUR)" binding="Price" string-format="N0" width="96" text-alignment="Right" /&gt;</c>.
/// </summary>
[HtmlTargetElement("jp-jetpack-grid-column", ParentTag = "jp-jetpack-grid", TagStructure = TagStructure.WithoutEndTag)]
public class JetPackGridColumnTagHelper : TagHelper
{
    public string Header { get; set; } = "";

    /// <summary>Binding: the property of each item to show; a dotted path reads nested properties.</summary>
    public string? Binding { get; set; }

    /// <summary>A pixel width, or * for the rest of the row.</summary>
    public string? Width { get; set; }

    /// <summary>StringFormat: a .NET format such as N0 or d, or a composite one such as "{0} hp".</summary>
    public string? StringFormat { get; set; }

    /// <summary>Left, Right (for numbers) or Center.</summary>
    public string? TextAlignment { get; set; }

    /// <summary>Ascending or Descending: the order the rows start in.</summary>
    public string? SortDirection { get; set; }

    /// <summary>False keeps a click on the header from sorting the rows.</summary>
    public bool CanUserSort { get; set; } = true;

    /// <summary>False keeps the column at its width.</summary>
    public bool CanUserResize { get; set; } = true;

    /// <summary>Whether the grid's search looks in this column and marks its hits.</summary>
    public virtual bool IsSearchable { get; set; } = true;

    /// <summary>Whether the header shows the filter button.</summary>
    public bool IsFilterable { get; set; } = true;

    /// <summary>
    /// Values the column must equal (ignoring case), separated by spaces or commas; a row passes if it
    /// matches any of them. Quotes keep a value with spaces together; * matches any text ("vol*").
    /// Ranges compare numbers and dates by value: "&gt;50000", "&lt;=2019", "2020-01-01..2020-06-30".
    /// </summary>
    public string? FilterText { get; set; }

    /// <summary>Days a column of dates must be one of (their time does not count); null stands for rows without a date.</summary>
    public IEnumerable<DateTime?>? FilterDates { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ChildList<JetPackGridColumnTagHelper>.Of(context)?.Items.Add(this);
        output.SuppressOutput();
    }

    internal virtual bool IsCheckBox => false;

    internal virtual string Attributes(string type)
    {
        var s = new StringBuilder();
        void Add(string name, string? value)
        {
            if (value is not null) s.Append($" {name}=\"{Html.Attr(value)}\"");
        }
        Add("data-binding", Binding);
        Add("data-type", type);
        Add("data-width", Width);
        Add("data-string-format", StringFormat);
        Add("data-align", TextAlignment?.ToLowerInvariant());
        Add("aria-sort", SortDirection?.ToLowerInvariant());
        Add("data-jp-sort", CanUserSort ? null : "false");
        Add("data-jp-resize", CanUserResize ? null : "false");
        Add("data-searchable", IsSearchable ? null : "false");
        Add("data-filterable", IsFilterable ? null : "false");
        Add("data-filter-text", string.IsNullOrEmpty(FilterText) ? null : FilterText);
        if (FilterDates is not null)
        {
            Add("data-filter-dates", JsonSerializer.Serialize(FilterDates.Select(d => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
        }
        return s.ToString();
    }

    internal object? Read(object? item) => GridCells.Read(item, Binding);

    internal string Format(object? value, CultureInfo culture) => GridCells.Text(value, GridCells.FormatOf(StringFormat), null, culture);
}

/// <summary>
/// The JetPack check box column: <c>&lt;jp-jetpack-grid-check-box-column header="Sold" binding="IsSold" width="56" /&gt;</c>.
/// The header filter and the search read the values as TrueText and FalseText.
/// </summary>
[HtmlTargetElement("jp-jetpack-grid-check-box-column", ParentTag = "jp-jetpack-grid", TagStructure = TagStructure.WithoutEndTag)]
public class JetPackGridCheckBoxColumnTagHelper : JetPackGridColumnTagHelper
{
    /// <summary>Whether the search looks in this column (for TrueText and FalseText); off by default.</summary>
    public override bool IsSearchable { get; set; }

    public string TrueText { get; set; } = "Yes";

    public string FalseText { get; set; } = "No";

    internal override bool IsCheckBox => true;

    internal override string Attributes(string type) =>
        base.Attributes("bool") + $" data-check-box data-true-text=\"{Html.Attr(TrueText)}\" data-false-text=\"{Html.Attr(FalseText)}\"";
}

/// <summary>SelectionMode of a JetPackGrid.</summary>
public enum GridSelectionMode
{
    Single,
    Extended,
    None,
}

/// <summary>
/// <c>&lt;jp-jetpack-grid items="@cars" page-size="100" group-member-path="Make" search-box="Search" height="400"&gt;
/// &lt;jp-jetpack-grid-column … /&gt;&lt;/jp-jetpack-grid&gt;</c>: the data grid for lists of up to a few thousand rows.
/// The items go into the page as JSON (each cell as it is shown, plus its value for numbers, dates and check
/// boxes) and jetpack.js draws the rows of the page shown: a word search across the columns with the hits
/// marked, a value filter in every column header, three-state sorting, grouping and paging, all in the browser.
/// </summary>
[HtmlTargetElement("jp-jetpack-grid")]
public class JetPackGridTagHelper : JetPackTagHelper
{
    public IEnumerable Items { get; set; } = Array.Empty<object>();

    /// <summary>
    /// Server mode: the address the grid posts a <see cref="JetPackGridRequest"/> to for each page (and for the
    /// values of a header filter); it answers with a <see cref="JetPackGridResponse"/>, for instance from
    /// <see cref="JetPackGridServer.Query"/>. The items, if any, are then not written into the page.
    /// </summary>
    public string? ItemsSourceUrl { get; set; }

    /// <summary>Words that must all be found in a row (in any searchable column); quotes keep a phrase together.</summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// The id of a text box whose text is the search, as <c>SearchText="{Binding Text, ElementName=Search}"</c> in WPF;
    /// the grid follows it as you type.
    /// </summary>
    public string? SearchBox { get; set; }

    /// <summary>Milliseconds after the last change to the search or a filter before the rows are filtered; 0 filters at once.</summary>
    public int SearchDelay { get; set; } = 300;

    /// <summary>A header filter lists the column's values to tick when it has at most this many; with more, they are typed.</summary>
    public int FilterListLimit { get; set; } = 20;

    /// <summary>The binding of the column the rows are grouped by; empty for no groups.</summary>
    public string? GroupMemberPath { get; set; }

    /// <summary>Rows per page; 0 shows every row.</summary>
    public int PageSize { get; set; }

    /// <summary>The page shown, from 0.</summary>
    public int PageIndex { get; set; }

    public int SelectedIndex { get; set; } = -1;

    public GridSelectionMode SelectionMode { get; set; } = GridSelectionMode.Single;

    /// <summary>Columns on the left that stay in view when the others scroll sideways.</summary>
    public int FrozenColumnCount { get; set; }

    /// <summary>Height in pixels; without it the grid fills the height its parent gives it.</summary>
    public double? Height { get; set; }

    /// <summary>False: the headers do not sort the rows.</summary>
    public bool CanUserSortColumns { get; set; } = true;

    /// <summary>False: the columns cannot be resized.</summary>
    public bool CanUserResizeColumns { get; set; } = true;

    /// <summary>Text colour of the search hits (a CSS colour); the accent by default, "none" keeps the cell's colour.</summary>
    public string? HighlightForeground { get; set; }

    /// <summary>Background of the cells with search hits (a CSS colour); grey by default, "none" for none.</summary>
    public string? HighlightCellBackground { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var columns = ChildList<JetPackGridColumnTagHelper>.Start(context);
        await output.GetChildContentAsync();
        var culture = CultureInfo.CurrentCulture;
        var items = ItemsSourceUrl is null ? Items.Cast<object?>().ToList() : [];

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-jetpack-grid");
        var style = (Height is { } h ? $"height:{Html.Num(h)}px;" : "") +
            (HighlightForeground is null ? "" : $"--jp-jetpack-grid-highlight-foreground:{(HighlightForeground == "none" ? "inherit" : HighlightForeground)};") +
            (HighlightCellBackground is null ? "" : $"--jp-jetpack-grid-highlight-cell-background:{(HighlightCellBackground == "none" ? "transparent" : HighlightCellBackground)};");
        if (style.Length > 0)
        {
            output.Attributes.SetAttribute("style", style + (output.Attributes["style"]?.Value ?? ""));
        }
        void Data(string name, object? value, object? skip = null)
        {
            if (value is not null && !Equals(value, skip))
            {
                output.Attributes.SetAttribute("data-jp-" + name, Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }
        Data("culture", culture.Name);
        Data("items-source-url", ItemsSourceUrl);
        Data("search-text", string.IsNullOrEmpty(SearchText) ? null : SearchText);
        Data("search-box", SearchBox);
        Data("search-delay", SearchDelay, 300);
        Data("filter-list-limit", FilterListLimit, 20);
        Data("group-member-path", string.IsNullOrEmpty(GroupMemberPath) ? null : GroupMemberPath);
        Data("page-size", PageSize, 0);
        Data("page-index", PageIndex, 0);
        Data("selected-index", SelectedIndex, -1);
        Data("selection", SelectionMode.ToString().ToLowerInvariant(), "single");
        Data("frozen-column-count", FrozenColumnCount, 0);
        Data("sort", CanUserSortColumns ? null : "false");
        Data("resize", CanUserResizeColumns ? null : "false");
        if (!IsEnabled) output.Attributes.SetAttribute("aria-disabled", "true");
        ApplyCommon(output);

        var types = columns.Items.Select(c => GridCells.TypeOf(c.IsCheckBox, items.Select(c.Read))).ToArray();
        var head = string.Concat(columns.Items.Select((c, i) => $"<th{c.Attributes(types[i])}>{Html.Text(c.Header)}</th>"));
        output.Content.SetHtmlContent(
            "<div class=\"jp-jetpack-grid__viewport\" tabindex=\"0\">" +
            $"<table class=\"jp-jetpack-grid__table\" role=\"grid\"><thead><tr>{head}</tr></thead><tbody></tbody></table></div>" +
            $"<script type=\"application/json\" class=\"jp-jetpack-grid__items\">{Json(columns.Items, types, items, culture)}</script>");
    }

    /// <summary>
    /// The rows as an array of arrays, one cell per column (GridCells.Cell). The default encoder escapes &lt; and
    /// &amp;, so the JSON is safe in a script element.
    /// </summary>
    private static string Json(List<JetPackGridColumnTagHelper> columns, string[] types, List<object?> items, CultureInfo culture) =>
        JsonSerializer.Serialize(items.Select(item => columns.Select((c, i) => GridCells.Cell(types[i], c.Read(item), v => c.Format(v, culture))).ToArray()));
}
