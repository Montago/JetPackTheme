using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>The JetPack.Styles.Button.* styles.</summary>
public enum ButtonStyle
{
    Default,
    Accent,
    Green,
    Orange,
    Blue,
    Square,
    Flat,
    Link,
}

/// <summary>
/// <c>&lt;jp-button button-style="Accent"&gt;Save&lt;/jp-button&gt;</c>: a button; with href a link that looks like
/// one. Content or the element's own content is the text.
/// </summary>
[HtmlTargetElement("jp-button")]
public class ButtonTagHelper : JetPackTagHelper
{
    public ButtonStyle ButtonStyle { get; set; }

    public string? Content { get; set; }

    public string? Href { get; set; }

    /// <summary>button, submit or reset.</summary>
    public string Type { get; set; } = "button";

    protected virtual void Extra(TagHelperOutput output)
    {
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var inner = (await output.GetChildContentAsync()).GetContent();
        output.TagMode = TagMode.StartTagAndEndTag;
        if (Href is not null && IsEnabled)
        {
            output.TagName = "a";
            output.Attributes.SetAttribute("href", Href);
        }
        else
        {
            output.TagName = "button";
            output.Attributes.SetAttribute("type", Type);
            if (!IsEnabled)
            {
                output.Attributes.SetAttribute("disabled", "disabled");
            }
        }
        AddClasses(output, "jp-button", ButtonStyle == ButtonStyle.Default ? null : "jp-button--" + ButtonStyle.ToString().ToLowerInvariant());
        ApplyCommon(output);
        Extra(output);
        output.Content.SetHtmlContent(Content is null ? inner : Html.Text(Content));
    }
}

/// <summary><c>&lt;jp-toggle-button is-checked="true"&gt;Bold&lt;/jp-toggle-button&gt;</c>: stays pressed (aria-pressed).</summary>
[HtmlTargetElement("jp-toggle-button")]
public class ToggleButtonTagHelper : ButtonTagHelper
{
    public bool IsChecked { get; set; }

    protected override void Extra(TagHelperOutput output)
    {
        output.Attributes.SetAttribute("aria-pressed", IsChecked ? "true" : "false");
        output.Attributes.SetAttribute("data-jp-toggle", "");
    }
}

/// <summary><c>&lt;jp-repeat-button&gt;</c>: clicks when pressed and again every 33 ms while held, after 0.4 s.</summary>
[HtmlTargetElement("jp-repeat-button")]
public class RepeatButtonTagHelper : ButtonTagHelper
{
    protected override void Extra(TagHelperOutput output) => output.Attributes.SetAttribute("data-jp-repeat", "");
}

/// <summary><c>&lt;jp-hyperlink href="…"&gt;Open timesheet&lt;/jp-hyperlink&gt;</c>: the accent link of the theme.</summary>
[HtmlTargetElement("jp-hyperlink")]
public class HyperlinkTagHelper : JetPackTagHelper
{
    public string? Href { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-hyperlink");
        if (IsEnabled)
        {
            output.Attributes.SetAttribute("href", Href ?? "#");
        }
        else
        {
            output.Attributes.SetAttribute("aria-disabled", "true");
        }
        ApplyCommon(output);
    }
}

/// <summary>A MenuItem of a SplitButton or DropDownButton menu, collected by the parent.</summary>
internal sealed record MenuPart(string Html);

/// <summary>
/// <c>&lt;jp-split-button content="Save" button-style="Accent"&gt;&lt;jp-menu-item header="Save as draft" /&gt;…&lt;/jp-split-button&gt;</c>:
/// the main action plus a menu behind the arrow. The children are its menu items.
/// </summary>
[HtmlTargetElement("jp-split-button")]
public class SplitButtonTagHelper : JetPackTagHelper
{
    public string? Content { get; set; }

    /// <summary>Default or Accent (JetPack.Styles.SplitButton.Accent).</summary>
    public ButtonStyle ButtonStyle { get; set; }

    public string? Id { get; set; }

    protected virtual bool Split => true;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var menu = (await output.GetChildContentAsync()).GetContent();
        var menuId = (Id ?? Html.NewId("jp-menu")) + "-menu";
        var style = ButtonStyle == ButtonStyle.Default ? "" : " jp-button--" + ButtonStyle.ToString().ToLowerInvariant();
        var disabled = IsEnabled ? "" : " disabled";
        var popup = $"<div class=\"jp-context-menu\" popover id=\"{menuId}\" role=\"menu\">{menu}</div>";
        output.TagMode = TagMode.StartTagAndEndTag;
        if (Split)
        {
            output.TagName = "div";
            AddClasses(output, "jp-split-button");
            output.Content.SetHtmlContent(
                $"<button type=\"button\" class=\"jp-button{style}\"{disabled}>{Html.Text(Content)}</button>" +
                $"<button type=\"button\" class=\"jp-button{style}\" popovertarget=\"{menuId}\" aria-haspopup=\"menu\" aria-label=\"More\"{disabled}><i class=\"jp-caret\"></i></button>" + popup);
        }
        else
        {
            output.TagName = "span";
            AddClasses(output, "jp-drop-down-button");
            output.Content.SetHtmlContent(
                $"<button type=\"button\" class=\"jp-button{style}\" popovertarget=\"{menuId}\" aria-haspopup=\"menu\"{disabled}>{Html.Text(Content)} <i class=\"jp-caret\"></i></button>" + popup);
        }
        if (Id is not null)
        {
            output.Attributes.SetAttribute("id", Id);
        }
        ApplyCommon(output);
    }
}

/// <summary><c>&lt;jp-drop-down-button content="Export"&gt;&lt;jp-menu-item header="PDF" /&gt;&lt;/jp-drop-down-button&gt;</c>: a button that opens a menu.</summary>
[HtmlTargetElement("jp-drop-down-button")]
public class DropDownButtonTagHelper : SplitButtonTagHelper
{
    protected override bool Split => false;
}
