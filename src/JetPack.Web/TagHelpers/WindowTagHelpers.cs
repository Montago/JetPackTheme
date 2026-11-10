using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>Flyout.Position: the edge a Flyout slides in from.</summary>
public enum FlyoutPosition
{
    Right,
    Left,
    Top,
    Bottom,
}

/// <summary>
/// <c>&lt;jp-flyout id="settings" header="Settings" position="Right" width="300"&gt;…&lt;/jp-flyout&gt;</c>: a panel
/// that slides in over the nearest .jp-flyout-host (the window). Open it with data-jp-open="settings".
/// </summary>
[HtmlTargetElement("jp-flyout")]
public class FlyoutTagHelper : JetPackTagHelper
{
    public string Id { get; set; } = "";

    public string? Header { get; set; }

    public FlyoutPosition Position { get; set; }

    public double? Width { get; set; }

    public bool IsOpen { get; set; }

    public bool IsClosable { get; set; } = true;

    public bool IsAutoCloseEnabled { get; set; }

    /// <summary>Milliseconds before an auto-closing flyout closes; the timer stops while the mouse is over it.</summary>
    public long AutoCloseInterval { get; set; } = 5000;

    /// <summary>The inner spacing, as CSS (WPF's Padding="14,12" is 12px 14px).</summary>
    public string? Padding { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "aside";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-flyout", IsOpen ? "is-open" : null);
        output.Attributes.SetAttribute("id", Id);
        output.Attributes.SetAttribute("data-position", Position.ToString().ToLowerInvariant());
        if (Header is not null) output.Attributes.SetAttribute("aria-label", Header);
        if (IsAutoCloseEnabled) output.Attributes.SetAttribute("data-jp-auto-close", AutoCloseInterval.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var style = (Width is { } w && Position is FlyoutPosition.Left or FlyoutPosition.Right ? $"width:{Html.Num(w)}px;" : "") +
                    (Padding is null ? "" : $"padding:{Html.Attr(Padding)};");
        if (style.Length > 0) output.Attributes.SetAttribute("style", style + (output.Attributes["style"]?.Value ?? ""));
        ApplyCommon(output);
        var close = IsClosable ? "<button type=\"button\" class=\"jp-flyout__close\" data-jp-close aria-label=\"Close\" data-jp-tool-tip=\"Close\"></button>" : "";
        var header = Header is null && !IsClosable ? "" : $"<header class=\"jp-flyout__header\">{close}<span>{Html.Text(Header)}</span></header>";
        output.Content.SetHtmlContent(header + content);
    }
}

/// <summary><c>&lt;jp-status-bar&gt;&lt;jp-status-bar-item&gt;Ready&lt;/jp-status-bar-item&gt;…&lt;/jp-status-bar&gt;</c></summary>
[HtmlTargetElement("jp-status-bar")]
public class StatusBarTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "footer";
        output.AddClass("jp-status-bar", Html.Encoder);
    }
}

/// <summary><c>&lt;jp-status-bar-item horizontal-alignment="Right"&gt;</c>: an item; Right ones move to the end.</summary>
[HtmlTargetElement("jp-status-bar-item", ParentTag = "jp-status-bar")]
public class StatusBarItemTagHelper : TagHelper
{
    public string? Content { get; set; }

    public string? HorizontalAlignment { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var inner = Content is null ? (await output.GetChildContentAsync()).GetContent() : Html.Text(Content);
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        if (string.Equals(HorizontalAlignment, "Right", StringComparison.OrdinalIgnoreCase))
        {
            output.AddClass("jp-status-bar__item--right", Html.Encoder);
        }
        output.Content.SetHtmlContent(inner);
    }
}

/// <summary>
/// <c>&lt;jp-hamburger-menu-item content="Overview" icon="Home" href="…" /&gt;</c>. In a menu with its own
/// content the element's content is the page the item shows. Items nested in an item make it a group
/// that opens and closes (<c>is-expanded</c>); sub-items show no icon, and there are two levels.
/// </summary>
[HtmlTargetElement("jp-hamburger-menu-item", ParentTag = "jp-hamburger-menu")]
[HtmlTargetElement("jp-hamburger-menu-item", ParentTag = "jp-hamburger-menu-item")]
public class HamburgerMenuItemTagHelper : TagHelper
{
    public string Content { get; set; } = "";

    public JetPackIconKind Icon { get; set; }

    public string? Href { get; set; }

    public bool IsSelected { get; set; }

    /// <summary>Whether a group shows its sub-items; a group with the selected page opens by itself.</summary>
    public bool IsExpanded { get; set; }

    internal string Html { get; private set; } = "";

    internal List<HamburgerMenuItemTagHelper> Items { get; private set; } = [];

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var parent = ChildList<HamburgerMenuItemTagHelper>.Of(context);
        var children = ChildList<HamburgerMenuItemTagHelper>.Start(context);
        Html = (await output.GetChildContentAsync()).GetContent();
        Items = children.Items;
        parent?.Items.Add(this);
        output.SuppressOutput();
    }
}

/// <summary>
/// <c>&lt;jp-hamburger-menu selected-index="0"&gt;&lt;jp-hamburger-menu-item …&gt;page&lt;/jp-hamburger-menu-item&gt;&lt;/jp-hamburger-menu&gt;</c>:
/// the dark navigation pane. Items with href are links (the window's navigation); items with content
/// switch the panel at the right, as the control in a window. As in WPF, <c>selected-index</c> counts
/// groups and their sub-items in order, and a group (never selected itself) passes it to its first sub-item.
/// </summary>
[HtmlTargetElement("jp-hamburger-menu")]
public class HamburgerMenuTagHelper : JetPackTagHelper
{
    public int SelectedIndex { get; set; } = -1;

    public bool IsPaneOpen { get; set; } = true;

    public double? PaneWidth { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var items = ChildList<HamburgerMenuItemTagHelper>.Start(context);
        await output.GetChildContentAsync();

        // Groups followed by their sub-items, as the WPF menu keeps them in Items
        var flat = items.Items.SelectMany(i => i.Items.Count > 0 ? [i, .. i.Items] : new[] { i }).ToList();
        var pages = flat.Where(i => i.Items.Count == 0 || !items.Items.Contains(i)).ToList();
        var hosted = pages.Any(i => i.Href is null && i.Html.Trim().Length > 0);
        var id = Html.NewId("jp-ham");
        var selectedItem = flat.FirstOrDefault(i => i.IsSelected)
            ?? (SelectedIndex >= 0 && SelectedIndex < flat.Count ? flat[SelectedIndex] : null);
        if (selectedItem is { Items.Count: > 0 } && items.Items.Contains(selectedItem))
        {
            selectedItem = selectedItem.Items[0];
        }

        string Icon(JetPackIconKind k) => Html.Icon(ViewContext, k);
        string Item(HamburgerMenuItemTagHelper item, bool sub)
        {
            var current = item == selectedItem ? " aria-current=\"page\"" : "";
            var css = sub ? "jp-hamburger-menu__item jp-hamburger-menu__item--sub" : "jp-hamburger-menu__item";
            var body = (sub ? "" : Icon(item.Icon)) + $"<span class=\"jp-hamburger-menu__label\">{Html.Text(item.Content)}</span>";
            return item.Href is not null
                ? $"<a class=\"{css}\" href=\"{Html.Attr(item.Href)}\" title=\"{Html.Attr(item.Content)}\"{current}>{body}</a>"
                : $"<button type=\"button\" class=\"{css}\" aria-controls=\"{id}-{pages.IndexOf(item)}\" title=\"{Html.Attr(item.Content)}\"{current}>{body}</button>";
        }
        string Group(HamburgerMenuItemTagHelper group)
        {
            var expanded = group.IsExpanded || (selectedItem is not null && group.Items.Contains(selectedItem));
            return "<div class=\"jp-hamburger-menu__group\">" +
                $"<button type=\"button\" class=\"jp-hamburger-menu__item jp-hamburger-menu__group-header\" data-jp-hamburger-group aria-expanded=\"{(expanded ? "true" : "false")}\" title=\"{Html.Attr(group.Content)}\">" +
                Icon(group.Icon) + $"<span class=\"jp-hamburger-menu__label\">{Html.Text(group.Content)}</span><span class=\"jp-hamburger-menu__arrow\"></span></button>" +
                $"<div class=\"jp-hamburger-menu__sub-items\" role=\"group\"{(expanded ? "" : " hidden")}>" +
                string.Concat(group.Items.Select(sub => Item(sub, true))) + "</div></div>";
        }

        var nav = $"<nav class=\"jp-hamburger-menu{(IsPaneOpen ? "" : " is-collapsed")}\"{(PaneWidth is { } w ? $" style=\"--pane-width:{Html.Num(w)}px\"" : "")} aria-label=\"Pages\">" +
            "<button type=\"button\" class=\"jp-hamburger-menu__toggle\" data-jp-toggle-pane aria-expanded=\"true\" aria-label=\"Show page names\">" + Icon(JetPackIconKind.Menu) + "</button>" +
            string.Concat(items.Items.Select(item => item.Items.Count > 0 ? Group(item) : Item(item, false))) + "</nav>";

        output.TagMode = TagMode.StartTagAndEndTag;
        if (!hosted)
        {
            output.TagName = null;
            output.Content.SetHtmlContent(nav);
            return;
        }
        output.TagName = "div";
        AddClasses(output, "jp-hamburger-menu-host");
        ApplyCommon(output);
        var panels = string.Concat(pages.Select((item, i) => $"<div id=\"{id}-{i}\" role=\"tabpanel\"{(item == selectedItem ? "" : " hidden")}>{item.Html}</div>"));
        output.Content.SetHtmlContent(nav + $"<div class=\"jp-hamburger-menu__content\">{panels}</div>");
    }
}
