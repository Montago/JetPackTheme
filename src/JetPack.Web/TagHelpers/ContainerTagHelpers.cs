using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>The TabControl styles: the panel tabs, JetPack.Styles.TabControl.Strip and .Navigation.</summary>
public enum TabControlStyle
{
    Default,
    Strip,
    Navigation,
}

/// <summary>TabControlHelper.SelectionAnimation: how the notch moves to the selected tab.</summary>
public enum TabSelectionAnimation
{
    Slide,
    Drop,
    None,
}

/// <summary>TabControlHelper.ContentTransition.</summary>
public enum TabContentTransition
{
    None,
    Fade,
}

/// <summary>
/// <c>&lt;jp-tab-item header="Core"&gt;page&lt;/jp-tab-item&gt;</c> inside a jp-tab-control; name gives the tab a
/// stable id (and the #name in the address for data-jp-hash).
/// </summary>
[HtmlTargetElement("jp-tab-item", ParentTag = "jp-tab-control")]
public class TabItemTagHelper : TagHelper
{
    public string Header { get; set; } = "";

    public string? Name { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsSelected { get; set; }

    internal string Html { get; private set; } = "";

    internal string? Class { get; private set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        Html = (await output.GetChildContentAsync()).GetContent();
        Class = output.Attributes["class"]?.Value?.ToString();
        ChildList<TabItemTagHelper>.Of(context)?.Items.Add(this);
        output.SuppressOutput();
    }
}

/// <summary><c>&lt;jp-tab-control-tag&gt;</c>: the TabControl's Tag, shown at the right of the strip or bar.</summary>
[HtmlTargetElement("jp-tab-control-tag", ParentTag = "jp-tab-control")]
public class TabControlTagTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (context.Items.TryGetValue(typeof(TabControlTagHelper), out var parent))
        {
            ((TabControlTagHelper)parent).Tag = (await output.GetChildContentAsync()).GetContent();
        }
        output.SuppressOutput();
    }
}

/// <summary>
/// <c>&lt;jp-tab-control tab-control-style="Strip" selected-index="0"&gt;&lt;jp-tab-item header="Core"&gt;…&lt;/jp-tab-item&gt;&lt;/jp-tab-control&gt;</c>.
/// jetpack.js switches the pages and moves the notch.
/// </summary>
[HtmlTargetElement("jp-tab-control")]
public class TabControlTagHelper : JetPackTagHelper
{
    public TabControlStyle TabControlStyle { get; set; }

    public int SelectedIndex { get; set; }

    public TabSelectionAnimation SelectionAnimation { get; set; }

    public TabContentTransition ContentTransition { get; set; }

    public string? Id { get; set; }

    /// <summary>Keeps the selected tab in the address (#name), so a link opens that tab.</summary>
    public bool UseHash { get; set; }

    /// <summary>The content of jp-tab-control-tag (the children get a copy of context.Items, so they write here).</summary>
    [HtmlAttributeNotBound]
    public string Tag { get; set; } = "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var tabs = ChildList<TabItemTagHelper>.Start(context);
        context.Items[typeof(TabControlTagHelper)] = this;
        await output.GetChildContentAsync();
        var id = Id ?? Html.NewId("jp-tabs");
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", id);
        AddClasses(output, "jp-tab-control", TabControlStyle == TabControlStyle.Default ? null : "jp-tab-control--" + TabControlStyle.ToString().ToLowerInvariant());
        output.Attributes.SetAttribute("data-selection-animation", SelectionAnimation.ToString().ToLowerInvariant());
        output.Attributes.SetAttribute("data-content-transition", ContentTransition.ToString().ToLowerInvariant());
        if (UseHash) output.Attributes.SetAttribute("data-jp-hash", "true");
        ApplyCommon(output);

        var selected = tabs.Items.FindIndex(t => t.IsSelected);
        if (selected < 0) selected = SelectedIndex;
        var strip = string.Concat(tabs.Items.Select((t, i) =>
        {
            var key = t.Name ?? i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var on = i == selected;
            var name = t.Name is null ? "" : $" data-name=\"{Html.Attr(t.Name)}\"";
            return $"<button type=\"button\" class=\"jp-tab-item\" role=\"tab\" id=\"{id}-tab-{Html.Attr(key)}\" aria-controls=\"{id}-panel-{Html.Attr(key)}\" " +
                   $"aria-selected=\"{(on ? "true" : "false")}\" tabindex=\"{(on ? "0" : "-1")}\"{name}{(t.IsEnabled ? "" : " disabled")}>{Html.Text(t.Header)}</button>";
        }));
        var panels = string.Concat(tabs.Items.Select((t, i) =>
        {
            var key = t.Name ?? i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var cls = t.Class is null ? "" : " " + Html.Attr(t.Class);
            return $"<div class=\"jp-tab-control__content{cls}\" role=\"tabpanel\" id=\"{id}-panel-{Html.Attr(key)}\" aria-labelledby=\"{id}-tab-{Html.Attr(key)}\"{(i == selected ? "" : " hidden")}>{t.Html}</div>";
        }));
        var tag = Tag;
        output.Content.SetHtmlContent(
            "<div class=\"jp-tab-control__header\">" +
            $"<div class=\"jp-tab-control__strip\" role=\"tablist\">{strip}<span class=\"jp-tab-control__notch\" aria-hidden=\"true\"></span></div>" +
            (tag.Length > 0 ? $"<div class=\"jp-tab-control__tag\">{tag}</div>" : "") +
            "</div>" + panels);
    }
}

/// <summary><c>&lt;jp-expander header="Billing address" is-expanded="true"&gt;…&lt;/jp-expander&gt;</c></summary>
[HtmlTargetElement("jp-expander")]
public class ExpanderTagHelper : JetPackTagHelper
{
    public string Header { get; set; } = "";

    public bool IsExpanded { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "details";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-expander");
        if (IsExpanded) output.Attributes.SetAttribute("open", "open");
        if (context.Items.TryGetValue(typeof(AccordionTagHelper), out var group)) output.Attributes.SetAttribute("name", (string)group);
        ApplyCommon(output);
        output.Content.SetHtmlContent($"<summary>{Html.Text(Header)}</summary><div class=\"jp-expander__content\">{content}</div>");
    }
}

/// <summary><c>&lt;jp-accordion&gt;&lt;jp-expander …&gt;…&lt;/jp-accordion&gt;</c>: one frame, one Expander open at a time.</summary>
[HtmlTargetElement("jp-accordion")]
public class AccordionTagHelper : JetPackTagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        context.Items[typeof(AccordionTagHelper)] = Html.NewId("jp-accordion");
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-accordion");
        ApplyCommon(output);
        output.Content.SetHtmlContent(content);
    }
}

/// <summary>
/// <c>&lt;jp-busy-indicator is-busy="true" busy-content="Loading orders…"&gt;content&lt;/jp-busy-indicator&gt;</c>:
/// blurs and dims the content and shows a card with a progress bar. Set data-busy from script.
/// </summary>
[HtmlTargetElement("jp-busy-indicator")]
public class BusyIndicatorTagHelper : JetPackTagHelper
{
    public bool IsBusy { get; set; }

    public string BusyContent { get; set; } = "Please wait…";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-busy-indicator");
        output.Attributes.SetAttribute("data-busy", IsBusy ? "true" : "false");
        output.Attributes.SetAttribute("aria-busy", IsBusy ? "true" : "false");
        ApplyCommon(output);
        output.Content.SetHtmlContent(
            $"<div class=\"jp-busy-indicator__content\">{content}</div>" +
            $"<div class=\"jp-busy-indicator__card\" role=\"status\">{Html.Text(BusyContent)}<progress class=\"jp-progress-bar\"></progress></div>");
    }
}

/// <summary><c>&lt;jp-badged badge="12"&gt;&lt;jp-button&gt;Inbox&lt;/jp-button&gt;&lt;/jp-badged&gt;</c>: the orange count badge.</summary>
[HtmlTargetElement("jp-badged")]
public class BadgedTagHelper : JetPackTagHelper
{
    public string? Badge { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-badged");
        ApplyCommon(output);
        output.Content.SetHtmlContent(content + (Badge is null ? "" : $"<span class=\"jp-badged__badge\">{Html.Text(Badge)}</span>"));
    }
}

/// <summary>
/// <c>&lt;jp-grid-splitter /&gt;</c> between two panes of a flex row (or of a flex column with
/// <c>resize-direction="Rows"</c>): drag it to resize the pane before it.
/// </summary>
[HtmlTargetElement("jp-grid-splitter", TagStructure = TagStructure.WithoutEndTag)]
public class GridSplitterTagHelper : JetPackTagHelper
{
    /// <summary><c>Columns</c> (default) or <c>Rows</c>.</summary>
    public string ResizeDirection { get; set; } = "Columns";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var rows = ResizeDirection.Equals("Rows", StringComparison.OrdinalIgnoreCase);
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-grid-splitter", rows ? "jp-grid-splitter--rows" : null);
        output.Attributes.SetAttribute("role", "separator");
        output.Attributes.SetAttribute("aria-orientation", rows ? "horizontal" : "vertical");
        ApplyCommon(output);
    }
}
