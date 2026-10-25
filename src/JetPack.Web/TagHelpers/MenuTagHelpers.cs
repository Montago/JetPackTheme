using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>Marks that the tag helpers below sit in a menu (a jp-separator becomes a menu separator).</summary>
internal sealed class MenuScope
{
}

/// <summary>
/// <c>&lt;jp-menu-item header="Cut" input-gesture-text="Ctrl+X" /&gt;</c>; is-checkable / is-checked for a check
/// mark; child jp-menu-items make it a submenu. In a jp-menu bar it is a top-level header.
/// </summary>
[HtmlTargetElement("jp-menu-item")]
public class MenuItemTagHelper : JetPackTagHelper
{
    public string Header { get; set; } = "";

    public string? InputGestureText { get; set; }

    public bool IsCheckable { get; set; }

    public bool IsChecked { get; set; }

    /// <summary>A JetPackIcon in the icon column.</summary>
    public JetPackIconKind Icon { get; set; }

    public string? Href { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var topLevel = context.Items.ContainsKey(typeof(MenuTagHelper)) && !context.Items.ContainsKey(typeof(MenuScope));
        context.Items[typeof(MenuScope)] = new MenuScope();
        var children = (await output.GetChildContentAsync()).GetContent().Trim();
        var hasMenu = children.Length > 0;
        var menuId = Html.NewId("jp-submenu");
        var text = Html.Text(Header.Replace("_", ""));

        output.TagName = Href is null ? "button" : "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        if (Href is null) output.Attributes.SetAttribute("type", "button");
        else output.Attributes.SetAttribute("href", Href);
        AddClasses(output, "jp-menu-item");
        if (!IsEnabled) output.Attributes.SetAttribute("disabled", "disabled");
        ApplyCommon(output);

        if (topLevel)
        {
            output.Attributes.SetAttribute("role", "menuitem");
            if (hasMenu)
            {
                output.Attributes.SetAttribute("popovertarget", menuId);
                output.Attributes.SetAttribute("aria-haspopup", "menu");
            }
            output.Content.SetHtmlContent(text);
            if (hasMenu)
            {
                output.PostElement.SetHtmlContent($"<div class=\"jp-context-menu\" popover id=\"{menuId}\" role=\"menu\">{children}</div>");
            }
            return;
        }

        output.Attributes.SetAttribute("role", IsCheckable ? "menuitemcheckbox" : "menuitem");
        if (IsCheckable) output.Attributes.SetAttribute("aria-checked", IsChecked ? "true" : "false");
        var icon = Icon == JetPackIconKind.None ? "" : $"<svg class=\"jp-icon\" aria-hidden=\"true\"><use href=\"/_content/JetPack.Web/img/icons.svg#{JetPackIconIds.Get(Icon)}\"></use></svg>";
        var gesture = InputGestureText is null ? "" : $"<span class=\"jp-menu-item__gesture\">{Html.Text(InputGestureText)}</span>";
        output.Content.SetHtmlContent(icon + $"<span class=\"jp-menu-item__header\">{text}</span>" + gesture);
        if (hasMenu)
        {
            output.Attributes.SetAttribute("aria-haspopup", "menu");
            output.Attributes.SetAttribute("data-jp-submenu", menuId);
            // The submenu sits inside its parent menu, so opening it keeps the parent open
            output.PostElement.SetHtmlContent($"<div class=\"jp-context-menu\" popover id=\"{menuId}\" role=\"menu\">{children}</div>");
        }
    }
}

/// <summary><c>&lt;jp-menu&gt;&lt;jp-menu-item header="_File"&gt;…&lt;/jp-menu-item&gt;&lt;/jp-menu&gt;</c>: the menu bar.</summary>
[HtmlTargetElement("jp-menu")]
public class MenuTagHelper : JetPackTagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        context.Items[typeof(MenuTagHelper)] = this;
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "nav";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-menu");
        output.Attributes.SetAttribute("role", "menubar");
        ApplyCommon(output);
        output.Content.SetHtmlContent(content);
    }
}

/// <summary>
/// <c>&lt;jp-context-menu id="gridMenu"&gt;&lt;jp-menu-item header="Copy" /&gt;&lt;/jp-context-menu&gt;</c>; put
/// data-jp-context-menu="gridMenu" on the element that opens it with a right-click.
/// </summary>
[HtmlTargetElement("jp-context-menu")]
public class ContextMenuTagHelper : TagHelper
{
    public string Id { get; set; } = "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        context.Items[typeof(MenuScope)] = new MenuScope();
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-context-menu", Html.Encoder);
        output.Attributes.SetAttribute("id", Id);
        output.Attributes.SetAttribute("popover", "");
        output.Attributes.SetAttribute("role", "menu");
        output.Content.SetHtmlContent(content);
    }
}

/// <summary><c>&lt;jp-tool-bar-tray&gt;&lt;jp-tool-bar&gt;…&lt;/jp-tool-bar&gt;&lt;/jp-tool-bar-tray&gt;</c></summary>
[HtmlTargetElement("jp-tool-bar-tray")]
public class ToolBarTrayTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.AddClass("jp-tool-bar-tray", Html.Encoder);
    }
}

/// <summary>
/// <c>&lt;jp-tool-bar&gt;</c>: the light bar with the grip; a jp-separator in it is the vertical one, and
/// buttons are flat until the mouse is over them.
/// </summary>
[HtmlTargetElement("jp-tool-bar")]
public class ToolBarTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        context.Items[typeof(ToolBarTagHelper)] = this;
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-tool-bar", Html.Encoder);
        output.Attributes.SetAttribute("role", "toolbar");
        output.Content.SetHtmlContent("<span class=\"jp-tool-bar__thumb\" aria-hidden=\"true\"></span>" +
            content.Replace("<hr class=\"jp-separator\">", "<span class=\"jp-tool-bar__separator\" role=\"separator\"></span>"));
    }
}
